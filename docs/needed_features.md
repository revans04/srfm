# SRFM Needed Features (New / Requested)

## 1) Security and Access Gaps
- Enforce resource-level authorization consistently on all budget and goal endpoints (membership checks for every `budgetId` / `entityId`).
- Add null-safe claim handling and hardened error paths in auth filter/token parsing.
- Standardize owner/admin checks for high-impact admin operations.

## 2) Reconciliation and Statements Completion
- ~~Implement missing statement write flows end-to-end~~ ✅ Done — finalize, unreconcile, statement history, and auto-populated beginning balance are implemented.
- ~~Statement delete and unreconcile behavior with full DB updates~~ ✅ Done.
- Guarantee atomic reconcile operations where multiple tables/statuses are updated.
- Consider adding a "notes" field to statements for user-facing annotations (e.g., "Feb statement, adjusted for returned check").
- Consider statement-level reporting: show which transactions belong to each reconciled statement for audit purposes.

## 3) API/Frontend Contract Alignment
- Resolve route and shape drift between controllers and `q-srfm/src/dataAccess.ts`.
- Align invite acceptance navigation path with existing frontend routes.
- Normalize endpoint naming conventions (`/api/budget` vs `/api/budgets`) and DTO field casing.

## 4) Merchant Domain Completion
- Implement first-class merchant APIs and persistence where currently implied by docs/UX but not fully realized.
- Add merge/rename side effects that correctly update linked transactions.
- Add usage-count maintenance jobs/logic for ranking merchants in UI.

## 5) Reporting Backend Maturity
- Add/complete report endpoints for Monthly Overview, Register, YoY, and Net Worth.
- Move heavy report calculations to server-side aggregation with performance-aware queries.
- Add consistency checks between dashboard tiles and report totals.

## 6) Import/Export Hardening
- Complete validation-first import commit pipeline for each import type.
- Add deterministic duplicate-handling policies (merge/replace/skip) and explicit user-visible outcomes.
- Improve long-running import progress feedback and retry/error recovery.
- Ensure export bundles are complete and versioned for re-import compatibility.

## 7) Family and Entity Administration Enhancements
- Add explicit ownership transfer workflow with audit logging.
- Add safer entity deletion options (archive/reassign/delete with clear dependency handling).
- Improve member-role management granularity beyond simple owner/member flows where needed.
- **Add member list to Settings > Manage Family/Group page.** Currently the page only shows an email invite field with no visibility into existing members. Display a list of current family members with role (owner/member), join date, and remove option (owner-only). **Owner decision: member visibility is a priority for family collaboration.**

## 8) Budget and Transaction Advanced Capabilities
- Expand deterministic budget merge conflict handling (categories, transactions, fund balances).
- Add stronger recurring-transaction controls in budget duplication flows.
- Improve audit coverage for batch transaction operations and admin-triggered maintenance actions.
- ~~**Introduce a first-class transfer transaction type**~~ ✓ **Shipped (2026-05-02).** `transactionType: 'transfer'` with signed splits is canonical for inter-category moves; aggregations skip transfers in income/expense totals (`q-srfm/src/utils/reportAggregations.ts`). Goal-funded expenses use a different model: a standard `Transaction` with `funded_by_goal_id` persisted on the row, so the expense counts in the destination category and the goal sees a Goal Spend (`GoalService.GetGoals` `funded_agg` sub-aggregation, `GoalService.GetGoalDetails` UNION).

## 11) Goal Visibility and Fund-to-Spend Traceability
- Surface goal **current balance** ("what's remaining") prominently — total contributions minus total spends — as a real-time number, not just a progress bar toward a target.
- Allow a goal spend entry to be **tagged to a budget category** so users can track "how much of this savings goal went to uniforms vs. birthday vs. other."
- Provide a goal detail view that answers three distinct questions in one place:
  1. How much has been contributed (and from what source)?
  2. Where was it spent (by category)?
  3. What is the remaining balance?
- Ensure goal contributions drawn into budget categories are **excluded from category income totals** in Monthly, YoY, and Register reports so actual cash outflows are visible without the contribution canceling them out.

## 9) Test Coverage and Quality Gates
- Add backend automated tests for authorization boundaries and critical financial mutations.
- Add integration tests for budget save/merge/carryover and reconciliation workflows.
- Add API + `dataAccess.ts` contract tests to prevent route/payload drift regressions.
- Extend frontend tests for reconciliation matching, carryover math, and month ordering behavior.

## 12) Mobile Experience Audit and Hardening
_Partially informed by March 2026 UI/UX review (see `UI_UX_Review_SteadyRise.md`)._
- **Audit all core workflows on mobile** (375–430px viewport): transaction entry, budget view, category selection, account overview. Document which are broken or painful to use.
- **Prioritize transaction entry on mobile** — this is the highest-frequency phone use case. The transaction form should open in a bottom sheet or full-screen modal, use native input types, and require minimal scrolling to submit.
- Review and fix any layouts that rely on horizontal scrolling, overflow tables, or side-by-side panels that collapse poorly on small screens.
- Ensure all tap targets (buttons, list items, form controls) meet 44×44px minimums throughout the app.
- Evaluate navigation UX on mobile: consider a bottom navigation bar or persistent floating action button for the most common phone actions (add transaction, switch month).
- Add mobile viewport sizes (375px, 390px, 430px) to any UI testing or review checklist.
- **[CRITICAL] Fix sidebar auto-open on mobile navigation.** Currently, every page change on mobile triggers the sidebar drawer to open, blocking content and requiring an extra tap to dismiss. The sidebar must default to closed on mobile breakpoints.
- **[CRITICAL] Consolidate mobile navigation.** The current dual navigation (sidebar + bottom tab bar) confuses users. The bottom tab bar should be the primary mobile nav and must provide access to all sections — currently missing Reports, Data Mgmt, and Settings. Add a "More" menu item or reorganize the tab bar.
- **[CRITICAL] Make data tables responsive.** Transaction and Account tables currently truncate columns on mobile with no scroll affordance or alternative view. Implement card-based mobile views or horizontally scrollable containers with visible scroll indicators.
- **Fix mobile base font size.** The current 14px base is below iOS's 16px threshold, triggering auto-zoom on input focus. Increase to 16px for mobile viewports.

## 13) Onboarding: Guided Tour System (Replaces Wizard Expansion)
_Informed by March 2026 UI/UX review. **Owner decision: replace wizard with guided tour approach.**_
- **Do NOT expand the setup wizard** with budget/goal steps. The existing wizard (family → entities → accounts) is sufficient for initial setup.
- **Migrate SetupWizardPage from Vuetify-era code.** The current implementation uses Vuetify CSS selectors (`.v-stepper-header`, `.v-stepper-item`) and variables (`var(--v-theme-*)`) in a Quasar app. Migrate to Quasar-native `q-stepper` styling and app semantic tokens.
- **Replace hardcoded styles.** The wizard uses hardcoded border color `rgba(0, 0, 0, 0.12)` instead of `var(--color-outline-soft)`. Migrate to token-based styling.
- **Build a guided tour system** with two components:
  1. **Auto-triggered contextual prompts** for first-time users: as users navigate, show inline tooltips/popovers explaining key features (e.g., "This is your budget — set planned amounts for each category," "Tap a transaction to edit it," "Match imported bank transactions here"). Must be permanently dismissible.
  2. **Persistent "Getting Started" checklist** accessible from the sidebar or settings at any time. Checklist items: create first budget, enter a transaction, import bank data, set up a savings goal, reconcile an account. Items auto-complete as users perform the actions.
- **Add contextual help for entity concept.** Non-expert users don't understand what an "entity" is. Add a brief explanation with examples (e.g., "An entity is a budget context — most families just need one called 'Family Budget.' Add more if you track rental property or business finances separately.").
- **Change default landing page to Budget.** After login, route users to the Budget page instead of Dashboard. **Owner decision: Budget is the primary workspace.**

## 14) Accessibility Remediation
_Informed by March 2026 UI/UX review._
- **[CRITICAL] Remove `user-scalable=no` and `maximum-scale=1` from viewport meta tag.** This prevents pinch-to-zoom on mobile and violates WCAG 2.1 AA SC 1.4.4 (Resize Text). This is a one-line fix in the HTML template.
- **Fix heading hierarchy across all pages.** The dashboard currently has only an H4 for "Evans Family" — the page title "Dashboard" is styled text, not a semantic heading. Every page must have a proper `h1` page title.
- **Add visible persistent labels to all form inputs.** Several inputs (Budget search, Transaction filters for Merchant/Min$/Max$/Start/End) use placeholder-only labels that disappear on focus. Replace with floating labels or visible above-field labels.
- **Mark required form fields.** No form in the app currently indicates which fields are required. Add visual indicators and `aria-required` attributes.
- **Audit and fix color contrast.** Light gray placeholder text and small status icons likely fail 4.5:1 contrast requirements. Run automated contrast checker on all pages.
- **Improve semantic HTML and ARIA.** The accessibility tree shows heavy use of `generic` roles where semantic elements (`nav`, `article`, `section`, `table`) or ARIA roles would improve screen reader experience.

## 15) Terminology and Plain-Language UX
_Informed by competitive analysis. **Owner decision: keep current labels, add tooltips on hover/tap (not rename).**_
- **Add tooltips to accounting terminology.** Terms like "Reconciled," "Cleared," "Uncleared," "Budget Register," "Account Register" need contextual tooltips explaining their meaning for non-expert users. Use Quasar `q-tooltip` on hover/tap. Labels remain unchanged.
- **Do NOT rename labels.** Both personas see the same interface with equal priority. Tooltips bridge the knowledge gap without dumbing down the UI for power users.
- **Add contextual help for complex workflows.** Match Bank Transactions, Reconciliation, and Statement features need brief in-page explanations of what they do and why a user would use them.
- **Remove Transactions page subtitle entirely.** "Monitor budgets and accounts side by side" is misleading. The page title "Transactions" is sufficient. **Owner decision: no subtitle.**

## 16) Destructive Action Safety
_Informed by March 2026 UI/UX review._
- **Add delete confirmation to Accounts page.** Edit and Delete icon buttons on account rows currently have no confirmation step. Deleting an account should require a dialog that names the account and warns about dependent data.
- **Restyle transaction delete in edit modal.** The Delete button is currently a plain red text link — easy to tap accidentally on mobile. Restyle as a proper destructive button with confirmation.
- **Restyle budget page unmatched transaction trash icons.** Red trash can icons next to dollar amounts are alarming for non-expert users ("will I lose money?"). **Owner clarification: the action is a soft delete — the transaction is marked as deleted and can be restored from the "Deleted" tab.** The icon should be restyled to communicate soft-delete intent (e.g., use a muted/outlined trash icon instead of solid red), add a tooltip ("Mark as deleted — can be restored"), and ensure the "Deleted (0)" tab on the Budget page is discoverable so users know where to find restored items.
- **Add confirmation for all batch operations.** Batch delete, batch status change, and similar multi-item actions must display affected count and require explicit confirmation.

## 17) Dashboard and Empty State Improvements
_Informed by March 2026 UI/UX review. **Owner decision: Budget page is the new default landing page, not Dashboard.** Dashboard remains available but is deprioritized._
- **Dashboard CTA is deprioritized** since users will land on Budget page instead. If/when Dashboard is revisited, consider a context-dependent CTA based on state.
- **Fix Income vs. Expenses chart.** The chart shows a flat $0 line — **needs investigation** to determine if this is a data issue (no income recorded) or a rendering bug. If data issue, add an empty-state message; if bug, fix the chart.
- **Fix Upcoming Bills empty state.** "Connect or import statements to see upcoming bills" — but there is no "Connect" button or link to the import page. Add a direct action link.
- **Fix Goals empty state.** "No active goals" with only a small "+" icon. Replace with a guided CTA: "Set your first savings goal →".
- **Fix Data Mgmt loading race condition.** The page shows "No bank or credit card accounts found" error simultaneously with a "Loading data..." spinner. The error must not render until the data fetch has completed.

## 18) Reports and Charts Fixes
_Informed by March 2026 UI/UX review._
- **Fix Planned vs. Actual chart x-axis.** The scatter chart on the Reports Monthly Overview repeats "Feb 2026" on the x-axis instead of showing a proper date range.
- **Add chart interactivity on mobile.** Charts should support tap-to-show-detail for data points, since hover is not available on touch devices.

## 10) Legacy/Documentation Cleanup Follow-ups
- Remove legacy or duplicate store patterns that risk split state ownership.
- Keep a single living contract reference for routes and DTOs to prevent future divergence.
- Track migration completion of mixed Firestore/Supabase responsibilities in auth-related flows.
- **Clean up SetupWizardPage Vuetify remnants** — migrate `.v-stepper-*` classes and `var(--v-theme-*)` variables to Quasar equivalents.
