#!/usr/bin/env node

// Bumps the app version, syncs it to .env.production (VITE_APP_VERSION),
// then deploys the q-srfm app to either Cloud Run (default) or Firebase Hosting.

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const projectRoot = path.resolve(__dirname, '..');
const pkgPath = path.join(projectRoot, 'package.json');
const envProdPath = path.join(projectRoot, '.env.production');
const deployScriptPath = path.join(projectRoot, 'deploy.sh');

// CLI usage examples:
//   node scripts/updateVersionAndDeploy.js patch
//   node scripts/updateVersionAndDeploy.js minor --provider=firebase --project=<firebase-project-id>
//
// Positional arg 1 = bump type (patch | minor | major)
// --provider=cloudrun|firebase (default: cloudrun)
// --project=<firebase project id> (only for provider=firebase; defaults to VITE_FIREBASE_PROJECT_ID)

const bumpType = (process.argv[2] || 'patch').toLowerCase();
const providerArg = process.argv.find((a) => a.startsWith('--provider='));
const projectArg = process.argv.find((a) => a.startsWith('--project='));
const provider = providerArg ? providerArg.split('=')[1] : 'firebase';
const fbProjectFromArg = projectArg ? projectArg.split('=')[1] : '';

function bumpVersion(version, type) {
  const m = /^([0-9]+)\.([0-9]+)\.([0-9]+)(?:[-+].*)?$/.exec(version || '0.0.0');
  if (!m) return '0.0.1';
  let [_, major, minor, patch] = m;
  let M = parseInt(major, 10) || 0;
  let mnr = parseInt(minor, 10) || 0;
  let p = parseInt(patch, 10) || 0;
  switch (type) {
    case 'major':
      return `${M + 1}.0.0`;
    case 'minor':
      return `${M}.${mnr + 1}.0`;
    case 'patch':
    default:
      return `${M}.${mnr}.${p + 1}`;
  }
}

function ensureEnvVersion(filePath, newVersion) {
  let content = '';
  try {
    content = fs.readFileSync(filePath, 'utf8');
  } catch (e) {
    // If file doesn't exist, create it with just the version
    fs.writeFileSync(filePath, `VITE_APP_VERSION=${newVersion}\n`, 'utf8');
    return;
  }

  const lines = content.split(/\r?\n/);
  let found = false;
  const updated = lines
    .map((line) => {
      if (line.startsWith('VITE_APP_VERSION=')) {
        found = true;
        return `VITE_APP_VERSION=${newVersion}`;
      }
      return line;
    })
    .filter((l, idx, arr) => !(idx === arr.length - 1 && l.trim() === ''));

  if (!found) updated.push(`VITE_APP_VERSION=${newVersion}`);

  fs.writeFileSync(filePath, updated.join('\n') + '\n', 'utf8');
}

try {
  // 1) Read and bump version in package.json
  const pkg = JSON.parse(fs.readFileSync(pkgPath, 'utf8'));
  const current = pkg.version || '0.0.0';
  const next = bumpVersion(current, bumpType);
  pkg.version = next;
  fs.writeFileSync(pkgPath, JSON.stringify(pkg, null, 2) + '\n', 'utf8');
  console.log(`Version bumped: ${current} -> ${next}`);

  // 2) Sync .env.production with VITE_APP_VERSION
  ensureEnvVersion(envProdPath, next);
  console.log(`Updated ${path.relative(projectRoot, envProdPath)} with VITE_APP_VERSION=${next}`);

  // 3) Build and deploy the q-srfm service (uses Docker + Cloud Run)
  if (provider === 'firebase') {
    // Ensure firebase.json exists for SPA hosting
    const fbJsonPath = path.join(projectRoot, 'firebase.json');
    if (!fs.existsSync(fbJsonPath)) {
      const fbJson = {
        hosting: {
          public: 'dist/spa',
          ignore: ['firebase.json', '**/.*', '**/node_modules/**'],
          rewrites: [{ source: '**', destination: '/index.html' }]
        }
      };
      fs.writeFileSync(fbJsonPath, JSON.stringify(fbJson, null, 2) + '\n', 'utf8');
      console.log('Created firebase.json for SPA hosting (dist/spa).');
    }

    // Determine Firebase project
    const envProd = fs.existsSync(envProdPath) ? fs.readFileSync(envProdPath, 'utf8') : '';
    const envProj = (envProd.match(/^VITE_FIREBASE_PROJECT_ID=(.+)$/m) || [])[1] || '';
    const fbProject = fbProjectFromArg || envProj || 'your-firebase-project-id';

    // Create .firebaserc if missing
    const firebasercPath = path.join(projectRoot, '.firebaserc');
    if (!fs.existsSync(firebasercPath)) {
      const rc = { projects: { default: fbProject } };
      fs.writeFileSync(firebasercPath, JSON.stringify(rc, null, 2) + '\n', 'utf8');
      console.log(`Created .firebaserc with default project: ${fbProject}`);
    }

    console.log('Building SPA for Firebase Hosting (yarn build)...');
    execSync('yarn build', { cwd: projectRoot, stdio: 'inherit' });
    console.log(`Deploying to Firebase Hosting (project: ${fbProject})...`);
    execSync(`npx firebase deploy --only hosting --project ${fbProject}`, { cwd: projectRoot, stdio: 'inherit' });
    console.log('Firebase Hosting deployment successful.');
  } else {
    console.log('Building and deploying q-srfm (Cloud Run)...');
    execSync(`bash "${deployScriptPath}"`, { stdio: 'inherit' });
    console.log('Cloud Run deployment successful.');
  }
} catch (err) {
  console.error('Error during update and deploy:', err?.message || err);
  process.exit(1);
}
