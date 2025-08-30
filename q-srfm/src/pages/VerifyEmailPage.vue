<template>
  <q-page>
    <div class="row justify-center">
      <div class="col col-12 col-md-6">
        <q-card>
          <q-card-section>Email Verification</q-card-section>
          <q-card-section v-if="loading">Verifying...</q-card-section>
          <q-card-section v-else-if="error">{{ error }}</q-card-section>
          <q-card-section v-else>Email verified successfully! You can now <router-link to="/login">log in</router-link>.</q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
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
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Verification failed';
    loading.value = false;
  }
});
</script>
