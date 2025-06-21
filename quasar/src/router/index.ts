import { defineRouter } from '#q-app/wrappers';
import { createMemoryHistory, createRouter, createWebHashHistory, createWebHistory } from 'vue-router';
import routes from './routes';
import { useAuthStore } from '../store/auth';

export default defineRouter(function (/* { store, ssrContext } */) {
  const createHistory = process.env.SERVER ? createMemoryHistory : process.env.VUE_ROUTER_MODE === 'history' ? createWebHistory : createWebHashHistory;

  const Router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    routes,
    history: createHistory(process.env.VUE_ROUTER_BASE),
  });

  Router.beforeEach(async (to, from, next) => {
    const auth = useAuthStore();
    console.log('Route guard: Navigating to', to.path);

    // Define routes that require authentication
    const protectedRoutes = ['/', '/setup', '/transactions', '/settings', '/accounts', '/data', '/reports', '/verify-email', '/accept-invite'];

    if (protectedRoutes.includes(to.path)) {
      console.log('Route guard: Protected route, checking auth');
      try {
        const user = await auth.initializeAuth();
        if (user) {
          console.log('Route guard: User authenticated', user.uid);
          next();
        } else {
          console.log('Route guard: No user, redirecting to /login');
          next('/login');
        }
      } catch (error) {
        console.error('Route guard: Auth error', error);
        next('/login');
      }
    } else {
      console.log('Route guard: Public route, allowing access');
      next();
    }
  });

  return Router;
});
