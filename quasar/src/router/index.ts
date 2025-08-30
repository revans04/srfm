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

    const isLoginRoute = to.path === '/login' || to.path.startsWith('/login/');

    // Allow login route; if already authenticated, redirect to dashboard
    if (isLoginRoute) {
      try {
        const user = await auth.initializeAuth();
        if (user) {
          console.log('Route guard: Already authenticated, redirecting from /login to /');
          return next('/');
        }
      } catch (error) {
        console.warn('Route guard: Auth check failed on login route', error);
      }
      console.log('Route guard: Public login route, allowing access');
      return next();
    }

    // All other routes require auth
    console.log('Route guard: Protected route, checking auth');
    try {
      const user = await auth.initializeAuth();
      if (user) {
        console.log('Route guard: User authenticated', user.uid);
        return next();
      }
      console.log('Route guard: No user, redirecting to /login');
      return next({ path: '/login', query: { redirect: to.fullPath } });
    } catch (error) {
      console.error('Route guard: Auth error', error);
      return next('/login');
    }
  });

  return Router;
});
