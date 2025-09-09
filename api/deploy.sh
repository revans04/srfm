#!/bin/bash

# Configuration
PROJECT_ID="budget-buddy-a6b6c"
REGION="us-central1"
REPO="family-budget-repo"
SERVICE_NAME="family-budget-api"
IMAGE="us-central1-docker.pkg.dev/${PROJECT_ID}/${REPO}/${SERVICE_NAME}:latest"
# Secret Manager secret name containing the DB connection string
SUPABASE_SECRET_NAME="supabase-db-connection"
# Secret Manager secret name containing Firebase service account JSON
FIREBASE_CREDENTIALS_SECRET_NAME="firebase-credentials-json"

# Resolve the Cloud Run runtime service account (defaults to project default compute SA)
PROJECT_NUMBER=$(gcloud projects describe "${PROJECT_ID}" --format="value(projectNumber)")
RUN_SERVICE_ACCOUNT="${RUN_SERVICE_ACCOUNT:-${PROJECT_NUMBER}-compute@developer.gserviceaccount.com}"

# Step 1: Authenticate Docker with Artifact Registry
echo "Authenticating Docker with Artifact Registry..."
gcloud auth configure-docker us-central1-docker.pkg.dev --quiet

# Step 2: Build and push the Docker image
echo "Building and pushing Docker image..."
docker buildx create --name mybuilder --use 2>/dev/null || docker buildx use mybuilder
docker buildx build \
  --platform linux/amd64 \
  --tag "${IMAGE}" \
  --push \
  .

# Step 3: Deploy to Cloud Run
echo "Deploying to Cloud Run..."
gcloud run deploy "${SERVICE_NAME}" \
  --image "${IMAGE}" \
  --platform managed \
  --region "${REGION}" \
  --allow-unauthenticated \
  --ingress all \
  --service-account "${RUN_SERVICE_ACCOUNT}" \
  --remove-env-vars=SUPABASE_DB_CONNECTION,GOOGLE_APPLICATION_CREDENTIALS_JSON \
  --set-secrets "SUPABASE_DB_CONNECTION=${SUPABASE_SECRET_NAME}:latest,GOOGLE_APPLICATION_CREDENTIALS_JSON=${FIREBASE_CREDENTIALS_SECRET_NAME}:latest"

echo "Deployment complete!"
