import { initializeApp } from 'firebase/app';
import { getAuth, GoogleAuthProvider, onAuthStateChanged, signInWithPopup } from 'firebase/auth';
import type { User } from 'firebase/auth';

const env = ((import.meta as unknown) as { env?: Record<string, string> }).env || {};
const firebaseConfig = {
  apiKey: env.VITE_FIREBASE_API_KEY ?? '',
  authDomain: env.VITE_FIREBASE_AUTH_DOMAIN ?? '',
  projectId: env.VITE_FIREBASE_PROJECT_ID ?? '',
};
const apiBaseUrl = env.VITE_API_BASE_URL
  || (typeof window !== 'undefined' && window.location.hostname === 'localhost'
        ? 'http://localhost:8080/api'
        : '/api');

let auth: ReturnType<typeof getAuth>;
let provider: GoogleAuthProvider;
if (env.VITE_FIREBASE_API_KEY) {
  const firebaseApp = initializeApp(firebaseConfig);
  auth = getAuth(firebaseApp);
  provider = new GoogleAuthProvider();
} else {
  // Test environment stub
  auth = {} as unknown as ReturnType<typeof getAuth>;
  provider = new GoogleAuthProvider();
}

const signInWithGoogle = async (): Promise<User | null> => {
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
