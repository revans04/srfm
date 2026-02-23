# SRFM Product Requirements

## 1) Platform, Security, and Tenancy
- Require Firebase ID token authentication for all protected API endpoints.
- Enforce family-scoped authorization for family, entity, budget, account, goal, statement, and import resources.
- Enforce family ownership/member rules:
  - One family owner.
  - Owner cannot be removed.
  - Invite-based membership with expiring tokens.
- Keep entity scoping as a first-class constraint for budgets, goals, and related data.

## 2) Authentication and Onboarding
- Support Google sign-in and server-issued Firebase custom token login.
- Persist authenticated session state.
- Ensure user profile exists on first login without destructive overwrite.
- Support email verification flow with idempotent verification endpoint.
- Support invite acceptance flow, including redirect-to-login and return.
- Provide onboarding wizard for family creation, entity setup, and optional account setup.

## 3) Family and Entity Management
- Support family create/read/update operations for authorized members.
- Support entity create/update/delete within family boundaries.
- Require safe deletion handling for entities with dependent data.
- Support member invite/remove workflows with role checks.

## 4) Budget Management
- Support budget CRUD scoped to `entity_id` and `month` (`YYYY-MM`).
- Enforce unique `(entity_id, month)` budget constraint.
- Support category groups and budget categories CRUD.
- Support fund categories with carryover behavior.
- Perform carryover calculations server-side and keep results auditable.
- Support budget duplication to a target month with recurring transaction options.
- Support deterministic budget merge behavior.
- Track budget edit history (timestamp, user, action).

## 5) Transaction Management
- Support transaction CRUD for budget-scoped transactions.
- Support transaction filtering/search by date, amount, status, account/entity, and text.
- Preserve transaction integrity with valid budget/category relationships.
- Support soft delete for auditability.
- Support batch transaction actions with explicit confirmation and affected-count display.
- Support imported transaction storage and matching workflow to budget transactions.
- Maintain transaction statuses (`U`, `C`, `R`) with reconciliation-safe behavior.

## 6) Reconciliation
- Support statement-based reconciliation (start/end dates, opening/ending balance).
- Support reconcile finalization that updates transaction statuses and locks statement state.
- Support unreconcile workflow with restore behavior and audit trail.
- Block finalize when computed delta is non-zero unless explicitly overridden.
- Support batch reconciliation actions against selected transactions.

## 7) Account and Snapshot Management
- Support account CRUD across `Bank`, `CreditCard`, `Investment`, `Property`, `Loan`.
- Group/filter accounts by type in UI.
- Support snapshot create/delete for net worth history.
- Enforce referential integrity across accounts, snapshots, and snapshot-account rows.
- Prevent unsafe account deletion when referenced by historical or transactional data.

## 8) Goal Tracking
- Support goal CRUD (including archive) scoped to entity.
- Support contribution and spend entries that immediately update progress.
- Enforce non-negative targets and positive contribution/spend values.
- Preserve historical records when editing or archiving goals.

## 9) Merchant Management
- Support merchant list/add/rename/merge/delete within budget/entity scope.
- Prevent case-insensitive duplicate merchant records.
- Preserve historical transaction relationships during merchant rename/merge.
- Prefer frequently used merchants in selection UX (usage count).

## 10) Reporting and Dashboard
- Provide dashboard summaries scoped by selected entity and month.
- Keep initial dashboard payload thin; load heavy details on demand.
- Provide report filtering by entity, budget(s), groups, and categories.
- Provide Monthly Overview, Register Report, YoY, and Net Worth reporting views.
- Ensure report aggregates reconcile with underlying budget/transaction/account data.
- Provide drill-down from summary views to detailed transaction context.

## 11) Data Management (Import/Export)
- Support structured import workflow: Upload -> Mapping -> Preview -> Commit.
- Support imports for entities, budgets/transactions, accounts/snapshots, and bank transactions.
- Validate required fields/types before commit; no partial writes on failed validation.
- Keep import actions traceable to user/family context.
- Support family-scoped export bundles containing core finance domains.

## 12) Operational and UX Requirements
- Require confirmation for destructive operations and high-impact batch operations.
- Surface clear, actionable API/UI error messages and empty states.
- Maintain performance for large datasets (thin endpoints, server-side aggregation where needed).
- Preserve month/date ordering conventions (`YYYY-MM`, `YYYY-MM-DD`) and deterministic sorting.
