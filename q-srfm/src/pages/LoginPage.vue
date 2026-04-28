<!-- LoginPage.vue -->
<template>
  <q-page class="login-page">
    <div class="login-page__shell">
      <q-card class="login-card">
        <!-- Brand header. Mirrors the lockup the rest of the app uses
             (sidebar / mobile header) so the login feels like the same product
             rather than a separate auth surface. -->
        <div class="login-card__brand">
          <img
            src="../assets/logo-sm.png"
            alt=""
            aria-hidden="true"
            class="login-card__brand-mark"
          />
          <div class="login-card__brand-name">Steady Rise</div>
        </div>

        <q-card-section class="login-card__body">
          <h1 class="login-card__title">Sign in to Steady Rise</h1>
          <p class="login-card__subtitle">
            Use your Google account — we'll create one if you're new.
          </p>

          <div class="login-card__cta">
            <q-btn
              unelevated
              no-caps
              color="primary"
              icon="login"
              label="Sign in with Google"
              :loading="loading"
              class="login-card__cta-btn"
              @click="loginWithPopup"
            />
          </div>

          <q-banner
            v-if="error"
            class="login-card__error bg-negative text-white q-mt-md"
            dense
          >
            {{ error }}
          </q-banner>

          <p class="login-card__legal">
            By signing in you agree to our terms of use. Your finances stay
            private to your family.
          </p>
        </q-card-section>
      </q-card>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../store/auth';

const loading = ref(false);
const error = ref('');
const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

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
.login-page {
  min-height: 100vh;
  background: var(--color-surface-page);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px 16px;
}

.login-page__shell {
  width: 100%;
  max-width: 420px;
}

.login-card {
  border-radius: var(--radius-lg);
  overflow: hidden;
}

.login-card__brand {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 24px 24px 8px;
}

.login-card__brand-mark {
  width: 36px;
  height: 36px;
  object-fit: contain;
  border-radius: 8px;
}

.login-card__brand-name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text-primary);
  letter-spacing: 0.01em;
}

.login-card__body {
  padding: 8px 24px 24px;
}

.login-card__title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 6px;
  line-height: 1.2;
}

.login-card__subtitle {
  font-size: 0.95rem;
  color: var(--color-text-muted);
  line-height: 1.45;
  margin: 0 0 20px;
}

.login-card__cta {
  display: flex;
  flex-direction: column;
}

.login-card__cta-btn {
  width: 100%;
  padding: 12px 16px;
  font-size: 1rem;
  font-weight: 600;
}

.login-card__error {
  border-radius: var(--radius-sm);
}

.login-card__legal {
  font-size: 0.75rem;
  color: var(--color-text-muted);
  line-height: 1.45;
  margin: 16px 0 0;
}
</style>
