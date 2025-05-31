<template>
  <v-app>
    <!-- Desktop Navigation Drawer -->
    <v-navigation-drawer app permanent v-if="!isMobile && !isLoginRoute">
      <div class="text-center bg-primary">
        <img alt="Steady Rise Financial Management" src="./assets/family-funds-sm.png" width="100" />
      </div>
      <v-list>
        <v-divider></v-divider>
        <v-list-item
          v-for="item in navItems.filter((n) => n.desktop)"
          :key="item.title"
          :to="item.path"
          :title="item.title"
          :prepend-icon="item.icon"
          :active="router.currentRoute.value.path === item.path"
        ></v-list-item>
      </v-list>
      <template v-slot:append>
        <v-list-item disabled class="version-item">
          <v-list-item-title class="text-caption text-center">
            {{ `Version: ${appVersion}` }}
          </v-list-item-title>
        </v-list-item>
        <v-divider></v-divider>
        <v-list-item :title="userEmail ? userEmail : ''" subtitle="Logout" @click="signOut">
          <template v-slot:prepend>
            <v-avatar size="36" class="mr-2">
              <v-img v-if="user" :src="avatarSrc" alt="User Avatar"></v-img>
            </v-avatar>
          </template>
        </v-list-item>
      </template>
    </v-navigation-drawer>

    <!-- Main Content -->
    <v-main>
      <v-container fluid :class="isMobile ? 'bg-primary' : 'bg-light'">
        <router-view />
      </v-container>
    </v-main>

    <!-- Mobile Bottom Navigation -->
    <v-bottom-navigation v-if="isMobile && !isLoginRoute" v-model="currentTab" app>
      <v-btn
        v-for="item in navItems.filter((i) => i.mobile)"
        :key="item.title"
        :value="item.path"
        @click="item.title === 'Logout' ? promptSignOut() : router.push(item.path)"
      >
        <v-icon>{{ item.icon }}</v-icon>
        <span style="font-size: 5pt">{{ item.title }}</span>
      </v-btn>
    </v-bottom-navigation>

    <!-- Sign-Out Confirmation Dialog -->
    <v-dialog v-model="showSignOutDialog" max-width="400">
      <v-card>
        <v-card-title class="bg-primary py-3">
          <span class="text-white">Confirm Logout</span>
        </v-card-title>
        <v-card-text class="pt-4"> Are you sure you want to logout? </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showSignOutDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="flat" @click="signOut">Confirm</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Onboarding Modal -->
    <v-dialog v-model="showOnboarding" persistent max-width="500">
      <group-naming-form @family-created="completeOnboarding" />
    </v-dialog>
  </v-app>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from "vue";
import { auth } from "./firebase/index";
import { User } from "firebase/auth";
import { useRouter } from "vue-router";
import version from "./version";
import GroupNamingForm from "./components/GroupNamingForm.vue";
import { useAuthStore } from "./store/auth";
import { useFamilyStore } from "./store/family";

const router = useRouter();
const authStore = useAuthStore();
const familyStore = useFamilyStore();
const currentTab = ref("/");

// Reactive state
const user = ref<User | null>(null);
const avatarSrc = ref<string>("https://via.placeholder.com/36");
const windowWidth = ref(window.innerWidth);
const showOnboarding = ref(false);
const showSignOutDialog = ref(false); // New dialog state

// Computed properties
const isLoginRoute = computed(() => router.currentRoute.value.path === "/login");
const isMobile = computed(() => windowWidth.value < 960);
const userEmail = computed(() => user.value?.email ?? "Guest");
const appVersion = version;

const navItems = [
  { title: "Budget", path: "/", icon: "mdi-turtle", desktop: true, mobile: true },
  { title: "Transactions", path: "/transactions", icon: "mdi-format-list-bulleted", desktop: true, mobile: true },
  { title: "Accounts", path: "/accounts", icon: "mdi-bank-outline", desktop: true, mobile: true },
  { title: "Reports", path: "/reports", icon: "mdi-trending-up", desktop: true, mobile: false },
  { title: "Data Mgmt", path: "/data", icon: "mdi-database-export-outline", desktop: true, mobile: false },
  { title: "Settings", path: "/settings", icon: "mdi-account-group-outline", desktop: true, mobile: true },
  { title: "Logout", path: "", icon: "mdi-logout", desktop: false, mobile: true },
];

// Functions
async function signOut() {
  try {
    await auth.signOut();
    router.push("/login");
  } catch (error) {
    console.error("Sign-out error:", error);
  } finally {
    showSignOutDialog.value = false; // Close dialog after sign-out
  }
}

function promptSignOut() {
  showSignOutDialog.value = true; // Show confirmation dialog
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
      // Check if user has a family
      try {
        const family = await familyStore.loadFamily(firebaseUser.uid);
        if (!family && router.currentRoute.value.path !== "/login") {
          showOnboarding.value = true; // Show modal if no family
        }
      } catch (ex: any) {
        console.warn("Failed to load family", ex.message);
      }
      avatarSrc.value = localStorage.getItem("userAvatar") || firebaseUser.photoURL || "https://via.placeholder.com/36";
      if (!localStorage.getItem("userAvatar") && firebaseUser.photoURL) {
        localStorage.setItem("userAvatar", firebaseUser.photoURL);
      }
    } else {
      avatarSrc.value = "https://via.placeholder.com/36";
      localStorage.removeItem("userAvatar");
      router.push("/login"); // Redirect to login if no user
    }
  });
  window.addEventListener("resize", handleResize);
});

onUnmounted(() => {
  window.removeEventListener("resize", handleResize);
});

watch(
  () => authStore.user,
  (user) => {
    if (!user) router.push("/login");
  }
);
</script>

<style scoped>
.v-main {
  min-height: 100vh;
}
</style>
