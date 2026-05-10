# SRFM (q-srfm)

The Quasar (Vite) SPA for SteadyRise Family Money. See the [root README](../README.md) for the full stack overview (API + web + deploy).

## Prerequisites

- Node.js 20.x
- Yarn 1.22+ (or npm — either works, yarn is what CI uses)

## Install dependencies

```bash
yarn          # or: npm install
```

The `postinstall` script runs `quasar prepare` automatically.

## Run the app locally

```bash
yarn dev      # or: npm run dev
# equivalently: npx quasar dev
```

The Vite dev server starts (default http://localhost:9000). API calls are routed to `http://localhost:8080/api` per `.env.development`.

> **Don't run bare `quasar dev`** — that uses the globally installed `@quasar/cli`, which only ships a small set of commands (`create`, `info`, `upgrade`, `serve`). `dev` and `build` live in the project-local `@quasar/app-vite` package and are exposed via `npm run …` / `npx quasar …`. Running the global directly errors with `Unknown command "dev"`.

## Build for production

```bash
yarn build    # or: npm run build
```

Output lands in `dist/spa/`. To preview the built bundle locally:

```bash
npx quasar serve dist/spa --history --port 4000
```

`quasar serve` (built into the global CLI) is a **production preview only** — it serves static files from `dist/spa/` with Vue Router history-mode fallback. It is **not** a substitute for `quasar dev`; serving without a build produces broken pages (raw EJS template placeholders like `<% if ... %>` leak through as viewport/meta errors).

## Lint & format

```bash
yarn lint
yarn format
```

## Test

```bash
yarn test
```

Runs `tsc -p tsconfig.tests.json`, the test prep script, and Node's built-in test runner against compiled output in `dist-tests/`.

## Deploy

**Use the unified `./deploy.sh` at the repo root** — it handles the API and the web app together, takes care of Node-version mismatches (auto-sources nvm and `nvm use --lts` when the host is on a non-LTS Node), and uses `npm` rather than yarn so the engines check passes.

```bash
# from the repo root
./deploy.sh                                          # patch bump, API + Web (Firebase)
./deploy.sh --target=web --bump=none                 # web only, no version bump
./deploy.sh --bump=minor --firebase-project=foo-bar  # minor bump, explicit project
```

The legacy `npm run update-version-and-deploy` from this directory still works, but it shells out to `yarn build` and will fail engines checks on non-LTS Node (e.g. Node 25 "Current"). Prefer the root script. See the [root README](../README.md#deployments) for the full deploy options.

## Troubleshooting

- **`quasar dev` → "Unknown command 'dev'"** — you're hitting the global v1 CLI launcher. Use `npm run dev` or `npx quasar dev`.
- **Blank page with `"1<%"` / `"if"` / `"}"` console errors** — `quasar serve` was run without (or before) `quasar build`. Build first, or just use `npm run dev`.
- **Port 9000 already in use** — kill the stale process: `lsof -iTCP -sTCP:LISTEN -P | grep 9000` then `kill -9 <pid>`.
- **Stale `.quasar/` cache after a refactor** — `rm -rf .quasar node_modules/.cache` and re-run `npm run dev`.

## Customize the configuration

See [Configuring quasar.config.js](https://v2.quasar.dev/quasar-cli-vite/quasar-config-js).
