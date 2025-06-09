<!-- src/layouts/MainLayout.vue -->
<template>
  <q-layout view="lHr LpR lFr">
    <!-- Desktop Navigation Drawer -->
    <q-drawer
      v-if="!isMobile && !isLoginRoute"
      v-model="drawer"
      show-if-above
      :width="200"
      :breakpoint="960"
      side="left"
      bordered
    >
      <div class="q-pa-md text-center bg-primary">
        <img
          alt="Steady Rise Financial Management"
          src="@/assets/family-funds-sm.png"
          style="width: 100px"
        />
      </div>
      <q-list>
        <q-item
          v-for="item in navItems.filter((n) => n.desktop)"
          :key="item.title"
          :to="item.path"
          clickable
          :active="router.currentRoute.value.path === item.path"
        >
          <q-item-section avatar>
            <q-icon :name="item.icon" />
          </q-item-section>
          <q-item-section>
            {{ item.title }}
          </q-item-section>
        </q-item>
      </q-list>
      <div class="absolute-bottom">
        <q-item class="text-caption text-center">
          {{ `Version: ${appVersion}` }}
        </q-item>
        <q-separator />
        <q-item clickable @click="signOut">
          <q-item-section avatar>
            <q-avatar size="36px" class="q-mr-sm">
              <img v-if="user" :src="avatarSrc" alt="User Avatar" />
            </q-avatar>
          </q-item-section>
          <q-item-section>
            {{ userEmail || '' }}
          </q-item-section>
          <q-item-section side>
            <q-icon name="mdi-logout" />
          </q-item-section>
        </q-item>
      </div>
    </q-drawer>

    <!-- Main Content -->
    <q-page-container>
      <q-page :class="isMobile ? 'bg-primary' : 'bg-light'">
        <router-view />
      </q-page>
    </q-page-container>

    <!-- Mobile Bottom Navigation -->
    <q-footer v-if="isMobile && !isLoginRoute" bordered class="bg-white">
      <q-tabs
        v-model="currentTab"
        dense
        class="text-grey"
        active-color="primary"
        indicator-color="primary"
        align="justify"
      >
        <q-tab
          v-for="item in navItems.filter((i) => i.mobile)"
          :key="item.title"
          :name="item.path"
          :icon="item.icon"
          :label="item.title"
          :style="{ fontSize: '5pt' }"
          @click="item.title === 'Logout' ? promptSignOut() : router.push(item.path)"
        />
      </q-tabs>
    </q-footer>

    <!-- Sign-Out Confirmation Dialog -->
    <q-dialog v-model="showSignOutDialog" persistent>
      <q-card>
        <q-card-section class="bg-primary text-white">
          <div class="text-h6">Confirm Logout</div>
        </q-card-section>
        <q-card-section> Are you sure you want to logout? </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey" @click="showSignOutDialog = false" />
          <q-btn flat label="Confirm" color="primary" @click="signOut" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Onboarding Modal -->
    <q-dialog v-model="showOnboarding" persistent>
      <group-naming-form @family-created="completeOnboarding" />
    </q-dialog>
  </q-layout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { auth } from '../firebase/index';
import { User } from 'firebase/auth';
import { useRouter } from 'vue-router';
import version from '@/version';
import GroupNamingForm from '@/components/GroupNamingForm.vue';
import { useAuthStore } from '../store/auth';
import { useFamilyStore } from '../store/family';

const router = useRouter();
const authStore = useAuthStore();
const familyStore = useFamilyStore();
const currentTab = ref('/');
const drawer = ref(true);

// Reactive state
const user = ref<User | null>(null);
const avatarSrc = ref<string>('https://via.placeholder.com/36');
const windowWidth = ref(window.innerWidth);
const showOnboarding = ref(false);
const showSignOutDialog = ref(false);

// Computed properties
const isLoginRoute = computed(() => router.currentRoute.value.path === '/login');
const isMobile = computed(() => windowWidth.value < 960);
const userEmail = computed(() => user.value?.email ?? 'Guest');
const appVersion = version;

const navItems = [
  { title: 'Budget', path: '/', icon: 'mdi-turtle', desktop: true, mobile: true },
  {
    title: 'Transactions',
    path: '/transactions',
    icon: 'mdi-format-list-bulleted',
    desktop: true,
    mobile: true,
  },
  { title: 'Accounts', path: '/accounts', icon: 'mdi-bank-outline', desktop: true, mobile: true },
  { title: 'Reports', path: '/reports', icon: 'mdi-trending-up', desktop: true, mobile: false },
  {
    title: 'Data Mgmt',
    path: '/data',
    icon: 'mdi-database-export-outline',
    desktop: true,
    mobile: false,
  },
  {
    title: 'Settings',
    path: '/settings',
    icon: 'mdi-account-group-outline',
    desktop: true,
    mobile: true,
  },
  { title: 'Logout', path: '', icon: 'mdi-logout', desktop: false, mobile: true },
];

// Functions
async function signOut() {
  try {
    await auth.signOut();
    router.push('/login');
  } catch (error) {
    console.error('Sign-out error:', error);
  } finally {
    showSignOutDialog.value = false;
  }
}

function promptSignOut() {
  showSignOutDialog.value = true;
}

function handleResize() {
  windowWidth.value = window.innerWidth;
}

function completeOnboarding(familyId: string) {
  showOnboarding.value = false;
}

// Lifecycle hooks
onMounted(async () => {
  authStore.initializeAuth();
  auth.onAuthStateChanged(async (firebaseUser) => {
    user.value = firebaseUser;
    if (firebaseUser) {
      try {
        const family = await familyStore.loadFamily(firebaseUser.uid);
        if (!family && router.currentRoute.value.path !== '/login') {
          showOnboarding.value = true;
        }
      } catch (ex: any) {
        console.warn('Failed to load family', ex.message);
      }
      avatarSrc.value =
        localStorage.getItem('userAvatar') ||
        firebaseUser.photoURL ||
        'https://via.placeholder.com/36';
      if (!localStorage.getItem('userAvatar') && firebaseUser.photoURL) {
        localStorage.setItem('userAvatar', firebaseUser.photoURL);
      }
    } else {
      avatarSrc.value = 'https://via.placeholder.com/36';
      localStorage.removeItem('userAvatar');
      router.push('/login');
    }
  });
  window.addEventListener('resize', handleResize);
});

onUnmounted(() => {
  window.removeEventListener('resize', handleResize);
});

watch(
  () => authStore.user,
  (user) => {
    if (!user) router.push('/login');
  },
);
</script>

<style scoped>
.q-page {
  min-height: 100vh;
}
</style>
