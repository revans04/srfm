#!/bin/sh
set -e

echo "==> ci_post_clone: installing JS dependencies and building Capacitor iOS assets"

brew install node

REPO_ROOT="$CI_PRIMARY_REPOSITORY_PATH"

cd "$REPO_ROOT/q-srfm"
npm install

cd src-capacitor
npm install
cd ..

npx quasar build -m capacitor -T ios

cd src-capacitor/ios/App
pod install --repo-update
