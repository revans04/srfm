<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated class="bg-primary text-white">
      <q-toolbar class="q-px-md">
        <q-btn
          flat
          dense
          round
          icon="menu"
          @click="toggleLeftDrawer"
          class="q-mr-sm"
        />
        <q-toolbar-title>SRFM</q-toolbar-title>
        <q-space />
        <q-btn flat dense icon="logout" @click="signOut" />
      </q-toolbar>
    </q-header>

    <q-drawer
      v-model="leftDrawerOpen"
      show-if-above
      bordered
      side="left"
      :width="250"
    >
      <q-list>
        <q-item
          v-for="item in navItems.filter(i => i.desktop)"
          :key="item.title"
          :to="item.path"
          clickable
          active-class="text-primary"
          @click="onNavClick(item)"
        >
          <q-item-section avatar>
            <q-icon :name="item.icon" />
          </q-item-section>
          <q-item-section>{{ item.title }}</q-item-section>
        </q-item>

        <q-item disabled class="q-mt-auto">
          <q-item-section>
            <div class="text-caption text-center">Version: {{ appVersion }}</div>
          </q-item-section>
        </q-item>

        <q-separator />

        <q-item clickable @click="signOut" :title="auth.user?.email">
          <q-item-section avatar>
            <q-avatar size="36">
              <img :src="auth.avatarSrc" alt="User Avatar" />
            </q-avatar>
          </q-item-section>
          <q-item-section>{{ auth.user?.email }}</q-item-section>
        </q-item>
      </q-list>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>

    <q-footer v-if="isMobile && !isLoginRoute" class="bg-primary text-white">
      <q-bottom-navigation v-model="currentTab" class="bg-primary text-white">
        <q-btn
          v-for="item in navItems.filter(i => i.mobile)"
          :key="item.title"
          flat
          :icon="item.icon"
          @click="onNavClick(item)"
          :title="item.title"
        >
          <span class="text-caption">{{ item.title }}</span>
        </q-btn>
      </q-bottom-navigation>
    </q-footer>
  </q-layout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../store/auth';
import version from '../version';

const router = useRouter();
const route = useRoute();
const auth = useAuthStore();

const leftDrawerOpen = ref(false);
const currentTab = ref('/');
const windowWidth = ref(window.innerWidth);

const isMobile = computed(() => windowWidth.value < 960);
const isLoginRoute = computed(() => route.path === '/login');
const appVersion = version;

const navItems = [
  { title: 'Budget', path: '/', icon: 'mdi-turtle', desktop: true, mobile: true },
  { title: 'Transactions', path: '/transactions', icon: 'mdi-format-list-bulleted', desktop: true, mobile: true },
  { title: 'Accounts', path: '/accounts', icon: 'mdi-bank-outline', desktop: true, mobile: true },
  { title: 'Reports', path: '/reports', icon: 'mdi-trending-up', desktop: true, mobile: false },
  { title: 'Data Mgmt', path: '/data', icon: 'mdi-database-export-outline', desktop: true, mobile: false },
  { title: 'Settings', path: '/settings', icon: 'mdi-account-group-outline', desktop: true, mobile: true },
  { title: 'Logout', path: '', icon: 'mdi-logout', desktop: false, mobile: true },
];

function toggleLeftDrawer() {
  leftDrawerOpen.value = !leftDrawerOpen.value;
}

function onNavClick(item: { title: string; path: string }) {
  if (item.title === 'Logout') {
    signOut();
  } else {
    router.push(item.path);
    currentTab.value = item.path;
  }
}

async function signOut() {
  await auth.logout();
  router.push('/login');
}

function handleResize() {
  windowWidth.value = window.innerWidth;
}

onMounted(() => {
  window.addEventListener('resize', handleResize);
});

onUnmounted(() => {
  window.removeEventListener('resize', handleResize);
});
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
