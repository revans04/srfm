<template>
  <q-page>
    <h1>Accept Invite</h1>
    <q-circular-progress v-if="loading" indeterminate></q-circular-progress>
    <q-banner v-else-if="error" type="negative">{{ error }}</q-banner>
    <div v-else-if="accepted">
      <q-banner type="positive">You’ve joined the family! Redirecting to dashboard...</q-banner>
    </div>
    <div v-else-if="!user">
      <p>Please log in or sign up to accept this invite.</p>
      <q-btn @click="login">Log In</q-btn>
      <q-btn @click="signup">Sign Up</q-btn>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { auth } from '../firebase/init';
import { dataAccess } from '../dataAccess';

const router = useRouter();
const route = useRoute();
const loading = ref(true);
const error = ref<string | null>(null);
const accepted = ref(false);
const user = ref(auth.currentUser);

onMounted(() => {
  const token = route.query.token as string;
  if (!token) {
    error.value = 'No invite token provided';
    loading.value = false;
    return;
  }

  auth.onAuthStateChanged((currentUser) => {
    user.value = currentUser;
    if (currentUser) {
      void (async () => {
        try {
          await dataAccess.acceptInvite(token);
          accepted.value = true;
          setTimeout(() => {
            void router.push('/');
          }, 2000);
        } catch (err: unknown) {
          error.value = err instanceof Error ? err.message : JSON.stringify(err);
        } finally {
          loading.value = false;
        }
      })();
    } else {
      loading.value = false;
    }
  });
});

function login() {
  void router.push({
    path: '/login',
    query: { redirect: '/accept-invite', token: typeof route.query.token === 'string' ? route.query.token : '' },
  });
}

function signup() {
  void router.push({
    path: '/signup',
    query: { redirect: '/accept-invite', token: typeof route.query.token === 'string' ? route.query.token : '' },
  });
}
</script>
