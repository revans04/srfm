#!/usr/bin/env bash
set -euo pipefail

# Unified deploy script for both API (Dotnet) and Web (Quasar SPA)

# ---- Config ----
PROJECT_ID="budget-buddy-a6b6c"         # gcloud config get-value project to confirm
REGION="us-central1"                    # Cloud Run + Artifact Registry region
REPO="family-budget-repo"               # Artifact Registry repo name (Docker format)

# Service names
API_SERVICE="family-budget-api"
WEB_SERVICE="srfm-web"

# Image URIs
API_IMAGE="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO}/${API_SERVICE}:latest"
WEB_IMAGE="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO}/${WEB_SERVICE}:latest"

# Paths
API_DIR="api"
WEB_DIR="q-srfm"
# Secret Manager secret name containing the DB connection string for API
SUPABASE_SECRET_NAME="supabase-db-connection"
# Secret Manager secret name containing Firebase service account JSON
FIREBASE_CREDENTIALS_SECRET_NAME="firebase-credentials-json"

# Resolve the Cloud Run runtime service account (defaults to project default compute SA)
PROJECT_NUMBER=$(gcloud projects describe "${PROJECT_ID}" --format="value(projectNumber)")
RUN_SERVICE_ACCOUNT="${RUN_SERVICE_ACCOUNT:-${PROJECT_NUMBER}-compute@developer.gserviceaccount.com}"

echo "Authenticating Docker with Artifact Registry..."
gcloud auth configure-docker "${REGION}-docker.pkg.dev" --quiet

echo "Setting up/buildx builder..."
docker buildx create --name srfm_builder --use 2>/dev/null || docker buildx use srfm_builder

echo "Building and pushing API image: ${API_IMAGE}"
docker buildx build \
  --platform linux/amd64 \
  -f "${API_DIR}/Dockerfile" \
  -t "${API_IMAGE}" \
  --push \
  "${API_DIR}"

echo "Building and pushing Web image: ${WEB_IMAGE}"
docker buildx build \
  --platform linux/amd64 \
  -f "${WEB_DIR}/Dockerfile" \
  -t "${WEB_IMAGE}" \
  --push \
  "${WEB_DIR}"

echo "Deploying API service: ${API_SERVICE} in ${REGION}"
gcloud run deploy "${API_SERVICE}" \
  --image "${API_IMAGE}" \
  --platform managed \
  --region "${REGION}" \
  --allow-unauthenticated \
  --ingress all \
  --service-account "${RUN_SERVICE_ACCOUNT}" \
  --remove-env-vars=SUPABASE_DB_CONNECTION,GOOGLE_APPLICATION_CREDENTIALS_JSON \
  --set-secrets "SUPABASE_DB_CONNECTION=${SUPABASE_SECRET_NAME}:latest,GOOGLE_APPLICATION_CREDENTIALS_JSON=${FIREBASE_CREDENTIALS_SECRET_NAME}:latest"

echo "Deploying Web service: ${WEB_SERVICE} in ${REGION}"
gcloud run deploy "${WEB_SERVICE}" \
  --image "${WEB_IMAGE}" \
  --platform managed \
  --region "${REGION}" \
  --allow-unauthenticated

echo "All done. API: ${API_SERVICE}, Web: ${WEB_SERVICE}"
