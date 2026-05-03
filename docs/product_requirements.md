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
- Provide initial setup flow for family creation, entity setup, and optional account setup (existing wizard handles this).
- Replace the step-by-step wizard with a **guided tour approach** for ongoing onboarding:
  - Auto-trigger contextual prompts for first-time users as they navigate (dismissible permanently).
  - Provide a persistent "Getting Started" checklist/section accessible from the sidebar or settings that users can return to at any time.
  - The guided tour should cover: creating a first budget, understanding entities, importing transactions, setting up goals, and using reconciliation.
  - Include contextual help explaining concepts like "entity" in plain language (e.g., "Most families just need one entity called 'Family Budget'").
- The existing SetupWizardPage must be migrated from Vuetify-era CSS to Quasar design system standards.
- After login, users should land on the **Budget page** (not the Dashboard). The Budget page is the primary workspace where most user actions happen.

## 3) Family and Entity Management
- Support family create/read/update operations for authorized members.
- Support entity create/update/delete within family boundaries.
- Require safe deletion handling for entities with dependent data.
- Support member invite/remove workflows with role checks.
- The Settings > Manage Family/Group page must display a list of current family members with their role (owner/member), join date, and a remove option (owner-only). The current page only shows an invite field with no visibility into existing membership.

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
- Inter-category moves use a first-class transfer transaction type (`transactionType: 'transfer'` with signed splits) so they can be excluded from spending and income aggregates without netting category spend to zero. Goal-funded expenses use a separate model: a standard `Transaction` with `funded_by_goal_id` persisted on the row — the expense counts in the destination category, and the goal derives Goal Spend from the column. Goal funding is *not* a transfer; do not auto-convert goal-funded expenses at save time. (See `AGENTS.md` anti-patterns.)

## 6) Reconciliation
- Support statement-based reconciliation (start/end dates, opening/ending balance).
- Support reconcile finalization that updates transaction statuses and locks statement state.
- Derive statement dates from the selected transactions rather than requiring manual date entry.
- Support unreconcile workflow with restore behavior and audit trail.
- Block finalize when computed delta is non-zero unless explicitly overridden.
- Support batch reconciliation actions against selected transactions.
- Include ignored transactions in reconciliation — ignored means "not matched to a budget transaction" but still part of the bank statement.
- Auto-populate beginning balance from the last reconciled statement's ending balance.
- Display statement history per account with dates, balances, and reconciled status.
- Show matched budget transaction category on the account register for cleared/matched imported transactions.
- Support unmatch workflow to disconnect an imported transaction from its linked budget transaction (blocked while reconciled).

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
- Display current goal balance (total contributions minus total spends) as a first-class value — the "what's left" view.
- Show contribution history alongside spend history so users can trace how a bonus or deposit was allocated and ultimately used.
- Support linking a goal spend to a specific budget category so a user can answer "how much of this goal went to uniforms."

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
- **Exclude intra-budget transfers from spending and income totals in all reports.** Transfers between categories (e.g., moving money from a savings fund to pay for uniforms) must not appear as income in the destination category, as this causes category spending to net to zero and hides real expenditure in annual and YoY views.
- **Distinguish real income from fund/goal draws.** Income lines in category and YoY reports must reflect only external income; draws from savings goals or fund categories must be reported separately or excluded from spending totals so actual spend is visible.
- Enable category-level spending history that accurately answers "how much did we spend on uniforms this year" independent of how those purchases were funded.

## 11) Data Management (Import/Export)
- Support structured import workflow: Upload -> Mapping -> Preview -> Commit.
- Support imports for entities, budgets/transactions, accounts/snapshots, and bank transactions.
- Validate required fields/types before commit; no partial writes on failed validation.
- Keep import actions traceable to user/family context.
- Support family-scoped export bundles containing core finance domains.

## 12) Operational and UX Requirements
- Require confirmation for destructive operations and high-impact batch operations. Confirmation dialogs must name the affected item(s) and state the impact. Destructive buttons must be visually distinct (not plain text links).
- Surface clear, actionable API/UI error messages and empty states. Empty states must include guidance copy and a link/button to the next action. Loading and error states must never appear simultaneously.
- Provide visible success feedback (toast notification) for all write operations — save, create, delete, import. Silent success is not acceptable.
- Maintain performance for large datasets (thin endpoints, server-side aggregation where needed).
- Preserve month/date ordering conventions (`YYYY-MM`, `YYYY-MM-DD`) and deterministic sorting.
- Accounting terminology (Reconciled, Cleared, Uncleared, Budget Register, Account Register) must be accompanied by contextual help via tooltips on hover/tap for non-expert users. Labels remain as-is; tooltips bridge the knowledge gap. **Decision: keep current labels, add tooltips (not rename).**
- Remove the Transactions page subtitle ("Monitor budgets and accounts side by side") entirely — the page title "Transactions" is sufficient. **Decision: no subtitle needed.**
- The default post-login landing page must be the **Budget page**, not the Dashboard. **Decision: Budget is the primary workspace.**
- All pages must have a proper heading hierarchy starting with `h1` for the page title.

## 13) Mobile Experience
- Treat mobile as a first-class target, not a fallback. The app is expected to be used on phones at least 50% of the time, primarily for entering transactions on the go. Users migrating from EveryDollar and Mint expect a polished, mobile-native experience — mobile quality is a competitive differentiator.
- All core workflows — adding a transaction, checking a category balance, viewing the current month's budget — must be fully usable on a phone without horizontal scrolling, tiny tap targets, or layout breakage.
- Tap targets must meet minimum size guidelines (44×44px) for all interactive elements.
- Forms (especially transaction entry) must be optimized for mobile keyboard input: correct input types (`number`, `date`, etc.), logical tab order, and minimal required fields visible without scrolling.
- Navigation must be reachable with one thumb; avoid patterns that require precise interaction or hover states.
- Mobile navigation must use a single primary pattern. The sidebar drawer must not auto-open on page navigation at mobile breakpoints. The bottom tab bar must provide access to all major sections — currently missing Reports, Data Mgmt, and Settings, which must be reachable via a "More" item or equivalent.
- Data tables (Transactions, Accounts) must use a responsive mobile pattern — card views, expandable rows, or scrollable containers with visible affordance. Simply truncating columns is not acceptable.
- Base font size must be at least 16px on mobile to prevent iOS auto-zoom on input focus.
- The viewport meta tag must not disable user zoom (`user-scalable=no`, `maximum-scale=1`). This is a WCAG 2.1 AA requirement.
- Responsive layout must be explicitly tested at common phone widths (375px, 390px, 430px) and not rely solely on Quasar's default breakpoints without validation.

## 14) Competitive Positioning
- The application intends to replace EveryDollar, Mint (discontinued), Quicken, and Excel for families tracking budgets, transactions, accounts, and net worth.
- The user base spans non-expert family budget managers (majority) and accountants/bookkeepers (minority but important).
- Key differentiators vs. EveryDollar: multi-entity support, reconciliation workflows, net worth tracking, detailed reporting.
- Key differentiators vs. Quicken: modern web-based UI, family collaboration, mobile-first accessibility.
- Key gaps vs. competitors: mobile experience polish (EveryDollar/Mint were mobile-first), guided onboarding (EveryDollar offers step-by-step budget setup), plain-language UX (EveryDollar avoids accounting terminology).
- To capture the family-budget market, the app must offer: (1) a polished mobile experience, (2) plain-language alternatives for accounting terminology, (3) guided onboarding for first-time users, and (4) simplified views alongside the full data-dense power-user views.
