<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated class="bg-primary text-white">
      <q-toolbar class="q-px-md">
        <q-btn flat dense round icon="menu" @click="toggleLeftDrawer" />
        <q-toolbar-title>SRFM</q-toolbar-title>
        <q-space />
        <q-btn flat label="Logout" @click="logout" />
      </q-toolbar>
    </q-header>

    <q-drawer v-model="leftDrawerOpen" show-if-above bordered side="left" :width="250">
      <q-list>
        <q-item-label header>Navigation</q-item-label>
        <q-item to="/" clickable exact active-class="text-primary">
          <q-item-section avatar>
            <q-icon name="home" />
          </q-item-section>
          <q-item-section>Home</q-item-section>
        </q-item>
        <q-item to="/dashboard" clickable active-class="text-primary">
          <q-item-section avatar>
            <q-icon name="dashboard" />
          </q-item-section>
          <q-item-section>Dashboard</q-item-section>
        </q-item>
      </q-list>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const leftDrawerOpen = ref(false);

function toggleLeftDrawer() {
  console.log('Toggling drawer, current state:', leftDrawerOpen.value);
  leftDrawerOpen.value = !leftDrawerOpen.value;
}

function logout() {
  console.log('Logout clicked');
  // Implement logout logic, e.g., clear auth and redirect
  router.push('/login');
}
</script>

<style scoped>
.q-toolbar {
  min-height: 64px;
  padding: 0 16px;
}
.q-toolbar-title {
  font-size: 1.5rem;
}
.q-drawer {
  transition: transform 0.3s ease-in-out;
}
</style>
