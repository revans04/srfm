<template>
  <v-container>
    <h1>Accept Invite</h1>
    <v-progress-circular v-if="loading" indeterminate></v-progress-circular>
    <v-alert v-else-if="error" type="error">{{ error }}</v-alert>
    <div v-else-if="accepted">
      <v-alert type="success">You’ve joined the family! Redirecting to dashboard...</v-alert>
    </div>
    <div v-else-if="!user">
      <p>Please log in or sign up to accept this invite.</p>
      <v-btn @click="login">Log In</v-btn>
      <v-btn @click="signup">Sign Up</v-btn>
    </div>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter, useRoute } from "vue-router";
import { auth } from "../firebase/index";
import { dataAccess } from "../dataAccess";

const router = useRouter();
const route = useRoute();
const loading = ref(true);
const error = ref<string | null>(null);
const accepted = ref(false);
const user = ref(auth.currentUser);

onMounted(async () => {
  const token = route.query.token as string;
  if (!token) {
    error.value = "No invite token provided";
    loading.value = false;
    return;
  }

  auth.onAuthStateChanged(async (currentUser) => {
    user.value = currentUser;
    if (currentUser) {
      try {
        await dataAccess.acceptInvite(token);
        accepted.value = true;
        setTimeout(() => router.push("/"), 2000);
      } catch (err: any) {
        error.value = err.message;
      }
    }
    loading.value = false;
  });
});

function login() {
  router.push(`/login?redirect=/accept-invite?token=${route.query.token}`);
}

function signup() {
  router.push(`/signup?redirect=/accept-invite?token=${route.query.token}`);
}
</script>
