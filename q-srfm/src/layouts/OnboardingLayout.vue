<!--
  Layout for the /setup route. The user is authenticated but has no family
  yet (or is doing focused entity-add work from Settings). Hides the
  sidebar/drawer, mobile header, and bottom tab bar so nothing pulls them
  off the seed flow.

  Keeps a small SRFM-branded top bar plus a "Sign out" escape so a user who
  arrives here unintentionally (or via the previous cross-user cache leak,
  pre-PR-3.5) can exit without rummaging through Settings.
-->
<template>
  <q-layout view="lHh Lpr lFf">
    <q-header class="onboarding-header" elevated>
      <div class="row items-center justify-between no-wrap q-px-md q-py-sm">
        <div class="row items-center no-wrap">
          <img
            src="../assets/logo-sm.png"
            alt="Steady Rise Financial Management logo"
            height="32"
            class="q-mr-sm"
          />
          <span class="text-subtitle1 text-weight-bold">Steady Rise</span>
        </div>
        <q-btn
          v-if="auth.user"
          flat
          dense
          no-caps
          icon="logout"
          label="Sign out"
          color="white"
          @click="onSignOut"
        >
          <q-tooltip>Sign out and return to login</q-tooltip>
        </q-btn>
      </div>
    </q-header>

    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useAuthStore } from '../store/auth';

const router = useRouter();
const auth = useAuthStore();

async function onSignOut() {
  try {
    await auth.logout();
  } catch (err) {
    // Non-fatal — Firebase usually still drops the session even when the
    // promise rejects (e.g. network blip). Push to /login regardless so
    // the user always has a way out.
    console.error('Sign out from onboarding failed', err);
  } finally {
    void router.replace('/login');
  }
}
</script>

<style scoped>
.onboarding-header {
  background: var(--q-primary);
  color: white;
}
</style>
