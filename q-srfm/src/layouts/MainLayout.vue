<template>
  <q-layout view="lHh Lpr lFf">

    <q-header v-if="isMobile && !isLoginRoute" class="mobile-header">
      <div class="row items-center no-wrap q-px-md q-py-sm">
        <img
          src="../assets/family-funds-sm.png"
          alt="Steady Rise Financial Management logo"
          height="32"
          class="q-mr-sm"
        />
        <span class="text-subtitle1">Steady Rise Financial Management</span>
      </div>
    </q-header>

    <q-drawer
      v-model="leftDrawerOpen"
      show-if-above
      side="left"
      :width="260"
      class="column app-drawer"
    >
      <div class="column justify-between fit app-drawer__content">
        <div>
          <div class="app-drawer__brand">
            <img src="../assets/family-funds-sm.png" alt="Steady Rise Financial Management" />
            <div class="app-drawer__brand-name">Steady Rise</div>
          </div>
          <q-list class="app-drawer__nav">
            <q-item
              v-for="item in navItems.filter(i => i.desktop)"
              :key="item.title"
              :to="item.path"
              clickable
              class="app-drawer__item"
              active-class="app-drawer__item--active"
              @click="onNavClick(item)"
            >
              <q-item-section avatar>
                <q-icon :name="item.icon" />
              </q-item-section>
              <q-item-section>{{ item.title }}</q-item-section>
            </q-item>
          </q-list>
        </div>
        <q-list>
          <q-item disabled>
            <q-item-section>
              <div class="text-caption text-center text-white text-opacity">Version: {{ appVersion }}</div>
            </q-item-section>
          </q-item>
          <q-separator dark inset />
          <q-item clickable @click="signOut" :title="auth.user?.email" class="app-drawer__profile">
            <q-item-section avatar>
              <q-avatar size="36" color="white" text-color="primary">
                <img :src="auth.avatarSrc" alt="User Avatar" />
              </q-avatar>
            </q-item-section>
            <q-item-section>{{ auth.user?.email }}</q-item-section>
          </q-item>
        </q-list>
      </div>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>

    <q-footer v-if="isMobile && !isLoginRoute" class="mobile-footer">
      <div class="row no-wrap justify-around items-center">
        <q-btn
          v-for="item in navItems.filter(i => i.mobile)"
          :key="item.title"
          dense
          flat
          color="white"
          :icon="item.icon"
          @click="onNavClick(item)"
          :title="item.title"
        >
          <span class="text-caption">{{ item.title }}</span>
        </q-btn>
      </div>
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

const leftDrawerOpen = ref(true);
const currentTab = ref('/');
const windowWidth = ref(window.innerWidth);

const isMobile = computed(() => windowWidth.value < 960);
const isLoginRoute = computed(() => route.path === '/login');
const appVersion = version;

const navItems = [
  { title: 'Dashboard', path: '/', icon: 'dashboard', desktop: true, mobile: true },
  { title: 'Budget', path: '/budget', icon: 'savings', desktop: true, mobile: true },
  { title: 'Transactions', path: '/transactions', icon: 'format_list_bulleted', desktop: true, mobile: true },
  { title: 'Accounts', path: '/accounts', icon: 'account_balance', desktop: true, mobile: true },
  { title: 'Reports', path: '/reports', icon: 'trending_up', desktop: true, mobile: false },
  { title: 'Data Mgmt', path: '/data', icon: 'dataset', desktop: true, mobile: false },
  { title: 'Settings', path: '/settings', icon: 'manage_accounts', desktop: true, mobile: false },
  { title: 'Logout', path: '', icon: 'logout', desktop: false, mobile: true },
];


async function onNavClick(item: { title: string; path: string }) {
  if (item.title === 'Logout') {
    await signOut();
  } else {
    await router.push(item.path);
    currentTab.value = item.path;
  }
}

async function signOut() {
  await auth.logout();
  await router.push('/login');
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
.q-drawer {
  transition: transform 0.3s ease-in-out;
  background: transparent;
}

.app-drawer {
  background: linear-gradient(180deg, #1d4ed8 0%, #1e3a8a 100%);
  color: #ffffff;
}

.app-drawer__content {
  gap: 24px;
}

.app-drawer__brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 32px 16px 24px;
  text-align: center;
  gap: 12px;
}

.app-drawer__brand img {
  width: 64px;
  height: 64px;
  border-radius: 16px;
  background: #ffffff;
  padding: 8px;
  box-shadow: var(--shadow-subtle);
}

.app-drawer__brand-name {
  font-size: 1.1rem;
  font-weight: 600;
}

.app-drawer__nav {
  padding: 0 12px;
  background: transparent;
}

.app-drawer__item {
  border-radius: 14px;
  margin-bottom: 6px;
  padding: 12px 14px;
  color: #0f172a;
  background: rgba(255, 255, 255, 0.9);
  transition: background 0.2s ease, color 0.2s ease, transform 0.2s ease;
}

.app-drawer__item:hover {
  background: #ffffff;
  transform: translateX(4px);
}

.app-drawer__item :deep(.q-item__label),
.app-drawer__item :deep(.q-icon) {
  color: inherit;
}

.app-drawer__item--active {
  background: #ffffff;
  color: #1d4ed8;
  font-weight: 600;
}

.app-drawer__item--active :deep(.q-icon) {
  color: #1d4ed8;
}

.app-drawer__footer {
  padding: 16px 12px 24px;
}

.app-drawer__profile {
  border-radius: 12px;
}

.app-drawer__profile:hover {
  background: rgba(255, 255, 255, 0.12);
}

.text-opacity {
  opacity: 0.7;
}

.mobile-header {
  background: linear-gradient(90deg, #1d4ed8 0%, #1e3a8a 100%);
  color: #ffffff;
  padding: 12px 16px;
}

.mobile-header img {
  border-radius: 12px;
  background: #ffffff;
  padding: 6px;
}

.mobile-footer {
  background: linear-gradient(90deg, #1d4ed8 0%, #1e3a8a 100%);
  color: #ffffff;
  padding: 6px 12px;
}

.mobile-footer .q-btn {
  flex: 1;
  border-radius: 12px;
  min-height: 48px;
}

.mobile-footer .q-btn span {
  font-size: 0.75rem;
  font-weight: 600;
}
</style>
