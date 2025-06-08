// src/firebase/auth.ts
import { auth } from "./index";
import { onAuthStateChanged, User } from "firebase/auth";
import { Notify } from "quasar"; // Optional: For user notifications

// Use Quasar's environment variable (set in .env or quasar.config.js)
const apiBaseUrl = process.env.API_BASE_URL; // Changed from VUE_APP_API_BASE_URL

export async function getIdToken(): Promise<string | null> {
  const user = auth.currentUser;
  return user ? await user.getIdToken(true) : null; // Force refresh
}

export const setupAuthListener = () => {
  const unsubscribe = onAuthStateChanged(auth, async (user) => {
    if (user) {
      try {
        // Force refresh to get the latest user profile
        await user.reload();
        const token = await user.getIdToken();

        const response = await fetch(`${apiBaseUrl}/auth/ensure-profile`, {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        });

        if (!response.ok) {
          const errorText = await response.text();
          console.error(`Ensure profile failed: ${response.status} - ${errorText}`);
          // Optional: Notify user of error
          Notify.create({
            type: "negative",
            message: "Failed to sync user profile. Please try again later.",
          });
        } else {
          console.log("Ensure profile succeeded");
          // Optional: Notify user of success (if desired)
          // Notify.create({
          //   type: "positive",
          //   message: "User profile synced successfully.",
          // });
        }
      } catch (err) {
        console.error("Fetch error:", err);
        // Optional: Notify user of error
        Notify.create({
          type: "negative",
          message: "An error occurred while syncing your profile.",
        });
      }
    }
  });

  // Optional: Return unsubscribe function for cleanup if needed
  return unsubscribe;
};
