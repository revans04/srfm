<!-- LoginView.vue -->
<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card class="elevation-12">
          <v-toolbar color="primary" dark flat>
            <v-toolbar-title>Steady Rise Login</v-toolbar-title>
          </v-toolbar>
          <v-card-text>
            <p class="text-center">
              Sign in with your Google account to manage your finances.
            </p>
          </v-card-text>
          <v-card-actions class="justify-center pb-4">
            <!-- Replace v-btn with a div for Google button -->
            <div id="google-signin-button"></div>
          </v-card-actions>
          <v-alert v-if="error" type="error" dense>{{ error }}</v-alert>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { signInWithCustomToken } from "firebase/auth";
import { auth } from "../firebase/index";

// Reactive state
const loading = ref(false);
const error = ref("");
const googleLoaded = ref(false);
const router = useRouter();
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

// Check if Google script is loaded and render button
onMounted(() => {
  const checkGoogle = () => {
    if (window.google && window.google.accounts) {
      googleLoaded.value = true;
      // Initialize and render the Google Sign-In button
      window.google.accounts.id.initialize({
        client_id: "583821970715-53n7g2bv1r3s810vro67vaiqujiek4en.apps.googleusercontent.com",
        callback: handleCredentialResponse,
      });
      window.google.accounts.id.renderButton(
        document.getElementById("google-signin-button")!,
        {
          theme: "outline",
          size: "large",
          text: "signin_with",
          logo_alignment: "left",
        }
      );
    } else {
      setTimeout(checkGoogle, 100); // Retry every 100ms
    }
  };
  checkGoogle();
});

// Handle Google Sign-In response
const handleCredentialResponse = async (response: any) => {
  loading.value = true;
  error.value = "";

  try {
    const googleIdToken = response.credential;
    const apiResponse = await fetch(apiBaseUrl + "/auth/google-login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ googleIdToken }),
    });

    if (!apiResponse.ok) {
      const errorText = await apiResponse.text();
      throw new Error(`API login failed: ${errorText}`);
    }
    const { token } = await apiResponse.json();

    const userCredential = await signInWithCustomToken(auth, token);
    await userCredential.user.reload(); // Force refresh
    router.push("/");
  } catch (err: any) {
    error.value = err.message || "Failed to sign in with Google";
    console.error(err);
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.fill-height {
  height: 100vh;
}
</style>