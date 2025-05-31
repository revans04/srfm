/** router/index.ts */
import { createRouter, createWebHistory, RouteRecordRaw } from "vue-router";
import { auth } from "../firebase/index";
import Login from "../views/LoginView.vue";
import Dashboard from "../views/DashboardView.vue";
import Transactions from "../views/TransactionsView.vue";
import SettingsView from "../views/SettingsView.vue";
import DataView from "../views/DataView.vue";
import AccountsView from "../views/AccountsView.vue";
import ReportsView from "../views/ReportsView.vue";
import VerifyEmail from "../views/VerifyEmail.vue";
import AcceptInvite from "../views/AcceptInvite.vue";
import SetupWizard from "../views/SetupWizard.vue";
import { useFamilyStore } from "@/store/family";

interface RouteMeta {
  requiresAuth?: boolean;
  title?: string; // Optional title
}

const routes: RouteRecordRaw[] = [
  {
    path: "/",
    component: Dashboard,
    meta: { requiresAuth: true, title: "Steady Rise - Budget" },
  },
  {
    path: "/setup",
    component: SetupWizard,
    meta: { requiresAuth: true, title: "Steady Rise - Setup" },
  },
  {
    path: "/transactions",
    component: Transactions,
    meta: { requiresAuth: true, title: "Steady Rise - Transactions" },
  },
  {
    path: "/settings",
    name: "Settings",
    component: SettingsView,
    meta: { requiresAuth: true, title: "Steady Rise - Settings" },
  },
  {
    path: "/accounts",
    component: AccountsView,
    meta: { requiresAuth: true, title: "Steady Rise - Accounts" },
  },
  {
    path: "/data",
    component: DataView,
    meta: { requiresAuth: true, title: "Steady Rise - Data Management" },
  },
  {
    path: "/reports",
    component: ReportsView,
    meta: { requiresAuth: true, title: "Steady Rise - Reports" },
  },
  {
    path: "/login",
    component: Login,
    meta: { requiresAuth: false, title: "Steady Rise - Login" },
  },
  {
    path: '/verify-email',
    component: VerifyEmail,
    meta: { requiresAuth: true, title: "Steady Rise - Verify Email" },
  },
  {
    path: '/accept-invite',
    component: AcceptInvite,
    meta: { requiresAuth: true, title: "Steady Rise - Accept Invite" },
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

// Promise to wait for Firebase auth state to initialize
let isAuthInitialized = false;

const authInitialized = new Promise<void>((resolve) => {
  const unsubscribe = auth.onAuthStateChanged((user) => {
    if (!isAuthInitialized) {
      isAuthInitialized = true;
      unsubscribe(); // Unsubscribe after first call
      resolve();
    }
  });
});

router.beforeEach(async (to, from, next) => {
  await authInitialized;
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth);
  const user = auth.currentUser;
  console.log(to.path);
  if (requiresAuth && !user) {
    next("/login");
  } else if (user && to.path === "/login") {
    next("/");
  } else if (user && to.path === "/setup") {
    next();
  } else {
    const family = await useFamilyStore().getFamily();
    console.log(family);
    if (!family || !family.entities || family.entities.length == 0) {
      console.log(family);
      next("/setup");
      return;
    }
    // Set document.title based on route meta with type assertion
    const meta = to.meta as RouteMeta;
    document.title = meta.title ?? "Steady Rise"; // Use nullish coalescing for safety
    next();
  }
});

export default router;
