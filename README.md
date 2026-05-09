# SteadyRise Family Money (SRFM)

This repo contains three apps:

- API: .NET 8 REST API deployed to Cloud Run (`api/`)
- Web app: Quasar (Vite) SPA deployed to Firebase Hosting at `app.steadyrise.us` (`q-srfm/`)
- Marketing site: Astro static site deployed to Cloudflare Pages at `steadyrise.us` (`marketing/`)

Below are concise instructions to run locally and deploy.

## Prerequisites

- Node.js 20.x and Yarn
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
  - `SUPABASE_DB_CONNECTION` – your Postgres connection string (Supabase is the system of record)
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
yarn
yarn dev
```

The app opens in your browser (Vite dev server). All API calls go to your local API at port 8080.

### 3) Run the marketing site locally (port 4321)

The marketing site (`marketing/`) is a standalone Astro static site — landing
page, Privacy Policy, and Terms of Service. It does not depend on the API or
the Quasar app.

```
cd marketing
npm install        # first time only
npm run dev        # http://localhost:4321
```

Pages live in `marketing/src/pages/`:

- `index.astro` — landing page (hero + features + CTA)
- `privacy.astro` — Privacy Policy
- `terms.astro` — Terms of Service

Shared chrome (header, footer, fonts) is in `marketing/src/layouts/Layout.astro`.
Design tokens mirror `design-system/colors_and_type.css` and live in
`marketing/src/styles/global.css` — keep them in sync if the design system
changes.

The header CTA links to `https://app.steadyrise.us`. Update `appUrl` in
`Layout.astro` and `index.astro` if that hostname changes.

## Deployments

The unified `deploy.sh` at the repo root handles both:

- API (.NET) → Cloud Run (Docker)
- Web (Quasar SPA) → Firebase Hosting (default) or Cloud Run (Docker)

It optionally bumps the q-srfm version + syncs `.env.production` before building.

```
./deploy.sh                                          # patch bump, deploy API + Web (Firebase)
./deploy.sh --bump=none                              # deploy without bumping
./deploy.sh --target=api --bump=none                 # API only
./deploy.sh --target=web                             # web only (defaults to Firebase)
./deploy.sh --target=web --web-host=cloudrun         # web only, Cloud Run instead of Firebase
./deploy.sh --bump=minor --firebase-project=foo-bar  # minor bump + explicit project
```

Hosting site is pinned to `budget-buddy-a6b6c` in `q-srfm/firebase.json`. The SPA uses `VITE_API_BASE_URL=/api` and Firebase Hosting rewrites `/api/**` to the Cloud Run service.

The Firebase Hosting path runs `yarn build` on the host. If your host Node isn't an LTS major (`^18 ^20 ^22 ^24`), `deploy.sh` will source nvm and `nvm use --lts` automatically; if nvm isn't installed it errors out and tells you to switch Node manually.

### API Deploy

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
# API only (no version bump)
./deploy.sh --target=api --bump=none

# Both API and Web (default: Firebase Hosting for the web)
./deploy.sh
```

After deploy, direct API URL is printed by `gcloud` (e.g., `https://family-budget-api-...run.app`). Hosting continues to call the API via `/api`.

### Marketing Site Deploy (Cloudflare Pages)

The marketing site builds to static HTML and deploys to Cloudflare Pages at
the apex domain `steadyrise.us`. The Quasar app stays at `app.steadyrise.us`.

One-time Pages project setup (Cloudflare dashboard → Pages → Create project →
Connect to Git):

- Production branch: `main`
- Root directory: `marketing`
- Build command: `npm run build`
- Build output directory: `dist`
- Node version: `20` or `22`

Then attach the apex domain in **Pages → Custom domains → Set up a custom
domain → `steadyrise.us`** and follow the DNS prompts. Leave the existing
`app.steadyrise.us` record pointed at Firebase Hosting.

Local production build (sanity check before pushing):

```
cd marketing
npm run build      # outputs to marketing/dist
npm run preview    # serves dist/ on http://localhost:4321
```

Pushes to `main` that touch `marketing/` will trigger a Cloudflare Pages
deploy automatically once the project is connected.

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
- Unified deploy: `deploy.sh` (use `--target=api` for API only, `--target=web` for web only)
- Quasar app: `q-srfm/`
- Quasar version-bump helper (called by `deploy.sh --bump=...`): `q-srfm/scripts/updateVersionAndDeploy.js`
- Firebase Hosting config: `q-srfm/firebase.json`
- Marketing site: `marketing/`
- Marketing site config: `marketing/astro.config.mjs`
