<template>
  <v-container>
    <v-row justify="center">
      <v-col cols="12" md="6">
        <v-card>
          <v-card-title>Email Verification</v-card-title>
          <v-card-text v-if="loading">Verifying...</v-card-text>
          <v-card-text v-else-if="error">{{ error }}</v-card-text>
          <v-card-text v-else>Email verified successfully! You can now <router-link to="/login">log in</router-link>.</v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
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
  } catch (err: any) {
    error.value = err.message || 'Verification failed';
    loading.value = false;
  }
});
</script>