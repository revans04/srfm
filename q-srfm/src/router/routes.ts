import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('layouts/MainLayout.vue'),
    children: [
      { path: '', component: () => import('pages/DashboardPage.vue'), meta: { title: 'Dashboard' } },
      { path: 'budget', component: () => import('pages/BudgetPage.vue'), meta: { title: 'Budget' } },
      { path: 'setup', component: () => import('pages/SetupWizardPage.vue'), meta: { title: 'Setup' } },
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
    children: [{ path: '', component: () => import('pages/LoginPage.vue'), meta: { title: 'Login' } }],
  },
  { path: '/:catchAll(.*)*', component: () => import('pages/ErrorNotFound.vue'), meta: { title: 'Not Found' } },
];

export default routes;
