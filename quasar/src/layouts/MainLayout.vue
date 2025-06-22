<template>
  <q-layout view="lHh Lpr lFf">

    <q-drawer
      v-model="leftDrawerOpen"
      show-if-above
      bordered
      side="left"
      :width="250"
      class="column"
    >
      <div class="column justify-between fit">
        <div>
          <div class="text-center q-pa-md">
            <img src="../assets/family-funds-sm.png" alt="Steady Rise Financial Management" width="100" />
          </div>
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
          </q-list>
        </div>
        <q-list>
          <q-item disabled>
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
      </div>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>

    <q-footer v-if="isMobile && !isLoginRoute" class="bg-primary text-white">
      <SwiperBottomNavigation v-model="currentTab" class="bg-primary text-white">
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
      </SwiperBottomNavigation>
    </q-footer>
  </q-layout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import SwiperBottomNavigation from 'bottom-navigation-vue';
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
  { title: 'Budget', path: '/', icon: 'savings', desktop: true, mobile: true },
  { title: 'Transactions', path: '/transactions', icon: 'format_list_bulleted', desktop: true, mobile: true },
  { title: 'Accounts', path: '/accounts', icon: 'account_balance', desktop: true, mobile: true },
  { title: 'Reports', path: '/reports', icon: 'trending_up', desktop: true, mobile: false },
  { title: 'Data Mgmt', path: '/data', icon: 'dataset', desktop: true, mobile: false },
  { title: 'Settings', path: '/settings', icon: 'manage_accounts', desktop: true, mobile: true },
  { title: 'Logout', path: '', icon: 'logout', desktop: false, mobile: true },
];


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
.q-drawer {
  transition: transform 0.3s ease-in-out;
}
</style>
