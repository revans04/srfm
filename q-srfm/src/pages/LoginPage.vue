<!-- LoginPage.vue -->
<template>
  <q-page class="fill-height">
    <div class="row justify-center">
      <div class="col-xs-12 col-sm-8 col-md-4">
        <q-card class="elevation-12">
          <q-toolbar class="bg-primary text-white" flat>
            <q-toolbar-title>Steady Rise Login</q-toolbar-title>
          </q-toolbar>
          <q-card-section>
            <p class="text-center">Sign in with your Google account to manage your finances.</p>
          </q-card-section>
          <q-card-actions class="justify-center q-pb-lg">
            <div id="google-signin-button"></div>
          </q-card-actions>
          <q-card-section v-if="error">
            <q-banner type="error" dense>{{ error }}</q-banner>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../store/auth';

const loading = ref(false);
const error = ref('');
const googleLoaded = ref(false);
const router = useRouter();
const authStore = useAuthStore();
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

onMounted(() => {
  const checkGoogle = () => {
    if (window.google && window.google.accounts) {
      googleLoaded.value = true;
      window.google.accounts.id.initialize({
        client_id: '583821970715-53n7g2bv1r3s810vro67vaiqujiek4en.apps.googleusercontent.com',
        callback: handleCredentialResponse,
      });
      window.google.accounts.id.renderButton(document.getElementById('google-signin-button')!, {
        theme: 'outline',
        size: 'large',
        text: 'signin_with',
        logo_alignment: 'left',
      });
    } else {
      setTimeout(checkGoogle, 100);
    }
  };
  checkGoogle();
});

const handleCredentialResponse = async (response: any) => {
  loading.value = true;
  error.value = '';

  try {
    const googleIdToken = response.credential;
    const apiResponse = await fetch(`${apiBaseUrl}/auth/google-login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ googleIdToken }),
    });

    if (!apiResponse.ok) {
      const errorText = await apiResponse.text();
      throw new Error(`API login failed: ${errorText}`);
    }
    const { token } = await apiResponse.json();

    await authStore.loginWithCustomToken(token);
    await authStore.user?.reload();
    router.push('/');
  } catch (err: any) {
    error.value = err.message || 'Failed to sign in with Google';
    console.error('Login error:', err);
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
