# Multi-stage build for Quasar SPA served by Nginx on Cloud Run

# 1) Build stage: install deps and run quasar build
FROM node:20-alpine AS build
WORKDIR /app

# Install Yarn (classic) and dependencies using yarn.lock for consistency
RUN corepack enable && corepack prepare yarn@1.22.22 --activate || true

# Copy manifests first for better caching
COPY package.json yarn.lock ./
# Install deps without running lifecycle scripts (avoids quasar prepare early)
RUN yarn install --non-interactive --ignore-scripts --network-timeout 600000

# Copy the rest and build
COPY . .
# Now run postinstall (quasar prepare) and then build
RUN yarn run postinstall && yarn build

# 2) Runtime stage: Nginx configured for SPA routing and Cloud Run port
FROM nginx:alpine AS runtime

# Copy custom nginx config (listens on 8080 and SPA fallback)
COPY scripts/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built artifacts
COPY --from=build /app/dist/spa /usr/share/nginx/html

# Cloud Run provides $PORT=8080 by default; our config listens on 8080
EXPOSE 8080

CMD ["nginx", "-g", "daemon off;"]
