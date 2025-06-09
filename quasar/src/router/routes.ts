import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('layouts/MainLayout.vue'),
    children: [
      { path: '', component: () => import('pages/DashboardPage.vue') },
      { path: 'setup', component: () => import('pages/SetupWizardPage.vue') },
      { path: 'transactions', component: () => import('pages/TransactionsPage.vue') },
      { path: 'settings', component: () => import('pages/SettingsPage.vue') },
      { path: 'accounts', component: () => import('pages/AccountsPage.vue') },
      { path: 'data', component: () => import('pages/DataPage.vue') },
      { path: 'reports', component: () => import('pages/ReportsPage.vue') },
      { path: 'verify-email', component: () => import('pages/VerifyEmailPage.vue') },
      { path: 'accept-invite', component: () => import('pages/AcceptInvitePage.vue') },
    ],
  },
  { path: '/login', component: () => import('pages/LoginPage.vue') },
  { path: '/:catchAll(.*)*', component: () => import('pages/ErrorNotFound.vue') },
];

export default routes;
