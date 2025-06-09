<template>
  <q-container>
    <q-row justify="center">
      <q-col cols="12" md="6">
        <q-card>
          <q-card-title>Email Verification</q-card-title>
          <q-card-text v-if="loading">Verifying...</q-card-text>
          <q-card-text v-else-if="error">{{ error }}</q-card-text>
          <q-card-text v-else>Email verified successfully! You can now <router-link to="/login">log in</router-link>.</q-card-text>
        </q-card>
      </q-col>
    </q-row>
  </q-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
const loading = ref(true);
const error = ref('');
const apiBaseUrl = process.env.VUE_APP_API_BASE_URL;

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
  } catch (err: any) {
    error.value = err.message || 'Verification failed';
    loading.value = false;
  }
});
</script>
