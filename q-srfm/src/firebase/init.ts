import { initializeApp } from 'firebase/app';
import {
  getAuth,
  GoogleAuthProvider,
  indexedDBLocalPersistence,
  initializeAuth,
  onAuthStateChanged,
  signInWithCredential,
  signInWithPopup,
} from 'firebase/auth';
import type { Auth, User } from 'firebase/auth';
import { Capacitor } from '@capacitor/core';
import { FirebaseAuthentication } from '@capacitor-firebase/authentication';

// Capacitor.isNativePlatform() is `true` only inside the packaged iOS /
// Android app. In every browser (Firebase Hosting / Cloud Run / local
// dev) it returns `false`, and this whole module behaves exactly as it
// did before the Capacitor integration: `getAuth(app)` + `signInWithPopup`.
//
// On native, the Firebase web SDK's default popup/redirect resolver
// boots an invisible `apis.google.com` iframe to recover redirect state
// — that iframe is blocked by the WebView's `capacitor://localhost`
// origin (CORS + nosniff), and `onAuthStateChanged` never resolves.
// We avoid it by:
//   1. Calling `initializeAuth(app, { persistence: indexedDBLocalPersistence })`
//      WITHOUT a `popupRedirectResolver` — auth state restores from
//      IndexedDB only.
//   2. Doing the actual Google sign-in via the Capacitor Firebase
//      Authentication native plugin, then bridging the returned ID
//      token back into the web SDK with `signInWithCredential`. This
//      keeps every existing consumer of `auth.currentUser` /
//      `getIdToken()` working unchanged.
const isNative = Capacitor.isNativePlatform();

const env = ((import.meta as unknown) as { env?: Record<string, string> }).env || {};
const firebaseConfig = {
  apiKey: env.VITE_FIREBASE_API_KEY ?? '',
  authDomain: env.VITE_FIREBASE_AUTH_DOMAIN ?? '',
  projectId: env.VITE_FIREBASE_PROJECT_ID ?? '',
};
// On Capacitor native the WebView origin is `capacitor://localhost`, so a
// relative `/api` URL has no real backend to hit. Read the absolute API
// URL from `VITE_API_BASE_URL_NATIVE`. Web path is unchanged. Kept in
// sync with the same branch in `src/dataAccess.ts`.
const nativeApiUrl = env.VITE_API_BASE_URL_NATIVE;
if (isNative && !nativeApiUrl) {
  throw new Error('VITE_API_BASE_URL_NATIVE is required for Capacitor builds');
}
const apiBaseUrl = isNative
  ? (nativeApiUrl as string)
  : (env.VITE_API_BASE_URL
      || (typeof window !== 'undefined' && window.location.hostname === 'localhost'
            ? 'http://localhost:8080/api'
            : '/api'));

let auth: Auth;
let provider: GoogleAuthProvider;
if (env.VITE_FIREBASE_API_KEY) {
  const firebaseApp = initializeApp(firebaseConfig);
  auth = isNative
    ? initializeAuth(firebaseApp, { persistence: indexedDBLocalPersistence })
    : getAuth(firebaseApp);
  provider = new GoogleAuthProvider();
} else {
  // Test environment stub
  auth = {} as unknown as Auth;
  provider = new GoogleAuthProvider();
}

const signInWithGoogle = async (): Promise<User | null> => {
  if (isNative) {
    const result = await FirebaseAuthentication.signInWithGoogle();
    const idToken = result.credential?.idToken;
    if (!idToken) {
      throw new Error('Native Google sign-in returned no ID token');
    }
    const credential = GoogleAuthProvider.credential(idToken);
    const userCredential = await signInWithCredential(auth, credential);
    return userCredential.user ?? null;
  }
  const result = await signInWithPopup(auth, provider);
  return result.user ?? null;
};

const setupAuthListener = () => {
  // eslint-disable-next-line @typescript-eslint/no-misused-promises
  onAuthStateChanged(auth, async (user) => {
    if (user) {
      // Force refresh to get the latest user profile
      await user.reload();

      const token = await user.getIdToken();
      try {
        const response = await fetch(apiBaseUrl + "/auth/ensure-profile", {
          method: "POST",
          headers: {
            "Authorization": `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        });
        if (!response.ok) {
          console.error(`Ensure profile failed: ${response.status} - ${await response.text()}`);
        } else {
          console.log("Ensure profile succeeded");
        }
      } catch (err) {
        console.error("Fetch error:", err);
      }
    }
  });
};

export { auth, signInWithGoogle, setupAuthListener };
