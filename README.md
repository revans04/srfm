# SteadyRise Family Money (SRFM)

This repo contains two apps:

- API: .NET 8 REST API deployed to Cloud Run (`api/`)
- Web: Quasar (Vite) SPA deployed to Firebase Hosting (`q-srfm/`)

Below are concise instructions to run locally and deploy.

## Prerequisites

- Node.js 20.x and npm (or Yarn)
- .NET SDK 8.0+
- Docker with Buildx
- Google Cloud SDK (`gcloud`)
- Firebase CLI (`npm i -g firebase-tools`)

Optional for deployments using Artifact Registry and Cloud Run:
- Logged in to gcloud: `gcloud auth login && gcloud config set project budget-buddy-a6b6c`

## Local Development

### 1) Run the API locally (port 8080)

From `api/`:

- Ensure env vars are available in your shell:
  - `SUPABASE_DB_CONNECTION` – your Postgres connection string
  - `GOOGLE_CLOUD_PROJECT=budget-buddy-a6b6c`
  - `GOOGLE_APPLICATION_CREDENTIALS_JSON` – contents of your Firebase service account JSON

Example (Mac/Linux):

```
cd api
export GOOGLE_CLOUD_PROJECT=budget-buddy-a6b6c
export GOOGLE_APPLICATION_CREDENTIALS_JSON="$(cat firebase-service-account.json)"
export SUPABASE_DB_CONNECTION='HOST=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Pooling=true;'

# run
dotnet run
```

The API listens on `http://localhost:8080`. Try the health check:

```
curl http://localhost:8080/api/budget/ping
```

### 2) Run the Quasar app locally

From `q-srfm/`:

- Dev env is already set in `q-srfm/.env.development` (uses `http://localhost:8080/api`).

```
cd q-srfm
npm install
npm run dev
```

The app opens in your browser (Vite dev server). All API calls go to your local API at port 8080.

## Deployments

You can deploy the web app to Firebase Hosting and the API to Cloud Run.

### Quick Web Deploy (Firebase Hosting)

From `q-srfm/`:

- Default provider is Firebase Hosting. This bumps the version, writes `VITE_APP_VERSION`, builds, and deploys.

```
cd q-srfm
npm run update-version-and-deploy           # default bump: patch
# or choose bump type
npm run update-version-and-deploy -- minor
```

Alternatively, just deploy current build:

```
npm run deploy:firebase
```

Hosting site is pinned to `budget-buddy-a6b6c` in `q-srfm/firebase.json`. The SPA uses `VITE_API_BASE_URL=/api` and Firebase Hosting rewrites `/api/**` to the Cloud Run service.

### API Deploy (Cloud Run)

There are two options:

- Deploy only API: `./api/deploy.sh`
- Deploy both API (Cloud Run) and a web container (Cloud Run): `./deploy.sh`

By default, the API is deployed publicly (`--allow-unauthenticated --ingress all`) and reads secrets from Secret Manager.

Secrets required (create once):

```
gcloud secrets create supabase-db-connection --replication-policy=automatic
gcloud secrets create firebase-credentials-json --replication-policy=automatic

# add versions
printf '%s' "$SUPABASE_DB_CONNECTION" | gcloud secrets versions add supabase-db-connection --data-file=-
printf '%s' "$(cat api/firebase-service-account.json)" | gcloud secrets versions add firebase-credentials-json --data-file=-

# grant runtime SA access (uses default compute SA by default)
PROJECT_ID=budget-buddy-a6b6c
PROJECT_NUMBER=$(gcloud projects describe "$PROJECT_ID" --format='value(projectNumber)')
RUN_SA="${PROJECT_NUMBER}-compute@developer.gserviceaccount.com"

gcloud secrets add-iam-policy-binding supabase-db-connection \
  --member="serviceAccount:${RUN_SA}" --role=roles/secretmanager.secretAccessor

gcloud secrets add-iam-policy-binding firebase-credentials-json \
  --member="serviceAccount:${RUN_SA}" --role=roles/secretmanager.secretAccessor
```

Deploy:

```
# API only
./api/deploy.sh

# Both API (Cloud Run) and Web container (Cloud Run)
./deploy.sh
```

After deploy, direct API URL is printed by `gcloud` (e.g., `https://family-budget-api-...run.app`). Hosting continues to call the API via `/api`.

### Hosting → Cloud Run (rewrite)

- `q-srfm/firebase.json` includes:
  - `"site": "budget-buddy-a6b6c"`
  - A rewrite that proxies `/api/**` to the Cloud Run service `family-budget-api` in `us-central1`.
- The SPA’s production env uses `/api`, so the browser never needs to know the direct Cloud Run URL.

## Common Issues

- 404 at `/api/...` from Hosting:
  - Ensure you’re on Firebase Blaze plan and deployed from `q-srfm/` to site `budget-buddy-a6b6c`.
  - If API is private, grant Hosting invoker role on the Cloud Run service; if public (default here), not required.

- Secret access denied on Cloud Run:
  - Grant `roles/secretmanager.secretAccessor` to the Cloud Run runtime service account for both secrets.

- Local dev still calls prod URL:
  - Remove `VITE_API_BASE_URL` from `q-srfm/.env` and set it in `.env.development`.

## File Pointers

- API Dockerfile: `api/Dockerfile`
- API deploy script: `api/deploy.sh`
- Unified deploy: `deploy.sh`
- Quasar app: `q-srfm/`
- Quasar deploy helper: `q-srfm/scripts/updateVersionAndDeploy.js`
- Firebase Hosting config: `q-srfm/firebase.json`

