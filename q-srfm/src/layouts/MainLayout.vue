<template>
  <q-layout view="lHh Lpr lFf">

    <q-header v-if="isMobile && !isLoginRoute" class="mobile-header">
      <div class="row items-center justify-center no-wrap q-px-md q-py-sm">
        <img
          src="../assets/logo-sm.png"
          alt="Steady Rise Financial Management logo"
          height="32"
          class="q-mr-sm"
        />
        <span class="text-subtitle1 text-weight-bold">Steady Rise</span>
      </div>
    </q-header>

    <q-drawer
      v-if="!isMobile"
      v-model="leftDrawerOpen"
      show-if-above
      side="left"
      :width="220"
      class="column app-drawer"
    >
      <div class="column justify-between fit app-drawer__content">
        <div class="col app-drawer__scrollable">
          <div class="app-drawer__brand">
            <img src="../assets/logo-sm.png" alt="Steady Rise Financial Management" />
            <span class="app-drawer__brand-name">Steady Rise</span>
          </div>
          <q-list class="app-drawer__nav">
            <q-item
              v-for="item in navItems"
              :key="item.title"
              :to="item.path"
              clickable
              class="app-drawer__item"
              active-class="app-drawer__item--active"
              @click="onNavClick(item)"
            >
              <q-item-section avatar>
                <q-icon :name="item.icon" size="20px" />
              </q-item-section>
              <q-item-section>{{ item.title }}</q-item-section>
            </q-item>
          </q-list>
          <div class="q-px-md q-mt-sm q-pb-sm">
            <GettingStartedChecklist />
          </div>
        </div>
        <div class="app-drawer__footer">
          <div class="app-drawer__email" @click="signOut" role="button" tabindex="0">{{ auth.user?.email }}</div>
          <div class="app-drawer__version">v{{ appVersion }}</div>
        </div>
      </div>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>

    <q-footer v-if="isMobile && !isLoginRoute" class="mobile-footer">
      <div class="row no-wrap justify-around items-center">
        <div
          v-for="item in bottomBarItems"
          :key="item.title"
          class="mobile-tab"
          :class="{ 'mobile-tab--active': route.path === item.path }"
          @click="onNavClick(item)"
        >
          <q-icon :name="item.icon" size="20px" />
          <span class="mobile-tab__label">{{ item.title }}</span>
        </div>
        <div
          class="mobile-tab"
          :class="{ 'mobile-tab--active': moreMenuItems.some(i => route.path === i.path) }"
          @click="showMoreMenu = true"
        >
          <q-icon name="more_horiz" size="20px" />
          <span class="mobile-tab__label">More</span>
        </div>
      </div>
    </q-footer>

    <q-dialog v-model="showMoreMenu" position="bottom">
      <q-card class="more-menu-sheet">
        <q-list>
          <q-item
            v-for="item in moreMenuItems"
            :key="item.title"
            clickable
            v-close-popup
            @click="onNavClick(item)"
          >
            <q-item-section avatar>
              <q-icon :name="item.icon" />
            </q-item-section>
            <q-item-section>{{ item.title }}</q-item-section>
          </q-item>
          <q-separator />
          <q-item clickable v-close-popup @click="signOut">
            <q-item-section avatar>
              <q-icon name="logout" color="negative" />
            </q-item-section>
            <q-item-section class="text-negative">Logout</q-item-section>
          </q-item>
        </q-list>
        <div class="q-pa-md">
          <GettingStartedChecklist />
        </div>
        <div class="q-px-md q-pb-md">
          <div style="font-size: 11px; color: #64748b;">{{ auth.user?.email }}</div>
          <div style="font-size: 10px; color: #94a3b8; margin-top: 2px;">v{{ appVersion }}</div>
        </div>
      </q-card>
    </q-dialog>
  </q-layout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../store/auth';
import version from '../version';
import GettingStartedChecklist from '../components/GettingStartedChecklist.vue';

const router = useRouter();
const route = useRoute();
const auth = useAuthStore();

const leftDrawerOpen = ref(false);
const showMoreMenu = ref(false);
const currentTab = ref('/');
const windowWidth = ref(window.innerWidth);

const isMobile = computed(() => windowWidth.value < 960);
const isLoginRoute = computed(() => route.path === '/login');
const appVersion = version;

const navItems = [
  { title: 'Dashboard', path: '/dashboard', icon: 'dashboard' },
  { title: 'Budget', path: '/budget', icon: 'savings' },
  { title: 'Transactions', path: '/transactions', icon: 'format_list_bulleted' },
  { title: 'Accounts', path: '/accounts', icon: 'account_balance' },
  { title: 'Reports', path: '/reports', icon: 'trending_up' },
  { title: 'Data Mgmt', path: '/data', icon: 'dataset' },
  { title: 'Settings', path: '/settings', icon: 'manage_accounts' },
];

const bottomBarItems = navItems.slice(0, 4);
const moreMenuItems = navItems.slice(4);


async function onNavClick(item: { title: string; path: string }) {
  await router.push(item.path);
  currentTab.value = item.path;
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
  background: #ffffff;
  color: var(--color-text-primary);
  box-shadow: 2px 0 16px rgba(15, 23, 42, 0.06);
}

.app-drawer__content {
  gap: 0;
}

.app-drawer__scrollable {
  overflow-y: auto;
  overflow-x: hidden;
  min-height: 0;
}

.app-drawer__brand {
  display: flex;
  align-items: center;
  padding: 20px 20px 28px;
  gap: 12px;
}

.app-drawer__brand img {
  width: 32px;
  height: 32px;
  border-radius: 6px;
}

.app-drawer__brand-name {
  font-size: 16px;
  font-weight: 600;
  color: #0f172a;
}

.app-drawer__nav {
  padding: 0 8px;
  background: transparent;
}

.app-drawer__item {
  border-radius: 10px;
  margin-bottom: 2px;
  padding: 10px 12px;
  min-height: 40px;
  color: #64748b;
  background: transparent;
  font-size: 14px;
  font-weight: 500;
  transition: background 0.15s ease, color 0.15s ease;
}

.app-drawer__item:hover {
  background: #f8fafc;
}

.app-drawer__item :deep(.q-item__label),
.app-drawer__item :deep(.q-icon) {
  color: inherit;
}

.app-drawer__item--active {
  background: #eef2ff;
  color: #1d4ed8;
  font-weight: 600;
}

.app-drawer__item--active :deep(.q-icon) {
  color: #1d4ed8;
}

.app-drawer__footer {
  flex-shrink: 0;
  padding: 16px 20px 24px;
}

.app-drawer__email {
  font-size: 11px;
  color: #64748b;
  cursor: pointer;
  line-height: 1.4;
}

.app-drawer__email:hover {
  color: #1d4ed8;
}

.app-drawer__version {
  font-size: 10px;
  color: #94a3b8;
  margin-top: 4px;
}

.text-opacity {
  opacity: 0.7;
}

.mobile-header {
  background: #1d4ed8;
  color: #ffffff;
  padding: 0;
  min-height: 56px;
}

.mobile-header img {
  border-radius: 6px;
}

.mobile-footer {
  background: #ffffff;
  color: var(--color-text-primary);
  padding: 8px 0;
  box-shadow: 0 -2px 8px rgba(15, 23, 42, 0.06);
}

.mobile-tab {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding: 6px 0;
  cursor: pointer;
  color: #6b7280;
}

.mobile-tab--active {
  color: #1d4ed8;
}

.mobile-tab__label {
  font-size: 10px;
  font-weight: 400;
}

.mobile-tab--active .mobile-tab__label {
  font-weight: 600;
}

.more-menu-sheet {
  width: 100%;
  border-radius: var(--radius-lg) var(--radius-lg) 0 0;
}

.more-menu-sheet .q-item {
  min-height: 48px;
  padding: 12px 16px;
}
</style>
