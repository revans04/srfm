import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('layouts/MainLayout.vue'),
    children: [
      { path: '', redirect: '/budget' },
      { path: 'dashboard', component: () => import('pages/DashboardPage.vue'), meta: { title: 'Dashboard' } },
      { path: 'budget', component: () => import('pages/BudgetPage.vue'), meta: { title: 'Budget' } },
      { path: 'transactions', component: () => import('pages/TransactionsPage.vue'), meta: { title: 'Transactions' } },
      { path: 'settings', component: () => import('pages/SettingsPage.vue'), meta: { title: 'Settings' } },
      { path: 'accounts', component: () => import('pages/AccountsPage.vue'), meta: { title: 'Accounts' } },
      { path: 'data', component: () => import('pages/DataPage.vue'), meta: { title: 'Data Mgmt' } },
      { path: 'reports', component: () => import('pages/ReportsPage.vue'), meta: { title: 'Reports' } },
      { path: 'verify-email', component: () => import('pages/VerifyEmailPage.vue'), meta: { title: 'Verify Email' } },
      { path: 'accept-invite', component: () => import('pages/AcceptInvitePage.vue'), meta: { title: 'Accept Invite' } },
    ],
  },
  {
    path: '/login',
    component: () => import('layouts/AuthLayout.vue'),
    children: [{ path: '', component: () => import('pages/LoginPage.vue'), meta: { title: 'Login', mode: 'login' } }],
  },
  {
    // /signup is rendered by LoginPage with meta.mode='signup' so the headline
    // and CTAs read as "create account" — used by AcceptInvitePage when an
    // unauthenticated invitee clicks "Sign Up". Auth itself is Google-only,
    // so the flow is identical to /login under the hood.
    path: '/signup',
    component: () => import('layouts/AuthLayout.vue'),
    children: [{ path: '', component: () => import('pages/LoginPage.vue'), meta: { title: 'Sign Up', mode: 'signup' } }],
  },
  {
    // /setup uses a minimal-chrome layout (no sidebar, no bottom tab bar)
    // so a brand-new user can focus on the seed flow without distractions.
    // Existing users coming via Settings → "Quick setup with starter
    // budget" land here too; the OnboardingLayout's "Sign out" button is
    // also their escape hatch back to the rest of the app via /budget on
    // re-login.
    path: '/setup',
    component: () => import('layouts/OnboardingLayout.vue'),
    children: [{ path: '', component: () => import('pages/SetupPage.vue'), meta: { title: 'Setup' } }],
  },
  { path: '/:catchAll(.*)*', component: () => import('pages/ErrorNotFound.vue'), meta: { title: 'Not Found' } },
];

export default routes;
