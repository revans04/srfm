<!-- LoginPage.vue -->
<template>
  <q-page class="fill-height">
    <div class="row justify-center">
      <div class="col-xs-12 col-sm-8 col-md-4">
        <q-card class="shadow-3">
          <q-toolbar class="bg-primary text-white" flat>
            <q-toolbar-title><h1 class="login-title">{{ headline }}</h1></q-toolbar-title>
          </q-toolbar>
          <q-card-section>
            <p class="text-center">{{ subhead }}</p>
          </q-card-section>
          <q-card-actions class="column items-center q-pb-lg q-gutter-sm">
            <div id="google-signin-button"></div>
            <q-btn
              color="primary"
              icon="login"
              :label="ctaLabel"
              :loading="loading"
              @click="loginWithPopup"
            />
            <q-btn
              flat
              dense
              no-caps
              :label="altLinkLabel"
              color="primary"
              :to="{ path: altLinkPath, query: $route.query }"
            />
          </q-card-actions>
          <q-card-section v-if="error">
            <q-banner class="bg-negative text-white" dense>{{ error }}</q-banner>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../store/auth';

const loading = ref(false);
const error = ref('');
const googleLoaded = ref(false);
const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

// /signup uses the same component but rephrases the surface as account
// creation. Auth itself is Google-only, so the underlying flow is identical.
const isSignup = computed(() => route.meta?.mode === 'signup');
const headline = computed(() => (isSignup.value ? 'Create your Steady Rise account' : 'Steady Rise Login'));
const subhead = computed(() =>
  isSignup.value
    ? 'Sign up with your Google account to start managing your finances.'
    : 'Sign in with your Google account to manage your finances.',
);
const ctaLabel = computed(() => (isSignup.value ? 'Sign up with Google' : 'Sign in with Google'));
const altLinkLabel = computed(() => (isSignup.value ? 'Already have an account? Sign in' : 'New here? Create an account'));
const altLinkPath = computed(() => (isSignup.value ? '/login' : '/signup'));

// Where to send the user after a successful sign-in. Honors ?redirect=… so
// AcceptInvitePage can round-trip an unauthenticated user through login and
// land them back on the invite to complete it. All sibling query params (e.g.
// `token` for the invite) are forwarded onto the redirect target.
function resolvePostLoginTarget() {
  const redirect = route.query.redirect;
  if (typeof redirect !== 'string' || !redirect.startsWith('/')) {
    return { path: '/' };
  }
  const forwarded: Record<string, string> = {};
  for (const [key, value] of Object.entries(route.query)) {
    if (key === 'redirect' || key === 'mode') continue;
    if (typeof value === 'string') forwarded[key] = value;
  }
  return { path: redirect, query: forwarded };
}

onMounted(() => {
  const checkGoogle = () => {
    if (window.google && window.google.accounts) {
      googleLoaded.value = true;
      window.google.accounts.id.initialize({
        client_id: '583821970715-53n7g2bv1r3s810vro67vaiqujiek4en.apps.googleusercontent.com',
        callback: (response) => {
          void handleCredentialResponse(response);
        },
      });
      const btn = document.getElementById('google-signin-button');
      if (btn) {
        window.google.accounts.id.renderButton(btn, {
          theme: 'outline',
          size: 'large',
          text: 'signin_with',
          logo_alignment: 'left',
        });
      }
    } else {
      setTimeout(checkGoogle, 100);
    }
  };
  checkGoogle();
});

const handleCredentialResponse = async (response: { credential: string }) => {
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
    await router.push(resolvePostLoginTarget());
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : 'Unknown error';
    error.value = msg || 'Failed to sign in with Google';
    console.error('Login error:', err);
  } finally {
    loading.value = false;
  }
};

const loginWithPopup = async () => {
  loading.value = true;
  error.value = '';
  try {
    await authStore.loginWithGoogle();
    await router.push(resolvePostLoginTarget());
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : 'Unknown error';
    error.value = msg || 'Failed to sign in with Google';
    console.error('Login popup error:', err);
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.fill-height {
  height: 100vh;
}

.login-title {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: inherit;
}
</style>
