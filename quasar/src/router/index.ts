// src/router/index.ts
import { createRouter, createWebHistory } from "vue-router";
import routes from "./routes";
import { auth } from "../firebase/index";
import { useFamilyStore } from "@/store/family";
import type { Router } from "vue-router";

interface RouteMeta {
  requiresAuth?: boolean;
  title?: string;
}

// Create the router instance
const router: Router = createRouter({
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

// Navigation guard
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
    if (!family || !family.entities || family.entities.length === 0) {
      console.log(family);
      next("/setup");
      return;
    }
    // Set document.title based on route meta
    const meta = to.meta as RouteMeta;
    document.title = meta.title ?? "Steady Rise";
    next();
  }
});

export default router;
