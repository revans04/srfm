#!/usr/bin/env bash
set -euo pipefail

# Unified deploy script:
#   - API (.NET)        -> Cloud Run via Docker
#   - Web (Quasar SPA)  -> Firebase Hosting (default) or Cloud Run via Docker
#
# Optionally bumps the q-srfm app version + syncs .env.production before building.
# Reuses q-srfm/scripts/updateVersionAndDeploy.js (via --no-deploy) so the
# version-bump logic lives in one place.

# ---- Defaults / Args ----
BUMP="patch"            # patch | minor | major | none
TARGET="all"            # all | api | web
WEB_HOST="firebase"     # firebase | cloudrun
FIREBASE_PROJECT=""     # falls back to VITE_FIREBASE_PROJECT_ID from .env.production

usage() {
  cat <<EOF
Usage: $0 [options]

Options:
  --bump=<patch|minor|major|none>   Bump q-srfm version before deploying (default: patch)
  --target=<all|api|web>            What to deploy (default: all)
  --web-host=<firebase|cloudrun>    Where to deploy the web app (default: firebase)
  --firebase-project=<id>           Override Firebase project id
  -h, --help                        Show this help

Examples:
  $0                                          # bump patch, deploy API + Web (Firebase)
  $0 --bump=none                              # deploy without bumping
  $0 --target=api --bump=none                 # deploy only the API
  $0 --target=web --web-host=cloudrun         # web only, to Cloud Run instead of Firebase
  $0 --bump=minor --firebase-project=foo-bar  # minor bump, deploy with explicit project
EOF
}

for arg in "$@"; do
  case "${arg}" in
    --bump=*)             BUMP="${arg#*=}" ;;
    --target=*)           TARGET="${arg#*=}" ;;
    --web-host=*)         WEB_HOST="${arg#*=}" ;;
    --firebase-project=*) FIREBASE_PROJECT="${arg#*=}" ;;
    -h|--help)            usage; exit 0 ;;
    *) echo "Unknown option: ${arg}"; usage; exit 1 ;;
  esac
done

case "${TARGET}"   in all|api|web)        ;; *) echo "Invalid --target=${TARGET}"; exit 1 ;; esac
case "${WEB_HOST}" in firebase|cloudrun)  ;; *) echo "Invalid --web-host=${WEB_HOST}"; exit 1 ;; esac
case "${BUMP}"     in patch|minor|major|none) ;; *) echo "Invalid --bump=${BUMP}"; exit 1 ;; esac

# ---- Config ----
PROJECT_ID="budget-buddy-a6b6c"
REGION="us-central1"
REPO="family-budget-repo"
API_SERVICE="family-budget-api"
WEB_SERVICE="srfm-web"
API_IMAGE="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO}/${API_SERVICE}:latest"
WEB_IMAGE="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO}/${WEB_SERVICE}:latest"
API_DIR="api"
WEB_DIR="q-srfm"
SUPABASE_SECRET_NAME="supabase-db-connection"
FIREBASE_CREDENTIALS_SECRET_NAME="firebase-credentials-json"
BREVO_API_KEY_SECRET_NAME="brevo-api-key"

# Resolve to repo root (this script's directory)
cd "$(dirname "$0")"

# ---- Helpers ----

# Ensure the host Node satisfies q-srfm/package.json engines (LTS-only: ^18 ^20 ^22 ^24 ^26 ^28).
# Only relevant for the Firebase Hosting path, which runs `yarn build` on the host.
ensure_lts_node() {
  local current_major=""
  if command -v node >/dev/null 2>&1; then
    current_major="$(node -v | sed 's/^v//' | cut -d. -f1)"
  fi
  case "${current_major}" in
    18|20|22|24|26|28)
      echo "Using Node $(node -v) (LTS)."
      return 0
      ;;
  esac

  echo "Host Node is '${current_major:-not installed}', not an LTS major; trying nvm..."
  if [[ -s "${NVM_DIR:-$HOME/.nvm}/nvm.sh" ]]; then
    # shellcheck disable=SC1091
    . "${NVM_DIR:-$HOME/.nvm}/nvm.sh"
    nvm use --lts
  else
    echo "ERROR: nvm not found and host Node is non-LTS." >&2
    echo "Install/select an LTS Node (e.g. 'nvm use --lts' or 'nvm install --lts') and re-run." >&2
    exit 1
  fi
}

ensure_docker_env() {
  echo "Authenticating Docker with Artifact Registry..."
  gcloud auth configure-docker "${REGION}-docker.pkg.dev" --quiet
  docker buildx create --name srfm_builder --use 2>/dev/null || docker buildx use srfm_builder
}

deploy_api() {
  ensure_docker_env

  echo "Building and pushing API image: ${API_IMAGE}"
  docker buildx build \
    --platform linux/amd64 \
    -f "${API_DIR}/Dockerfile" \
    -t "${API_IMAGE}" \
    --push \
    "${API_DIR}"

  PROJECT_NUMBER=$(gcloud projects describe "${PROJECT_ID}" --format="value(projectNumber)")
  RUN_SERVICE_ACCOUNT="${RUN_SERVICE_ACCOUNT:-${PROJECT_NUMBER}-compute@developer.gserviceaccount.com}"

  echo "Deploying API service: ${API_SERVICE} in ${REGION}"
  gcloud run deploy "${API_SERVICE}" \
    --image "${API_IMAGE}" \
    --platform managed \
    --region "${REGION}" \
    --allow-unauthenticated \
    --ingress all \
    --service-account "${RUN_SERVICE_ACCOUNT}" \
    --remove-env-vars=SUPABASE_DB_CONNECTION,GOOGLE_APPLICATION_CREDENTIALS_JSON,BREVO_KEY \
    --set-secrets "SUPABASE_DB_CONNECTION=${SUPABASE_SECRET_NAME}:latest,GOOGLE_APPLICATION_CREDENTIALS_JSON=${FIREBASE_CREDENTIALS_SECRET_NAME}:latest,BREVO_KEY=${BREVO_API_KEY_SECRET_NAME}:latest"
}

deploy_web_cloudrun() {
  ensure_docker_env

  echo "Building and pushing Web image: ${WEB_IMAGE}"
  docker buildx build \
    --platform linux/amd64 \
    -f "${WEB_DIR}/Dockerfile" \
    -t "${WEB_IMAGE}" \
    --push \
    "${WEB_DIR}"

  echo "Deploying Web service: ${WEB_SERVICE} in ${REGION}"
  gcloud run deploy "${WEB_SERVICE}" \
    --image "${WEB_IMAGE}" \
    --platform managed \
    --region "${REGION}" \
    --allow-unauthenticated
}

deploy_web_firebase() {
  ensure_lts_node

  pushd "${WEB_DIR}" >/dev/null

  echo "Building SPA (yarn build)..."
  yarn build

  if [[ -z "${FIREBASE_PROJECT}" ]]; then
    if [[ -f .env.production ]]; then
      FIREBASE_PROJECT=$(grep -E '^VITE_FIREBASE_PROJECT_ID=' .env.production | cut -d= -f2- | tr -d '\r' || true)
    fi
  fi
  if [[ -z "${FIREBASE_PROJECT}" ]]; then
    echo "ERROR: --firebase-project not set and VITE_FIREBASE_PROJECT_ID missing from q-srfm/.env.production" >&2
    exit 1
  fi

  echo "Deploying to Firebase Hosting (project: ${FIREBASE_PROJECT})..."
  npx firebase deploy --only hosting --project "${FIREBASE_PROJECT}"

  popd >/dev/null
}

# ---- Optional: bump version ----
if [[ "${BUMP}" != "none" ]]; then
  echo "Bumping ${WEB_DIR} version (${BUMP}) and syncing .env.production..."
  (cd "${WEB_DIR}" && node scripts/updateVersionAndDeploy.js "${BUMP}" --no-deploy)
fi

# ---- Deploy ----
if [[ "${TARGET}" == "all" || "${TARGET}" == "api" ]]; then
  deploy_api
fi

if [[ "${TARGET}" == "all" || "${TARGET}" == "web" ]]; then
  case "${WEB_HOST}" in
    cloudrun) deploy_web_cloudrun ;;
    firebase) deploy_web_firebase ;;
  esac
fi

echo "Done. target=${TARGET} web-host=${WEB_HOST} bump=${BUMP}"
