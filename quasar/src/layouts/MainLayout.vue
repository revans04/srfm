<template>
  <q-layout view="lHh Lpr lFf">
    <!-- Desktop Navigation Drawer -->
    <q-drawer app permanent v-if="!isMobile && !isLoginRoute">
      <div class="text-center bg-primary">
        <img alt="Steady Rise Financial Management" src="../assets/family-funds-sm.png" width="100" />
      </div>
      <q-list>
        <q-separator />
        <q-item
          v-for="item in navItems.filter((n) => n.desktop)"
          :key="item.title"
          :to="item.path"
          :title="item.title"
          :prepend-icon="item.icon"
          :active="router.currentRoute.value.path === item.path"
        ></q-item>
      </q-list>
      <template v-slot:append>
        <q-item disabled class="version-item">
          <q-item-section class="text-caption text-center">
            {{ `Version: ${appVersion}` }}
          </q-item-section>
        </q-item>
        <q-separator />
        <q-item :title="userEmail ? userEmail : ''" subtitle="Logout" @click="signOut">
          <template v-slot:prepend>
            <q-avatar size="36" class="mr-2">
              <q-img v-if="user" :src="avatarSrc" alt="User Avatar"></q-img>
            </q-avatar>
          </template>
        </q-item>
      </template>
    </q-drawer>

    <!-- Main Content -->
    <q-page-container :class="isMobile ? 'bg-primary' : 'bg-light'">
      <router-view />
    </q-page-container>

    <!-- Mobile Bottom Navigation -->
    <q-footer v-if="isMobile && !isLoginRoute">
      <q-tabs no-caps active-color="primary" indicator-color="transparent" class="text-grey-8" v-model="currentTab">
        <q-btn
          v-for="item in navItems.filter((i) => i.mobile)"
          :key="item.title"
          :value="item.path"
          @click="item.title === 'Logout' ? promptSignOut() : router.push(item.path)"
        >
          <q-icon>{{ item.icon }}</q-icon>
          <span style="font-size: 5pt">{{ item.title }}</span>
        </q-btn>
      </q-tabs>
    </q-footer>

    <!-- Sign-Out Confirmation Dialog -->
    <q-dialog v-model="showSignOutDialog" max-width="400">
      <q-card>
        <q-card-section class="bg-primary py-3">
          <span class="text-white">Confirm Logout</span>
        </q-card-section>
        <q-card-section class="pt-4"> Are you sure you want to logout? </q-card-section>
        <q-card-actions>
          <q-space />
          <q-btn color="grey" variant="text" @click="showSignOutDialog = false">Cancel</q-btn>
          <q-btn color="primary" variant="flat" @click="signOut">Confirm</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Onboarding Modal -->
    <q-dialog v-model="showOnboarding" persistent max-width="500">
      <group-naming-form @family-created="completeOnboarding" />
    </q-dialog>
  </q-layout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useQuasar } from 'quasar';
import type { User } from 'firebase/auth';
import { useRouter } from 'vue-router';
import version from '../version';
import GroupNamingForm from '../components/GroupNamingForm.vue';
import { useAuthStore } from '../store/auth';
import { useFamilyStore } from '../store/family';

const router = useRouter();
const auth = useAuthStore();
const familyStore = useFamilyStore();
const $q = useQuasar();
const currentTab = ref('/');

// Reactive state
const user = ref<User | null>(null);
const avatarSrc = ref<string>('https://via.placeholder.com/36');
const showOnboarding = ref(false);
const showSignOutDialog = ref(false); // New dialog state

// Computed properties
const isLoginRoute = computed(() => router.currentRoute.value.path === '/login');
const isMobile = computed(() => $q.screen.lt.md);
const userEmail = computed(() => user.value?.email ?? 'Guest');
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

// Functions
async function signOut() {
  try {
    await auth.logout();
    void router.push('/login');
  } catch (error) {
    console.error('Sign-out error:', error);
  } finally {
    showSignOutDialog.value = false; // Close dialog after sign-out
  }
}

function promptSignOut() {
  showSignOutDialog.value = true; // Show confirmation dialog
}

function completeOnboarding(_familyId: string) {
  showOnboarding.value = false;
}

// Lifecycle hooks
onMounted(async () => {
  auth.initializeAuth();
  if (auth.user) {
    try {
      const family = await familyStore.loadFamily(auth.user.uid);
      if (!family && router.currentRoute.value.path !== '/login') {
        showOnboarding.value = true; // Show modal if no family
      }

      avatarSrc.value = localStorage.getItem('userAvatar') || auth.user.photoURL || 'https://via.placeholder.com/36';
      if (!localStorage.getItem('userAvatar') && auth.user.photoURL) {
        localStorage.setItem('userAvatar', auth.user.photoURL);
      }
    } catch (ex: any) {
      console.warn('Failed to load family', ex.message);
    }
  } else {
    avatarSrc.value = 'https://via.placeholder.com/36';
    localStorage.removeItem('userAvatar');
    void router.push('/login'); // Redirect to login if no user
  }

});


watch(
  () => auth.user,
  (user) => {
    if (!user) void router.push('/login');
  },
);
</script>

<style scoped>
.v-main {
  min-height: 100vh;
}
</style>
