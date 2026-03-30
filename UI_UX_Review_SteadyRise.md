# UI/UX Review — Steady Rise Financial Management (SRFM)

**Application URL:** http://localhost:9000/
**Version:** 0.3.31

---

## Reviewer Info

| Field | Detail |
|---|---|
| **Reviewer** | Claude (AI-assisted review for Ray Evans) |
| **Date** | March 29, 2026 |
| **Desktop Device / Browser / OS** | Microsoft Edge (Chromium) / Windows |
| **Mobile Emulation** | 375×812 viewport (iPhone-equivalent via browser resize) |
| **Time Spent** | ~30 minutes |

---

## Application Overview

Steady Rise is a family-oriented personal finance management web application. It provides budgeting, transaction tracking, account management, reporting, data import/export, and settings management across seven main navigation sections: Dashboard, Budget, Transactions, Accounts, Reports, Data Mgmt, and Settings.

---

## Target Audience & Competitive Context

**Intended replacement for:** EveryDollar, Mint (discontinued), Quicken, and Excel-based budgeting.

**User base spans two distinct personas:**

**Persona 1 — Family Budget Manager (majority):** Non-expert users who want a simple, clear way to track spending, set budgets, and see where their money goes each month. These users are coming from EveryDollar or Mint and expect guided workflows, minimal jargon, and mobile-first convenience. They may check the app daily on their phone to log a purchase or weekly on desktop to review the budget.

**Persona 2 — Accountant / Bookkeeper:** Power users who may manage finances for multiple entities, need reconciliation workflows, want detailed reports, and are comfortable with terms like "reconciled," "cleared," and "uncleared." These users are migrating from Quicken or Excel and expect data density, keyboard shortcuts, and export capabilities.

### How This Context Affects the Review

This dual-audience reality creates specific tensions that surface throughout the review:

- **Terminology:** The app uses accounting terms like "Reconciled," "Cleared," "Uncleared," "Budget Register," and "Account Register" throughout Transactions. For Persona 1 (the family user coming from EveryDollar), these terms are unfamiliar and intimidating. EveryDollar uses plain language like "spent," "planned," and "remaining." Quicken users understand these terms, but they are a minority of the target audience. **Severity: 3** — Consider adding tooltips or a glossary, or using plainer language with a "show advanced view" toggle.

- **Onboarding / empty states:** Competitors like EveryDollar guide new users through budget setup with a step-by-step wizard. Steady Rise drops users onto a dashboard with pre-existing data (for this account) but provides no onboarding guidance. For a new user, the "Upcoming Bills" card says "Connect or import statements to see upcoming bills" — but there is no obvious "Connect" button or link. The "Goals" card says "No active goals" with only a small "+" icon. These empty states are missed opportunities to guide non-expert users through setup. **Severity: 3**

- **Mobile expectations:** EveryDollar and Mint were primarily mobile apps. Users migrating from these tools will expect a polished, mobile-first experience. Steady Rise's mobile experience (as documented in this review) falls short of that expectation, which will be a dealbreaker for a significant portion of the target audience.

- **Data density vs. simplicity:** The Transactions page displays a full table with checkboxes, Date, Payee, Category, Entity/Budget, Amount, Status, and Notes — 8 columns. This serves Persona 2 well but overwhelms Persona 1. EveryDollar shows transactions as a simple list of merchant + amount + category. Consider offering a simplified "quick view" alongside the full register.

- **Reconciliation workflow:** The "Match Bank Transactions" tab and reconciliation features are powerful for bookkeepers but invisible/confusing for casual users. This is actually well-handled by placing it in a separate tab — the concern is that the tab label doesn't explain what matching means to a non-expert.

- **Multi-entity support:** The "Evans Family" dropdown and entity management (Family, Walton RentalProperty, Willow Oak RentalProperty) is a strong differentiator vs. EveryDollar. However, the entity concept is unexplained — a new user wouldn't know what an "entity" is or why they'd create one. Settings > Manage Entities lists items like "Walton (RentalProperty) - Owner: N/A" with no explanation. **Severity: 2**

- **Budget page — Unmatched Transactions panel:** The right sidebar on the Budget page showing "Unmatched (21)" transactions with delete icons (red trash cans) is a powerful feature for matching imported bank data to budget categories. However, for Persona 1, the red trash icons next to dollar amounts could be frightening — "will I lose money if I click this?" The delete action needs clearer labeling (e.g., "Dismiss" or "Ignore") and a confirmation step. **Severity: 3**

---

## Section 1: First Impressions

### Desktop

1. **What does this application do?** It is clearly a personal finance/budgeting tool. The dashboard displays budget progress, net worth, upcoming bills, goals, spending by category (donut chart), and an income-vs-expenses trend line. The purpose is immediately understandable.

2. **Primary action the interface wants you to take?** Review your financial snapshot. There is no single dominant CTA — the dashboard is informational. The "Evans Family" and "March 2026" dropdowns at top-right suggest switching context, but the primary action is passive (read your data). **Severity: 2** — Consider adding a prominent CTA like "Add Transaction" or "Review Unmatched Items" to drive engagement.

3. **Visual hierarchy:** The top row of four summary cards (Budget Progress, Net Worth, Upcoming Bills, Goals) draws the eye first, which is appropriate. The large donut chart dominates below. The "Income vs. Expenses" chart on the right is significantly underweighted — it shows a flat $0 line, making it feel broken or empty. **Severity: 2**

4. **Cluttered, balanced, or sparse?** Balanced on desktop. Good use of card-based layout. The left sidebar is clean with icon+label navigation. The dashboard has two clear rows of content.

5. **Trust:** Moderately trustworthy. The design is clean and professional, using a standard sidebar+content layout. The "Steady Rise" logo and branding are present. However, the version number (0.3.31) visible in the sidebar footer signals "beta" software, which may reduce confidence for some users. **Severity: 1**

6. **Mobile-specific first impression:** See Mobile Findings section below.

### Mobile

1. **Above the fold:** Only Budget Progress and Net Worth cards are visible above the fold. The header takes up meaningful space. The layout stacks cards vertically, which is appropriate.

2. **Purpose-built or shrunken desktop?** Mixed. The cards adapt well, and there is a dedicated bottom tab bar (Dashboard, Budget, Transactions, Accounts, Logout). However, the sidebar opens automatically on every page navigation, overlaying the content — this is a **critical** issue. The bottom nav partially duplicates the sidebar, creating confusion about which is the primary navigation. **Severity: 4**

---

## Section 2: Usability & Task Flow

### Core Task: View Dashboard

- **Steps:** Log in → land on Dashboard (1 step)
- **Time:** Instant after login
- **Confusion points:** None. Dashboard is the default landing page.
- **Success feedback:** Data is displayed immediately.

### Core Task: View/Edit a Transaction

- **Steps (Desktop):** Click "Transactions" in sidebar → click a row → edit modal appears → modify fields → click "Save Transaction" (3 clicks to edit)
- **Time:** ~10 seconds
- **Confusion points:** Clicking anywhere on a transaction row opens the edit modal. There is no read-only detail view — only an edit modal. Users who just want to review a transaction are immediately placed into an edit context. **Severity: 2**
- **Error handling:** Not tested for invalid input, but the modal includes Cancel, Delete, and Save buttons. The Delete button is styled as a plain text link in red, which is inconsistent with its destructive nature — it should have more visual weight or a confirmation step. **Severity: 3**
- **Success feedback:** Not clearly communicated after save (no toast/banner confirmation observed).

### Core Task: Navigate Transactions Tabs

- **Steps:** Click "Budget Register" / "Account Register" / "Match Bank Transactions" tabs
- **Time:** Instant
- **Confusion points:** The page subtitle says "Monitor budgets and accounts side by side" but the actual view is not side-by-side — it's tabbed. The description is misleading. **Severity: 2**

### Core Task: Import Data

- **Steps:** Click "Data Mgmt" in sidebar → Select Import Type → Select Account → Upload CSV
- **Time:** ~15 seconds
- **Confusion points:** The page simultaneously shows "No bank or credit card accounts found. Please create an account in the Accounts section before importing transactions" AND a "Loading data..." spinner. The error message appears while data is still loading, creating a contradictory state. **Severity: 3**
- **Success feedback:** Not observable due to the loading/error conflict.

### Core Task: Manage Accounts

- **Steps:** Click "Accounts" → view table → click Edit (pencil) icon or Delete (trash) icon
- **Time:** ~5 seconds
- **Confusion points:** The tab bar (Bank Accounts, Credit Cards, Investments, Properties, Loans, Snapshots) provides good organization. The "+Add Account" button is clearly visible.
- **Edit/Delete:** Edit and Delete icons are small but recognizable. No confirmation dialog observed for Delete. **Severity: 3**

### Core Task: View Reports

- **Steps:** Click "Reports" → select budget from dropdown → view charts and tables
- **Time:** ~5 seconds
- **Confusion points:** The "Planned vs Actual" scatter chart on the right shows "Feb 2026" repeated on the x-axis instead of showing a proper month range, suggesting a data or rendering bug. **Severity: 2**

### Key Usability Questions

| Question | Finding |
|---|---|
| Most important task within 3 clicks? | Yes — viewing dashboard (1 click), viewing transactions (1 click + row click for edit) |
| Navigation intuitive? | Yes on desktop (clear sidebar). Problematic on mobile (dual nav). |
| Dead ends? | Settings > Manage Family/Group page is very sparse — only an email invite field with no explanation of current family members. |
| Back button behavior? | Works as expected throughout. |
| Form labels clear? | Mostly yes. Transaction edit modal uses floating labels. Budget search uses placeholder text only (no label). |
| Required fields marked? | No — required fields are not visually indicated anywhere in the app. **Severity: 2** |
| Loading states? | Present on Data Mgmt page ("Loading data...") and refresh icons on dashboard cards. However, the loading state on Data Mgmt conflicts with an error message. |

### Mobile-Specific Usability

| Question | Finding | Severity |
|---|---|---|
| Tap targets adequate (44×44px)? | Bottom nav icons appear adequate. Small icons in tables (edit/delete on Accounts, status icons on Transactions) are likely undersized. | 3 |
| Forms optimized for mobile? | Filter fields on Transactions stack vertically, which is good. No evidence of specialized keyboard types for amount fields. | 2 |
| One-handed use? | Bottom nav is thumb-friendly. Top header controls (Evans Family dropdown, month selector) require reaching to the top. | 2 |
| Modals on small screens? | Transaction edit modal was not tested on mobile, but modal-based editing on 375px screens is often problematic. | 2 |
| Horizontal scrolling required? | Yes — transaction table and accounts table both truncate columns, requiring horizontal scroll to access all data. **This is a significant mobile usability failure.** | 3 |
| Content prioritization? | The mobile layout stacks the desktop layout vertically without meaningfully reprioritizing content. No mobile-first redesign of data tables. | 3 |

---

## Section 3: Visual Design & Consistency

### Layout & Spacing

- Consistent card-based layout on the dashboard with uniform padding and border-radius.
- The sidebar uses a fixed-width column (~200px) with good icon+label alignment.
- Whitespace is generally well-used on desktop. Some table pages (Transactions, Accounts) feel dense when fully populated.
- **Severity: 1** — Minor: some inconsistency in card shadow/border treatment between pages.

### Typography

- **Typefaces:** Single typeface family: Inter (with system font fallbacks). This is appropriate and clean.
- **Type hierarchy:** Weak. The dashboard heading "Dashboard" is styled as plain large text, not a semantic H1. The only heading element on the dashboard is an H4 for "Evans Family." Page titles across the app are styled large but may not use proper heading tags consistently. **Severity: 2**
- **Body text legibility:** 14px base font size. On desktop this is acceptable but on the small side. On mobile, this is below the recommended 16px minimum to prevent iOS auto-zoom on input focus. **Severity: 3**
- **Line length:** Acceptable on desktop. On mobile, text width adapts to the viewport.

### Color

- **Palette:** Blue primary (#1976D2-ish for active nav and links), with blue header bar on mobile. Red for destructive actions (delete icons). Green for positive actions (smart matches button). Yellow/amber for the legacy savings warning banner. The palette is cohesive.
- **Interactive elements distinct?** Yes — blue links, blue active states, colored action buttons are distinguishable from static content.
- **Color for meaning:** Red = negative amounts/delete, green = positive/confirm. Status icons use green (C = Cleared) and other colors. However, the status icons are very small and rely on single-letter abbreviations + color — meaning partially depends on color alone. **Severity: 2**
- **Supporting indicators beyond color:** Transaction status uses letter codes (C, U) with icons. Category donut chart uses color + text labels. Generally adequate.

### Iconography & Imagery

- Icons are from Material Icons/Symbols set — recognizable standard icons.
- Navigation icons have accompanying text labels — good practice.
- Refresh icons on dashboard cards lack labels and could be confusing (is it refresh data or a sync indicator?). **Severity: 1**
- The app logo (frog/lizard mascot) is friendly but doesn't immediately convey "finance." **Severity: 1**

### Consistency

- Button styles vary across the app: "Invite" on Settings is outlined, "Add Entity" is filled blue, "Find Smart Matches" is filled green, filter buttons on Transactions are outlined blue/colored. **Severity: 2** — Button styling should follow a consistent system (primary, secondary, destructive).
- Card styles are mostly consistent (rounded corners, light shadow).
- Tab navigation styles are consistent across pages (uppercase text, underline active).
- Edit/Delete icon buttons on Accounts (large colored circles) differ dramatically from the pen/trash icons used elsewhere. **Severity: 2**

---

## Section 4: Accessibility (WCAG 2.1 AA Baseline)

### Perceivable

| Check | Status | Severity |
|---|---|---|
| Alt text on images | Pass — all images have alt text | — |
| Color contrast | Not programmatically tested, but light gray placeholder text and small status icons likely fail 4.5:1 contrast ratio | 2 |
| Zoom to 200% | **FAIL** — viewport meta includes `user-scalable=no` and `maximum-scale=1`, which prevents pinch-to-zoom on mobile. This is a WCAG 2.1 AA violation (Success Criterion 1.4.4). | 4 |
| Captions/transcripts | N/A — no audio/video content | — |

### Operable

| Check | Status | Severity |
|---|---|---|
| Keyboard navigation | Navigation items use anchor tags with hrefs — should be keyboard-accessible. Full keyboard-only testing was not performed. | — |
| Visible focus indicator | Not tested in detail. Default browser focus styles may be suppressed by the CSS framework. | 2 |
| Tab order logical | Sidebar nav uses a `<ul>/<li>` list structure, which should produce reasonable tab order. | — |
| Keyboard traps | None observed. | — |
| Touch targets ≥44×44px | Likely failures on small icons: transaction status indicators, refresh icons on dashboard cards, table action icons. | 3 |

### Understandable

| Check | Status | Severity |
|---|---|---|
| Page language set | Pass — `lang="en-US"` on HTML element | — |
| Form inputs with labels | Mixed — some inputs use floating labels, others use only placeholder text (e.g., "Search categories or groups" on Budget, filter fields on Transactions). Placeholder-only labels disappear on focus, failing WCAG. | 3 |
| Error messages near fields | Not tested in depth. The Data Mgmt error message appears page-level, not field-level. | 2 |
| Predictable behavior | Generally yes. No unexpected context changes observed. | — |

### Robust

| Check | Status | Severity |
|---|---|---|
| HTML validation | Not tested. Navigation items use `<li>` elements with `href` attributes directly, which may be non-standard (should be `<a>` inside `<li>`). | 2 |
| ARIA usage | Minimal ARIA observed. Buttons have implicit roles. The accessibility tree shows `generic` for many elements that could benefit from semantic roles. | 2 |
| Screen reader compatibility | Not tested with a screen reader. The heavy use of `generic` roles in the accessibility tree suggests screen reader experience would be suboptimal. | 3 |

---

## Section 5: Responsive Design & Cross-Browser

### Responsive Layout

| Check | Finding | Severity |
|---|---|---|
| Breakpoint adaptation (375px, 768px, 1024px+) | Desktop (1440px+) works well. Mobile (375px) has significant issues: sidebar auto-opens on every navigation, tables truncate without horizontal scroll affordance, tab bars overflow. | 3 |
| Mobile navigation pattern | Dual navigation exists — a sidebar (that auto-opens) AND a bottom tab bar. The bottom bar has only 5 items (Dashboard, Budget, Transactions, Accounts, Logout) and omits Reports, Data Mgmt, and Settings. Users must find the sidebar to access those. | 3 |
| Data tables on mobile | Transaction and Account tables simply truncate columns. No responsive pattern (e.g., card view, expandable rows, horizontal scroll affordance) is employed. | 3 |
| Image sizing | Charts (donut, line) resize properly for mobile viewport. | — |
| Viewport meta | `user-scalable=no, initial-scale=1, maximum-scale=1` — prevents accessibility zoom. | 4 |

### Mobile-Specific Interaction

| Check | Finding | Severity |
|---|---|---|
| Sidebar auto-open on navigation | The sidebar drawer opens automatically on every page change, requiring users to dismiss it before seeing content. This makes every navigation action require an extra tap. | 4 |
| Bottom nav completeness | The bottom tab bar omits 3 of 7 navigation sections (Reports, Data Mgmt, Settings). Users on mobile have no obvious way to discover these sections unless they realize they need the sidebar. | 3 |
| Sticky header | The blue header bar is sticky on mobile — good. The bottom nav is also fixed — good. Together they consume ~120px of vertical space on a 812px viewport. | 1 |
| Orientation changes | Not tested. | — |
| On-screen keyboard | Not tested, but the 14px base font + `user-scalable=no` may cause iOS auto-zoom issues on input focus. | 3 |

### Cross-Browser

- Tested only in Microsoft Edge (Chromium). Safari, Firefox, and Chrome testing not performed in this review.

---

## Section 6: Performance Perception

| Check | Finding | Severity |
|---|---|---|
| Application feels fast? | Yes on desktop — pages load instantly, navigation is snappy. Dashboard charts render without visible delay. | — |
| Loading indicators | Spinner and "Loading data..." text on Data Mgmt page. Refresh icons on dashboard cards. No skeleton screens used. | 1 |
| Layout shifts | None observed on desktop. | — |
| Scroll jank | None observed on desktop or mobile emulation. | — |
| Data Mgmt loading conflict | The Data Mgmt page shows "No bank or credit card accounts found" error simultaneously with a loading spinner, suggesting a race condition where the error renders before the async data fetch completes. | 3 |

---

## Summary Tables

### Desktop Findings

| Category | Top Issue | Severity | Recommendation |
|---|---|---|---|
| First Impressions | Income vs. Expenses chart shows flat $0 line — appears broken | 2 | Add an empty-state message when no income data exists, or hide the chart |
| Usability & Task Flow | Data Mgmt shows error + loading spinner simultaneously | 3 | Wait for data fetch to complete before rendering error states |
| Visual Design | Button styling inconsistent across pages | 2 | Establish and enforce a design system with primary/secondary/destructive button variants |
| Accessibility | `user-scalable=no` prevents zoom (WCAG violation) | 4 | Remove `user-scalable=no` and `maximum-scale=1` from viewport meta |
| Responsive / Cross-Browser | Only tested in Edge; no cross-browser data | — | Test in Chrome, Firefox, Safari |
| Performance Perception | Data Mgmt race condition between loading and error state | 3 | Ensure async data fetches gate UI state rendering |

### Mobile Findings

| Category | Top Issue | Severity | Recommendation |
|---|---|---|---|
| First Impressions | Sidebar auto-opens on every page navigation, blocking content | 4 | Sidebar should default to closed on mobile; use the bottom tab bar as primary nav |
| Usability & Task Flow | Sidebar auto-open adds an extra dismiss-tap to every navigation | 4 | Same as above |
| Mobile-Specific Usability | Data tables truncate columns with no responsive alternative | 3 | Implement card-based views or horizontally scrollable tables with scroll affordance for mobile |
| Visual Design | Dual navigation (sidebar + bottom tab bar) creates confusion | 3 | Choose one primary mobile nav pattern; consolidate into a complete bottom tab bar or use a hamburger menu |
| Accessibility | 14px base font below iOS 16px minimum; zoom disabled | 4 | Increase base font to 16px on mobile; enable user zoom |
| Responsive / Cross-Browser | Bottom nav omits Reports, Data Mgmt, Settings | 3 | Add a "More" item to the bottom bar to access additional sections, or implement a hamburger menu |
| Performance Perception | No significant mobile-specific performance issues observed | — | — |

---

## Overall Assessment

**Strongest aspect of the UI/UX:** The desktop dashboard is well-organized with a clean card-based layout, and the sidebar navigation is clear and intuitive. The application's purpose is immediately obvious, and core tasks (viewing budgets, transactions, accounts) are accessible within 1-2 clicks. The multi-entity support and reconciliation features are genuine differentiators vs. EveryDollar and put Steady Rise in Quicken's territory for power users.

**Most urgent improvement needed (desktop):** Remove `user-scalable=no` and `maximum-scale=1` from the viewport meta tag. This is a WCAG 2.1 AA violation that prevents users with low vision from zooming in to read content.

**Most urgent improvement needed (mobile):** Fix the sidebar auto-open behavior. Currently, every page navigation on mobile triggers the sidebar drawer to open, blocking the content and requiring an extra tap to dismiss. This makes the mobile experience feel broken. Given that Mint and EveryDollar were mobile-first products, users migrating from those tools will judge Steady Rise primarily by its mobile experience.

**One quick win that would make a noticeable difference:** Fix the viewport meta tag (remove `user-scalable=no, maximum-scale=1`) — this is a one-line code change that resolves the most critical accessibility violation.

**Does the mobile experience feel like a first-class product, or an afterthought?** The mobile experience feels like an afterthought. While there are some mobile-aware elements (bottom tab bar, stacked card layout, collapsed header), the fundamental issues — sidebar auto-opening on navigation, truncated data tables without responsive alternatives, incomplete bottom navigation missing 3 of 7 sections, and disabled zoom — indicate that mobile was not the primary design target. The desktop experience is solid, but roughly 50% of users accessing on mobile will encounter significant friction. For a product intended to replace EveryDollar and Mint (both mobile-first apps), this is the single biggest competitive gap.

**Competitive positioning summary:** Steady Rise currently sits closer to Quicken than EveryDollar on the simplicity-vs-power spectrum. The reconciliation workflow, multi-entity support, and data-dense tables serve bookkeepers well. However, to capture the larger family-budget market that EveryDollar and Mint served, the app needs: (1) a mobile experience that feels native and complete, (2) plain-language alternatives for accounting terminology, (3) guided onboarding for first-time users, and (4) simplified views that don't require understanding what "reconciled" or "cleared" means.

---

## Appendix: All Issues Ranked by Severity

### Critical (Severity 4) — Must Fix Before Release

1. **Viewport meta prevents zoom** — `user-scalable=no` and `maximum-scale=1` violate WCAG 2.1 AA SC 1.4.4. Remove these attributes.
2. **Sidebar auto-opens on mobile navigation** — Every page change triggers the sidebar to open, blocking content and requiring extra interaction to dismiss.
3. **14px base font on mobile** — Below iOS's 16px threshold, which triggers auto-zoom on input focus and creates readability issues.

### Major (Severity 3) — Needs Priority Attention

4. **Accounting jargon alienates non-expert users** — Terms like "Reconciled," "Cleared," "Budget Register," and "Account Register" are unfamiliar to family budget managers coming from EveryDollar. Add tooltips, a glossary, or plain-language alternatives.
5. **No onboarding or guided setup** — New users land on the dashboard with no walkthrough. Empty states ("No active goals," "Connect or import statements") don't link to actionable next steps. Competitors offer step-by-step budget setup wizards.
6. **Budget page unmatched-transaction trash icons are alarming** — Red trash can icons next to dollar amounts on the Budget sidebar suggest "delete money" to non-expert users. Relabel as "Dismiss" or "Ignore" and add confirmation.
7. **Data Mgmt race condition** — Error message ("No accounts found") renders while data is still loading, creating contradictory UI state.
8. **No delete confirmation on Accounts** — Destructive actions should always require confirmation.
9. **Transaction Delete button understated** — A destructive action styled as a plain text link in the edit modal is easy to hit accidentally.
10. **Data tables not responsive** — Transaction and Account tables truncate on mobile with no card view, expandable rows, or scroll affordance.
11. **Bottom nav incomplete** — Reports, Data Mgmt, and Settings are inaccessible from the mobile bottom tab bar.
12. **Dual navigation confusion** — Both a sidebar and bottom tab bar exist on mobile without clear hierarchy.
13. **Form inputs use placeholder-only labels** — Placeholder text disappears on focus, failing WCAG understandable criteria.
14. **Small touch targets on table icons** — Status indicators and action icons likely fail the 44×44px minimum.
15. **Screen reader experience likely poor** — Heavy use of `generic` roles in the accessibility tree; limited semantic HTML and ARIA.

### Minor (Severity 2) — Should Be Addressed

16. **Entity concept unexplained** — Settings > Manage Entities shows items like "Walton (RentalProperty) - Owner: N/A" with no explanation of what entities are or why to create them.
17. **No primary CTA on dashboard** — Consider adding an action prompt (e.g., "Review 21 unmatched transactions").
18. **Income vs. Expenses chart appears broken** — Shows flat $0 line with no empty state messaging.
19. **Transaction subtitle misleading** — "Monitor budgets and accounts side by side" but layout is tabbed, not side-by-side.
20. **Reports chart x-axis bug** — "Feb 2026" repeated on the Planned vs Actual scatter chart.
21. **Required fields not marked** — No visual indication of required form fields anywhere.
22. **Button styling inconsistent** — Multiple button variants used without a clear pattern.
23. **Entity edit/delete icons inconsistent** — Large colored circle buttons differ from icon-only buttons used elsewhere.
24. **Heading hierarchy** — Dashboard has only an H4, no H1. Other pages may have similar issues.
25. **Possible contrast failures** — Light gray placeholder text and small status icons may fail 4.5:1 contrast.
26. **Focus indicator visibility** — May be suppressed by CSS framework.
27. **Color-dependent status indicators** — Transaction status uses letter + color, but icons are very small.

### Cosmetic (Severity 1) — Fix If Time Permits

28. **Version number in sidebar** — "Version: 0.3.31" visible to end users signals beta status.
29. **Refresh icon purpose unclear** — No tooltip or label explaining what the refresh button does on dashboard cards.
30. **App logo** — Mascot image doesn't immediately convey "finance" — consider pairing with a tagline.
31. **No skeleton screens** — Loading states use spinners rather than skeleton UI.
32. **Sticky header + bottom nav consume ~120px** — Slightly reduces usable viewport on mobile but not a major issue.

---

## Addendum: Owner Design Decisions (March 29, 2026)

The following decisions were made by the product owner during the review walkthrough. These supersede or refine the original review recommendations where applicable. All decisions have been propagated to `docs/design_system.md`, `docs/needed_features.md`, `docs/product_requirements.md`, and `AGENTS.md`.

### Persona Priority
**Decision: Equal priority.** Both family budget managers and accountants/bookkeepers see the same interface. No features are hidden or simplified for one group. Tooltips bridge the knowledge gap.

### Terminology
**Decision: Keep current labels, add tooltips on hover/tap.** Labels like "Reconciled," "Cleared," "Budget Register," and "Account Register" remain unchanged. Tooltips via `q-tooltip` explain each term for non-experts. Labels are NOT renamed.

### Mobile Investment
**Decision: Full mobile overhaul.** Fix all critical bugs (sidebar auto-open, viewport zoom, font size) AND redesign mobile nav (complete bottom tab bar with "More" menu), implement responsive table patterns (card views), and optimize mobile forms. This is a top priority.

### Budget Page Trash Icons
**Decision: Soft delete with restore.** The trash icon marks the imported transaction as deleted, but it can be restored from the "Deleted" tab on the Budget page. Restyle the icon to communicate soft-delete intent (muted/outlined instead of solid red, tooltip explaining restore capability). Ensure the "Deleted" tab is discoverable.

### Onboarding Approach
**Decision: Replace wizard expansion with guided tour.** The existing SetupWizardPage (family → entities → accounts) is sufficient for initial setup and should NOT be expanded with budget/goal steps. Instead, build a guided tour system: auto-triggered contextual prompts for new users + a persistent "Getting Started" checklist accessible anytime from sidebar/settings.

### Default Landing Page
**Decision: Budget page.** After login, users land on the Budget page instead of the Dashboard. The Budget page is the primary workspace where most actions happen. The Dashboard remains accessible via navigation but is deprioritized.

### Transactions Page Subtitle
**Decision: Remove entirely.** The subtitle "Monitor budgets and accounts side by side" is misleading and unnecessary. The page title "Transactions" is sufficient.

### Settings > Family Member List
**Decision: Add member list.** The Manage Family/Group page must display current family members with role (owner/member), join date, and remove option (owner-only). Currently only shows an invite email field.

### Income vs. Expenses Chart
**Decision: Needs investigation.** Unclear if the flat $0 line is a data issue or rendering bug. Investigate before deciding on fix approach.
