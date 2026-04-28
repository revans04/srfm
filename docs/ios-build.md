# iOS Build (Capacitor shell)

SRFM ships to iOS as a Capacitor wrap of the Quasar SPA. The native shell
lives in `q-srfm/src-capacitor/`. The web app is unchanged — Capacitor
just hosts it inside `WKWebView`.

## Prerequisites

- **Xcode 15+** (tested with 26.4). Run `xcode-select -p` to confirm a
  developer dir is set; if it points at the Command Line Tools, run
  `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer`.
- **First-launch components installed.** After installing or updating
  Xcode, run **`sudo xcodebuild -runFirstLaunch`** once. Skipping this
  causes the `pod install` step of `quasar build -m capacitor -T ios` to
  fail with `IDESimulatorFoundation` / `DVTDownloads` symbol-not-found
  errors. (If you see those, this is the fix.)
- **CocoaPods.** `brew install cocoapods` (Apple Silicon). System Ruby
  2.6 is too old.
- **Node 20+ / Yarn 1.22+.** Same as the web build.
- An Apple Developer account is **not** required to build for the
  simulator. It is required to install on a physical device or upload to
  TestFlight.

## Build for the iOS simulator

```bash
cd q-srfm

# IMPORTANT: VITE_API_BASE_URL must be set explicitly. The default
# resolution in dataAccess.ts/firebase/init.ts/boot/axios.ts treats
# `window.location.hostname === 'localhost'` as a localhost-API
# signal — but on Capacitor the origin is `capacitor://localhost`,
# so without VITE_API_BASE_URL the bundled app will try to hit
# http://localhost:8080 on the device (which is the device itself,
# not your laptop) and every request will fail.
VITE_API_BASE_URL=https://app.steadyrise.us/api yarn build -m capacitor -T ios
```

The build:
1. Compiles the Quasar SPA into `src-capacitor/www/`.
2. Runs `npx cap sync ios`, which copies `www/` into
   `src-capacitor/ios/App/App/public/` and runs `pod install`.
3. Hands control to `xcodebuild` to produce a build product.

To open the project in Xcode (e.g. to launch the simulator manually):

```bash
open q-srfm/src-capacitor/ios/App/App.xcworkspace
```

Always open the **`.xcworkspace`**, not the `.xcodeproj` — CocoaPods
configures the workspace.

## Configuration

`q-srfm/src-capacitor/capacitor.config.ts` is the source of truth:

- `appId: com.steadyrise.srfm`
- `appName: SteadyRise`
- `iosScheme: "capacitor"` — the bundled app is served from
  `capacitor://localhost`. This is the origin sent in CORS preflight,
  and is added to the API allowlist in [`api/Program.cs`](../api/Program.cs).
  If you change `iosScheme`, the API allowlist must change too.
- `server.url` is intentionally **not** set. Setting it bakes a
  developer-machine URL (e.g. live-reload over LAN) into the bundle and
  ships broken to anyone else.

## Signing

Phase 1 leaves signing as the Xcode default ("Automatically manage
signing", which uses your personal team for simulator builds). For
TestFlight / App Store distribution, this section will be expanded with
the team ID, provisioning profile, and entitlements once we have an
Apple Developer membership and decide on bundle prefix ownership.

## `skipNativeAuth: true` is required

`q-srfm/src-capacitor/capacitor.config.json` sets:

```json
"plugins": {
  "FirebaseAuthentication": {
    "skipNativeAuth": true,
    "providers": ["google.com"]
  }
}
```

`skipNativeAuth: true` makes the Capacitor plugin return only the
Google credential (idToken) and **not** call Firebase iOS SDK's
`Auth.auth().signIn(with:)`. The JS code in
`q-srfm/src/firebase/init.ts` then exchanges that idToken for a Firebase
session via `signInWithCredential(auth, credential)`. This is the
Capawesome plugin's recommended pattern when JS Firebase Auth is the
source of truth.

Why we need this: Firebase iOS SDK writes the signed-in user to the
keychain. On the iOS Simulator, with the unsigned/ad-hoc-signed builds
we use during development (no Apple Developer team configured), the
keychain access fails with `OSStatus -34018` and the plugin throws
`"keychain error"` even though Google sign-in itself succeeded. Routing
auth through JS Firebase sidesteps the native keychain write entirely.

`providers: ["google.com"]` is also required — without it the plugin
rejects `signInWithGoogle` with
`"Google sign-in provider is not enabled. Make sure to add the provider
to the 'providers' list in the Capacitor configuration."`

## Ad-hoc code signing for the simulator

The simulator build needs a code signature (even ad-hoc) for some
Capacitor pods to function — a fully unsigned binary works for boot but
GoogleSignIn SDK in particular hits keychain errors. Use:

```bash
xcodebuild -workspace App.xcworkspace -scheme App -configuration Debug \
  -destination 'generic/platform=iOS Simulator' \
  -derivedDataPath build \
  CODE_SIGN_IDENTITY="-" CODE_SIGNING_REQUIRED=NO CODE_SIGNING_ALLOWED=YES \
  build
```

Verify with `codesign -dvv build/Build/Products/Debug-iphonesimulator/App.app`
— look for `Signature=adhoc`. The earlier `CODE_SIGNING_ALLOWED=NO`
incantation strips the signature entirely and breaks GoogleSignIn at
runtime.

## Manual simulator build (use this until signing is set up)

`quasar build -m capacitor -T ios` runs a release build that requires a
configured Apple Developer team and will fail at the signing step. Use
this manual flow for simulator testing:

```bash
cd q-srfm
rm -rf dist src-capacitor/www src-capacitor/ios/App/build

VITE_API_BASE_URL=https://app.steadyrise.us/api \
  LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 \
  yarn build

cp -R dist/spa src-capacitor/www
cd src-capacitor && LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 npx cap copy ios

cd ios/App && rm -rf Pods Podfile.lock && \
  LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 pod install

xcodebuild -workspace App.xcworkspace -scheme App -configuration Debug \
  -destination 'generic/platform=iOS Simulator' \
  -derivedDataPath build \
  CODE_SIGN_IDENTITY="-" CODE_SIGNING_REQUIRED=NO CODE_SIGNING_ALLOWED=YES \
  build

xcrun simctl boot "iPhone 17 Pro" 2>/dev/null
open -a Simulator
xcrun simctl uninstall booted com.steadyrise.srfm
xcrun simctl install booted \
  "$(pwd)/build/Build/Products/Debug-iphonesimulator/App.app"
xcrun simctl launch booted com.steadyrise.srfm
```

To capture the JS console, attach Safari Web Inspector: **Develop →
Simulator → App** in Safari's menu bar (enable Develop menu in Safari
Preferences first). `simctl log stream` does **not** surface WebKit
console output reliably in this environment — Safari's inspector is
the highest-signal diagnostic.

## Phase 1 verification status

Build pipeline: ✅ working end-to-end.
- `quasar build -m capacitor -T ios` completes through `cap sync`, `pod install`, and Xcode compile.
- A debug simulator build succeeds via:
  ```bash
  cd q-srfm/src-capacitor/ios/App && \
    xcodebuild -workspace App.xcworkspace -scheme App -configuration Debug \
      -destination 'generic/platform=iOS Simulator' \
      -derivedDataPath build CODE_SIGNING_ALLOWED=NO build
  ```
  This is the recommended path until Apple Developer signing is set up;
  the default `quasar build -m capacitor -T ios` runs a release build
  that requires a development team.

Runtime: ⚠️ **app launches but renders a blank page.** On `iPhone 17 Pro
/ iOS 26.x`, after `xcrun simctl install` + `simctl launch
com.steadyrise.srfm`, the WKWebView hosts the bundle (`index.html`,
chunked JS, env values including `VITE_FIREBASE_*` and
`VITE_API_BASE_URL=https://app.steadyrise.us/api` are all present in the
shipped bundle), but `<div id="q-app">` stays empty. No content paints
beyond the system status bar.

Most likely cause (not yet confirmed — `simctl log` did not surface
WebKit console messages in this environment): Vue Router is configured
in `history` mode in [quasar.config.ts:55](../q-srfm/quasar.config.ts:55).
Under `capacitor://localhost`, the initial URL path interaction with the
router base is a known sharp edge — `hash` mode is the standard Capacitor
recommendation. This needs verification via Safari Web Inspector before
fixing.

To diagnose:
1. Open Safari → Develop menu → choose the booted simulator → "App"
   target. The WKWebView's full console + DOM is available there.
2. Look for a Vue/router error during cold start, an unresolved Pinia
   store, or a Firebase init exception.
3. If it's the router, the cleanest fix is to flip `vueRouterMode` to
   `'hash'` *only for capacitor mode* (Quasar's config wrapper exposes
   `ctx.mode.capacitor`).

This is being treated as a **separate follow-up**, not a Phase 1
deliverable. The shell, build pipeline, signing-free simulator path,
CORS allowlist, and config scaffolding are all in place — the next
iteration just needs to identify and fix the runtime boot issue.

## Capacitor Firebase Authentication — pod integration hack

The `@capacitor-firebase/authentication` plugin (v8.x) **does not link
correctly via stock CocoaPods 1.16.x** when you depend on its `Google`
subspec. CocoaPods generates the plugin as a `PBXAggregateTarget` (a
no-op grouping target) instead of a `PBXNativeTarget`, so the plugin's
Swift sources never enter any compile phase, the App target never links
against the plugin, and at runtime the JS bridge throws
`"FirebaseAuthentication" plugin is not implemented on ios` the first
time you call any method on it.

The fix lives in [`q-srfm/src-capacitor/ios/App/Podfile`](../q-srfm/src-capacitor/ios/App/Podfile)
as a `post_install` hook that:

1. Locates the `App` user target via `installer.aggregate_targets`.
2. Walks `node_modules/@capacitor-firebase/authentication/ios/Plugin/**/*.{swift,m,h}`
   and adds every source file directly to App's Sources build phase.
3. Adds `-DRGCFA_INCLUDE_GOOGLE` to App's `OTHER_SWIFT_FLAGS` so the
   plugin's `#if RGCFA_INCLUDE_GOOGLE` branches (the Google sign-in
   handler) compile in. This is the same flag the `Google` subspec
   would have set on the (broken) plugin pod target.
4. Adds `App/GoogleService-Info.plist` to App's resources build phase
   so `FirebaseApp.configure()` can auto-discover it.

Because the plugin is compiled directly into App, you will **not** see
`-framework "CapacitorFirebaseAuthentication"` in
`Pods/Target Support Files/Pods-App/Pods-App.debug.xcconfig`'s
`OTHER_LDFLAGS` — that is expected. What you should see instead:

```bash
nm -arch arm64 \
  src-capacitor/ios/App/build/Build/Products/Debug-iphonesimulator/App.app/App.debug.dylib \
  | grep -c FirebaseAuthenticationPlugin
# expect a few hundred symbols (~380 as of 8.2.0). Zero means the hook
# did not run — re-check Podfile and rerun `pod install`.
```

The dependent frameworks (`FirebaseAuth`, `GoogleSignIn`,
`GTMAppAuth`, etc.) are still pulled in normally by the other pods, so
nothing else needs to be linked manually.

Other Podfile settings that matter and should not be reverted:
- `platform :ios, '15.0'` — Firebase iOS SDK 12.x deployment target.
- `use_frameworks! :linkage => :static` — Firebase iOS SDK's
  recommended setup; required because of the plugin's static-framework
  dependency chain.
- `pod 'CapacitorFirebaseAuthentication', :subspecs => ['Lite', 'Google']`
  — `Lite` excludes the optional providers (Facebook, Twitter, etc.)
  whose pods we don't ship; `Google` is what we actually use.
- `static_framework = true` is **removed** from
  `node_modules/@capacitor-firebase/authentication/CapacitorFirebaseAuthentication.podspec`
  (deliberate diagnostic edit; safe to leave because the post_install
  hook bypasses the pod target entirely). If you re-run `npm install`
  it will come back; re-remove it or accept the slight re-test cost.

If the plugin ever publishes a fix that makes CocoaPods generate a
proper `PBXNativeTarget` again, the entire `post_install` hook (except
`assertDeploymentTarget`) can be deleted and replaced with the standard
podspec wiring.

## Known gaps deferred to follow-up phases

These are intentionally out of scope for the Phase 1 shell:

- **Live reload.** Not configured. If you need it during development,
  add a `server.url` to a local copy of `capacitor.config.ts` (do not
  commit) pointing at `http://<your-LAN-ip>:9000`.
- **Push notifications, biometrics, widgets, native receipt OCR, Plaid
  iOS SDK.** Each gets its own follow-up.
- **Viewport meta `user-scalable=no`** and the rest of the
  CLAUDE.md-flagged mobile UI/UX violations remain unfixed in this PR.

## Pre-flight findings (recorded for future reference)

- `apiBaseUrl` is computed in **three places** independently
  ([dataAccess.ts:32](../q-srfm/src/dataAccess.ts:32),
  [boot/axios.ts:19](../q-srfm/src/boot/axios.ts:19),
  [firebase/init.ts:11](../q-srfm/src/firebase/init.ts:11)) and each
  falls back to `http://localhost:8080/api` when
  `window.location.hostname === 'localhost'`. On Capacitor the origin
  is `capacitor://localhost`, so this fallback misfires unless
  `VITE_API_BASE_URL` is set at build time. Long-term this should be
  consolidated into a single config helper; for now, just always set
  the env var.
- `localStorage` is used by Pinia persisted state, the tour store, and
  the onboarding banner. All small string values — fine in `WKWebView`.
- The viewport meta in `q-srfm/index.html` already conditionally adds
  `viewport-fit=cover` for capacitor mode, which is required for
  safe-area inset handling on notched devices. No action needed.
