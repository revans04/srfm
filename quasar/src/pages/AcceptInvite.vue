<template>
  <q-page-container>
    <q-page padding>
      <div class="text-h4 q-mb-md">Accept Invite</div>
      <q-spinner v-if="loading" color="primary" size="3em" />
      <q-banner v-else-if="error" class="bg-negative text-white q-mb-md">
        {{ error }}
      </q-banner>
      <div v-else-if="accepted">
        <q-banner class="bg-positive text-white q-mb-md">
          You’ve joined the family! Redirecting to dashboard...
        </q-banner>
      </div>
      <div v-else-if="!user">
        <p class="q-mb-md">Please log in or sign up to accept this invite.</p>
        <q-btn color="primary" label="Log In" class="q-mr-sm" @click="login" />
        <q-btn color="primary" label="Sign Up" @click="signup" />
      </div>
    </q-page>
  </q-page-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { auth } from '../firebase/index';
import { dataAccess } from '../dataAccess';

const router = useRouter();
const route = useRoute();
const loading = ref(true);
const error = ref<string | null>(null);
const accepted = ref(false);
const user = ref(auth.currentUser);

onMounted(async () => {
  const token = route.query.token as string;
  if (!token) {
    error.value = 'No invite token provided';
    loading.value = false;
    return;
  }

  auth.onAuthStateChanged(async (currentUser) => {
    user.value = currentUser;
    if (currentUser) {
      try {
        await dataAccess.acceptInvite(token);
        accepted.value = true;
        setTimeout(() => router.push('/'), 2000);
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
