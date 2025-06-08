// src/router/routes.ts
import { RouteRecordRaw } from "vue-router";
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
];

export default routes;
