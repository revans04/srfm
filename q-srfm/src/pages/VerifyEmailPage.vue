<template>
  <q-page class="bg-grey-1 q-pa-lg">
    <h1 class="page-title q-mb-md">Email Verification</h1>
    <div class="row justify-center">
      <div class="col col-12 col-md-6">
        <q-card>
          <q-card-section v-if="loading" class="row items-center q-gutter-sm">
            <q-spinner size="20px" color="primary" />
            <span>Verifying…</span>
          </q-card-section>
          <q-card-section v-else-if="error">{{ error }}</q-card-section>
          <q-card-section v-else>
            <div class="row items-center q-gutter-sm q-mb-sm">
              <q-icon name="check_circle" color="positive" size="24px" />
              <span class="text-weight-medium">Email verified.</span>
            </div>
            <div v-if="authStore.user" class="text-body2 text-grey-8">
              Sending you back to your budget…
            </div>
            <div v-else class="text-body2 text-grey-8">
              You can now <router-link to="/login">sign in</router-link>.
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../store/auth';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const loading = ref(true);
const error = ref('');
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

onMounted(async () => {
  const token = route.query.token as string;
  if (!token) {
    error.value = 'No verification token provided';
    loading.value = false;
    return;
  }

  try {
    const response = await fetch(`${apiBaseUrl}/auth/verify-email?token=${token}`);
    if (!response.ok) {
      throw new Error(await response.text());
    }
    loading.value = false;

    // If the user is authenticated in this tab (clicked the link from
    // their email while still signed in), refresh the Firebase user record
    // so `emailVerified` flips true in the auth store, then send them back
    // to the budget. The brief "Email verified" notice is enough — no
    // confetti per design system rules ("calm, not cheerleading").
    if (authStore.user) {
      try {
        await authStore.user.reload();
      } catch {
        // Token may be stale; non-fatal.
      }
      setTimeout(() => {
        void router.replace('/budget');
      }, 1500);
    }
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Verification failed';
    loading.value = false;
  }
});
</script>
