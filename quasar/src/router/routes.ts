// src/router/routes.ts
import { RouteRecordRaw } from "vue-router";
import Login from "../pages/LoginView.vue";
import Dashboard from "../pages/DashboardView.vue";
import Transactions from "../pages/TransactionsView.vue";
import SettingsView from "../pages/SettingsView.vue";
import DataView from "../pages/DataView.vue";
import AccountsView from "../pages/AccountsView.vue";
import ReportsView from "../pages/ReportsView.vue";
import VerifyEmail from "../pages/VerifyEmail.vue";
import AcceptInvite from "../pages/AcceptInvite.vue";
import SetupWizard from "../pages/SetupWizard.vue";
import ErrorNotFound from "../pages/ErrorNotFound.vue";

interface RouteMeta {
  requiresAuth?: boolean;
  title?: string;
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
    path: "/verify-email",
    component: VerifyEmail,
    meta: { requiresAuth: true, title: "Steady Rise - Verify Email" },
  },
  {
    path: "/accept-invite",
    component: AcceptInvite,
    meta: { requiresAuth: true, title: "Steady Rise - Accept Invite" },
  },
  {
    path: "/:catchAll(.*)*",
    component: ErrorNotFound,
    meta: { requiresAuth: false, title: "Steady Rise - Not Found" },
  },
];

export default routes;
