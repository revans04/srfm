#!/bin/sh
set -e

echo "==> ci_post_clone: installing JS dependencies and building Capacitor iOS assets"

REPO_ROOT="$CI_PRIMARY_REPOSITORY_PATH"

if ! command -v yarn >/dev/null 2>&1; then
  corepack enable
  corepack prepare yarn@1.22.22 --activate
fi

cd "$REPO_ROOT/q-srfm"
yarn install --frozen-lockfile

cd src-capacitor
yarn install --frozen-lockfile
cd ..

yarn build -m capacitor -T ios

cd src-capacitor/ios/App
pod install --repo-update
