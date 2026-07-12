#!/bin/sh
set -e

echo "==> ci_post_clone: installing JS dependencies and building Capacitor iOS assets"

export HOMEBREW_NO_AUTO_UPDATE=1
export HOMEBREW_NO_INSTALL_CLEANUP=1

for i in 1 2 3; do
  if brew install node; then
    break
  fi
  echo "brew install node failed (attempt $i), retrying..."
  sleep 5
  if [ "$i" = 3 ]; then
    echo "brew install node failed after 3 attempts"
    exit 1
  fi
done

REPO_ROOT="$CI_PRIMARY_REPOSITORY_PATH"

cd "$REPO_ROOT/q-srfm"
npm install

cd src-capacitor
npm install
cd ..

npx quasar build -m capacitor -T ios --skip-pkg
