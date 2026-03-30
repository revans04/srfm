# UI/UX Rework — Coding Agent Prompt

You are working on **Steady Rise Financial Management (SRFM)**, a Quasar 2 + Vue 3 personal finance web app. A comprehensive UI/UX review was completed on March 29, 2026, and the product owner has made binding design decisions based on the findings. Your job is to implement these changes.

## Required Reading (do this first)

Before writing any code, read these files in order:

1. `AGENTS.md` — Architecture, invariants, extension guidelines, anti-patterns
2. `docs/design_system.md` — Authoritative UI/UX rules with `[ENFORCED]` markers
3. `docs/needed_features.md` — Feature roadmap with prioritized items
4. `docs/product_requirements.md` — Product specifications and owner decisions
5. `UI_UX_Review_SteadyRise.md` — Full review with findings, severity ratings, and the **Addendum: Owner Design Decisions** at the bottom

The design system is law. Every `[ENFORCED]` rule is mandatory. Items marked with **"Owner decision"** in the docs are final and should not be second-guessed.

## Owner Design Decisions (summary)

These were decided by the product owner and are non-negotiable:

- **Equal persona priority** — both family users and accountants see the same interface
- **Tooltips, not renames** — keep labels like "Reconciled," "Budget Register" as-is; add `q-tooltip` explanations
- **Full mobile overhaul** — this is the top priority
- **Budget page is the default landing page** — not Dashboard
- **Guided tour, not expanded wizard** — auto-prompts for new users + persistent "Getting Started" checklist
- **Soft-delete trash icons on Budget page** — restyle to muted/outlined with tooltip; Deleted tab must be discoverable
- **Remove Transactions page subtitle** entirely
- **Add family member list** to Settings > Manage Family/Group
- **Investigate Income vs. Expenses chart** — determine if data issue or rendering bug before fixing

## Implementation Phases

Work through these phases in order. Each phase should be a separate commit (or set of commits). Do not move to the next phase until the current one is complete and tested at 375px, 390px, and 430px viewports.

---

### Phase 1: Critical Fixes (one-line to small changes)

These are the highest-severity issues and mostly require minimal code:

1. **Fix viewport meta tag.** In the HTML template (likely `q-srfm/index.html` or equivalent), remove `user-scalable=no` and `maximum-scale=1` from the viewport meta content. The result should be: `<meta name="viewport" content="width=device-width, initial-scale=1">`. This is a WCAG 2.1 AA violation.

2. **Fix sidebar auto-open on mobile.** In `q-srfm/src/layouts/MainLayout.vue`, ensure the left drawer defaults to **closed** on mobile breakpoints (`$q.screen.lt.md`). It should only open when explicitly toggled. The bottom tab bar is the primary mobile nav. This is the single most impactful mobile bug.

3. **Fix base font size on mobile.** In `q-srfm/src/css/app.scss`, add a media query that sets `font-size: 16px` on `body`/`html` for viewports ≤430px. The current 14px triggers iOS auto-zoom on input focus.

4. **Remove Transactions page subtitle.** In `q-srfm/src/pages/TransactionsPage.vue`, remove the subtitle text "Monitor budgets and accounts side by side." Keep only the "Transactions" page title.

5. **Change default landing page to Budget.** In `q-srfm/src/router/`, update the default authenticated route from `/` (Dashboard) to `/budget`. The Dashboard route should still exist and be navigable, just not the default.

6. **Fix Data Mgmt loading race condition.** In `q-srfm/src/pages/DataPage.vue`, the error message "No bank or credit card accounts found" must NOT render while the loading spinner is active. Gate the error state behind the loading completion: only show the error after the async fetch resolves and the result is genuinely empty.

After Phase 1, verify: viewport allows zoom, sidebar stays closed on mobile nav, font is 16px on mobile, Transactions has no subtitle, login lands on Budget, Data Mgmt doesn't flash error during load.

---

### Phase 2: Mobile Navigation Overhaul

7. **Complete the bottom tab bar.** The current bottom nav has Dashboard, Budget, Transactions, Accounts, and Logout but is **missing Reports, Data Mgmt, and Settings**. Add a "More" menu item (use `more_horiz` Material icon) that opens a bottom sheet or popup with the remaining sections: Reports, Data Mgmt, Settings, and Logout (move Logout into More). This gives the bottom bar: Dashboard, Budget, Transactions, Accounts, More.

8. **Ensure sidebar and bottom nav don't conflict.** On mobile (`$q.screen.lt.md`): the sidebar should be accessible via a hamburger menu icon in the header but should NOT be the primary navigation. The bottom tab bar is primary. On desktop (`$q.screen.gt.sm`): the sidebar is primary and the bottom tab bar is hidden. There should be no state where both are visible and competing.

After Phase 2, verify: all 7 sections are reachable on mobile via bottom bar + More menu. Test at 375px. Sidebar only opens on explicit user action, never automatically.

---

### Phase 3: Responsive Data Tables

9. **Make Transactions table responsive.** On mobile breakpoints, replace the multi-column table with a card-based list view showing: Date, Payee, Amount, and Category as the primary visible fields. Status and Notes can be shown on tap/expand. Use `$q.screen.lt.md` to switch between table (desktop) and card list (mobile). No horizontal scrolling.

10. **Make Accounts table responsive.** Same pattern: on mobile, show Name, Institution, and Balance as primary fields in a card layout. Account Number, Owner, and Actions are accessible on tap/expand. Edit and Delete actions must be reachable from the expanded card with proper tap target sizing (44×44px minimum).

11. **Make Budget page responsive.** Review the Budget page at 375px. The income table currently drops the "Planned" column on mobile — verify this is intentional or restore it. The right-side unmatched transactions panel should stack below the main budget content on mobile, not alongside it. The transaction cards in the unmatched panel need adequate tap targets.

After Phase 3, verify: no horizontal scrolling on any page at 375px. All data is accessible. Tap targets ≥44×44px.

---

### Phase 4: Accessibility & Semantic HTML

12. **Fix heading hierarchy on all pages.** Every page must have exactly one `h1` for the page title. Audit all pages in `q-srfm/src/pages/` and ensure the page title uses a semantic `<h1>` tag (or Quasar equivalent). The Dashboard currently has the title as styled text and only an `h4` for "Evans Family."

13. **Add visible persistent labels to all form inputs.** Audit all pages for placeholder-only inputs. Key offenders: Budget page search ("Search categories or groups"), Transaction filter fields (Merchant, Min $, Max $, Start, End). Replace placeholder-only patterns with Quasar floating labels (`label` prop on `q-input`) that remain visible when the field has content.

14. **Mark required form fields.** Add visual required indicators (asterisk or "required" text) and `aria-required="true"` to all mandatory fields across: Transaction edit modal, Account forms, Entity forms, Budget category forms.

15. **Add tooltips to accounting terminology.** Add `q-tooltip` to these terms wherever they appear:
    - "Reconciled" → "Verified against your bank statement"
    - "Cleared" → "Transaction has posted to your account"
    - "Uncleared" → "Transaction hasn't posted yet"
    - "Budget Register" → "Transactions matched to your budget categories"
    - "Account Register" → "Transactions imported from your bank account"
    - "Match Bank Transactions" → "Link imported bank transactions to your budget entries"

After Phase 4, verify: every page has one `h1`, no placeholder-only labels, required fields marked, tooltips appear on hover/tap for accounting terms.

---

### Phase 5: Destructive Action Safety & Feedback

16. **Add delete confirmation to Accounts page.** When clicking the trash icon on an account row, show a `q-dialog` confirmation: "Delete [Account Name]? This will remove the account and its transaction history. This cannot be undone." Confirm button uses `color="negative"` with label "Delete Account."

17. **Restyle transaction Delete in edit modal.** The "Delete" text link in the transaction edit modal (`TransactionForm` or `TransactionDialog`) must become a proper `q-btn` with `color="negative"` and `outline` or `flat` variant. Add a confirmation dialog before executing.

18. **Restyle Budget page unmatched-transaction trash icons.** Replace solid red trash icons with muted/outlined style. Add `q-tooltip`: "Mark as deleted — can be restored from Deleted tab." Ensure the "Deleted" tab/filter in the unmatched transactions panel is visually discoverable (not just a count that says "Deleted (0)").

19. **Audit success feedback.** Ensure every write operation across the app fires a `$q.notify` toast on success. Key areas to check: transaction save/delete, account save/delete, budget save, entity save/delete, goal save, import completion, settings changes.

After Phase 5, verify: all destructive actions require confirmation, delete buttons are visually distinct, success toasts appear on all saves.

---

### Phase 6: Button Consistency & Visual Polish

20. **Standardize button hierarchy.** Audit all pages and enforce:
    - **Primary** actions (Save, Create, Finish): `q-btn color="primary"` (filled)
    - **Secondary** actions (Cancel, Back, Skip): `q-btn flat` or `q-btn outline`
    - **Destructive** actions (Delete, Remove): `q-btn color="negative"` (filled or outlined)
    - **Tertiary/filter** actions: `q-btn outline` with consistent coloring

    Key offenders: Settings "Invite" button (outlined), "Add Entity" (filled blue), "Find Smart Matches" (filled green), filter buttons on Transactions (mixed styles), Entity edit/delete icons (oversized colored circles vs. icon-only elsewhere).

21. **Standardize edit/delete action icons.** The Settings > Manage Entities page uses large colored circle buttons (blue edit, red delete) that are visually inconsistent with the smaller icon buttons used on Accounts. Choose one pattern and apply consistently. Recommendation: use `q-btn flat round` with `icon` prop and consistent sizing across all list/table action columns.

After Phase 6, verify: button styles are consistent across all pages. No mixed patterns for the same action type.

---

### Phase 7: Settings & Member Management

22. **Add member list to Settings > Manage Family/Group.** Below the invite email field, display a list of current family members. For each member show: name/email, role (Owner/Member badge), join date. The family owner should see a "Remove" action on non-owner members. Use a `q-list` with `q-item` components. The owner row should be visually distinct (e.g., bold name, "Owner" chip). Requires a backend endpoint to fetch family members — check if `FamilyService` already exposes this data or if a new endpoint is needed.

After Phase 7, verify: member list displays correctly, owner badge shown, remove action works (owner only).

---

### Phase 8: Guided Tour System

23. **Build the guided tour infrastructure.** Create a composable `useGuidedTour` that:
    - Tracks tour state per user in a Pinia store (which steps have been seen/dismissed)
    - Persists dismissal state (probably via API, not localStorage per design system)
    - Exposes `showTip(stepId)` and `dismissTip(stepId)` / `dismissAll()` methods
    - Integrates with Quasar `q-popup-proxy` or `q-tooltip` for contextual prompts

24. **Implement auto-triggered prompts for new users.** On key pages, show a one-time contextual prompt:
    - Budget page: "This is your monthly budget. Set planned amounts for each category and track actual spending."
    - Transactions > Budget Register: "Budget transactions are ones you've entered or matched to your budget categories."
    - Transactions > Account Register: "These are transactions imported from your bank. Match them to budget entries to reconcile."
    - Transactions > Match Bank Transactions: "This tool helps you link imported bank transactions to your budget entries automatically."
    - Accounts page: "Add your bank accounts, credit cards, and investments to track your net worth."
    - Reports page: "Compare your planned budget to actual spending across months."

25. **Build persistent "Getting Started" checklist.** Accessible from the sidebar (desktop) or More menu (mobile). Checklist items that auto-complete when the user performs the action:
    - [ ] Create your first budget
    - [ ] Enter a transaction
    - [ ] Import bank transactions
    - [ ] Set up a savings goal
    - [ ] Reconcile an account
    Show a progress indicator (e.g., "3 of 5 complete").

After Phase 8, verify: new user sees prompts on first visit to each page, can dismiss permanently, checklist tracks completion.

---

### Phase 9: Investigate & Fix Charts

26. **Investigate Income vs. Expenses chart.** On the Dashboard, determine why the chart shows a flat $0 line. Check: is income data present in the database for the displayed period? Is the chart query filtering correctly? Is the chart component receiving data? Document findings and fix accordingly — if data issue, add an empty state message; if rendering bug, fix the chart.

27. **Fix Reports Planned vs. Actual chart x-axis.** The scatter chart on Reports > Monthly Overview repeats "Feb 2026" on the x-axis. Investigate the chart data source and fix the axis labeling to show a proper date range.

---

### Phase 10: SetupWizardPage Migration

28. **Migrate SetupWizardPage from Vuetify to Quasar.** The current page uses:
    - Vuetify CSS selectors: `.v-stepper-header`, `.v-stepper-item`, `.v-stepper-item--complete`
    - Vuetify CSS variables: `var(--v-theme-on-surface)`, `var(--v-theme-primary)`, `var(--v-theme-success)`, `var(--v-theme-surface-variant)`
    - Hardcoded border color: `rgba(0, 0, 0, 0.12)`

    Replace all with Quasar-native equivalents and app semantic tokens (`var(--color-outline-soft)`, `var(--color-surface-subtle)`, etc.). The stepper already uses `q-stepper` components — it's only the CSS that needs migration.

---

## General Rules (apply to all phases)

- Follow `docs/design_system.md` `[ENFORCED]` rules without exception.
- Use Quasar primitives and existing SRFM components before creating new ones.
- Use semantic tokens only — no hardcoded hex values, no arbitrary spacing.
- Test every change at 375px, 390px, and 430px viewport widths.
- All interactive elements must have 44×44px minimum tap targets on mobile.
- All form inputs must use correct mobile input types (`number`, `date`, `tel`, `email`).
- Preserve existing functionality — these are UX improvements, not rewrites.
- If you discover an issue not covered here, document it but don't fix it outside scope.
