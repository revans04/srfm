#!/usr/bin/env bash
set -euo pipefail

# Deploy the Quasar SPA (q-srfm) to Cloud Run via Artifact Registry

# ---- Config ----
PROJECT_ID="budget-buddy-a6b6c"           # gcloud config get-value project to confirm
REGION="us-central1"                      # Cloud Run + Artifact Registry region
REPO="family-budget-repo"                 # Existing Artifact Registry repo name
SERVICE_NAME="srfm-web"                   # Cloud Run service name for the web app
IMAGE="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO}/${SERVICE_NAME}:latest"

# Move to this script's directory (q-srfm)
cd "$(dirname "$0")"

echo "Authenticating Docker with Artifact Registry..."
gcloud auth configure-docker "${REGION}-docker.pkg.dev" --quiet

echo "Building and pushing image ${IMAGE}..."
docker buildx create --name qsrfm_builder --use 2>/dev/null || docker buildx use qsrfm_builder
docker buildx build \
  --platform linux/amd64 \
  --tag "${IMAGE}" \
  --push \
  .

echo "Deploying Cloud Run service ${SERVICE_NAME} in ${REGION}..."
gcloud run deploy "${SERVICE_NAME}" \
  --image "${IMAGE}" \
  --platform managed \
  --region "${REGION}" \
  --allow-unauthenticated

echo "Deployment complete."

