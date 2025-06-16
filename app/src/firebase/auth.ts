/*** firebase/auth.ts */
import { auth } from "./index";
import { onAuthStateChanged, User } from "firebase/auth";

const apiBaseUrl = process.env.VITE_API_BASE_URL;

export async function getIdToken(): Promise<string | null> {
  const user = auth.currentUser;
  return user ? await user.getIdToken(true) : null; // Force refresh
}

export const setupAuthListener = () => {
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