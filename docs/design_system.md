# design_system.md

## 1. Design Principles
- `[ENFORCED]` Finance UX must prioritize trust, clarity, and error prevention over novelty.
- `[ENFORCED]` Primary users are household budget maintainers (owners/admins) and collaborating members; workflows must support frequent month-to-month operations with low friction.
- `[ENFORCED]` The application serves two distinct personas: (1) **family budget managers** — non-experts migrating from EveryDollar, Mint, or spreadsheets who expect guided workflows, plain language, and mobile-first convenience, and (2) **accountants/bookkeepers** — power users migrating from Quicken or Excel who expect data density, reconciliation workflows, and detailed reporting. UI must serve both without alienating either.
- `[ENFORCED]` Core workflows are auth/onboarding, entity selection, monthly budget editing, transaction entry/matching, reconciliation, account/snapshot management, and reporting.
- `[ENFORCED]` UX tradeoff is speed for repeated tasks, with explicit safety gates for destructive or high-impact actions.
- `[ENFORCED]` Progressive disclosure is required for advanced actions. Accounting-specific terminology (e.g., "Reconciled," "Cleared," "Budget Register," "Account Register") must be accompanied by tooltips, contextual help, or a plain-language alternative for non-expert users.
- `[ENFORCED]` Empty states must include task-specific guidance copy, an actionable next step (link or button), and never display alongside a conflicting loading state.
- `[SUGGESTED]` Use direct, utilitarian UI language; avoid decorative complexity in data-dense screens.
- `[ASSUMPTION/UNCLEAR]` No formal brand voice guide exists in repo docs; current implementation implies a practical, professional tone.

## 2. Visual Identity
- `[ENFORCED]` Use semantic tokens, not ad hoc colors.
- `[ENFORCED]` Canonical color source is `q-srfm/src/css/quasar.variables.scss` plus app-level semantic CSS vars in `q-srfm/src/css/app.scss`.
- `[ENFORCED]` Quasar semantic tokens are `primary`, `secondary`, `accent`, `positive`, `negative`, `info`, `warning`, `dark`.
- `[ENFORCED]` Surface/text tokens are `--color-surface-page`, `--color-surface-card`, `--color-surface-subtle`, `--color-surface-muted`, `--color-outline-soft`, `--color-text-primary`, `--color-text-muted`.
- `[ENFORCED]` Typography baseline is in `q-srfm/src/css/app.scss`: body uses Inter with 1.45 line-height; headings are `h1` 2.5rem, `h2` 2rem, `h3` 1.5rem, `h4` 1.25rem, weight 600.
- `[ENFORCED]` Base font size must be at least 16px on mobile viewports (≤430px) to prevent iOS Safari auto-zoom on input focus and ensure readability. The current 14px default is a known violation — remediation is required.
- `[ENFORCED]` Radius tokens are `--radius-sm` (8px), `--radius-md` (12px), `--radius-lg` (18px).
- `[ENFORCED]` Shadow tokens are `--shadow-soft` and `--shadow-subtle`.
- `[SUGGESTED]` Prefer pill buttons/chips consistent with global Quasar overrides in `app.scss`.
- `[ENFORCED]` Inconsistency: `q-srfm/quasar.config.ts` brand hex values conflict with `q-srfm/src/css/quasar.variables.scss`; canonical direction is `quasar.variables.scss` as source of truth and alignment of `quasar.config.ts`.
- `[ENFORCED]` Over-budget states use `$warning` (#E65100), never `$negative`. Red is reserved for destructive actions and errors only.

## 3. Layout System
- `[ENFORCED]` Use Quasar grid (`row`, `col-*`, `q-col-gutter-*`) and spacing utilities (`q-pa-*`, `q-ma-*`) as the primary layout system.
- `[ENFORCED]` Desktop shell uses left drawer navigation and mobile uses header/footer navigation as in `q-srfm/src/layouts/MainLayout.vue`.
- `[ENFORCED]` Responsive behavior must use Quasar breakpoints (`col-12`, `col-md-*`, `col-lg-*`) and `$q.screen.lt.md` for logic-level switches.
- `[ENFORCED]` Mobile is a first-class target; the app is used on phones at least 50% of the time, primarily for on-the-go transaction entry. Mobile usability is never optional. Users migrating from EveryDollar and Mint expect a polished, mobile-native experience.
- `[ENFORCED]` Mobile-first adaptation is required for dialogs, sticky actions, and dense tables.
- `[ENFORCED]` All new UI must be validated at 375px, 390px, and 430px viewport widths with no horizontal scrolling, broken layout, or inaccessible interactions.
- `[ENFORCED]` Transaction entry is the highest-priority mobile workflow; its form must open in a bottom sheet or full-screen modal, use native mobile input types, and be submittable without scrolling on a standard phone.
- `[ENFORCED]` Navigation must be reachable with one thumb; avoid patterns requiring precise tap or hover to access primary actions.
- `[ENFORCED]` Mobile navigation must be consolidated into a single primary pattern. The sidebar drawer must NOT auto-open on page navigation at mobile breakpoints — it must default to closed, with the bottom tab bar serving as the primary mobile nav. The bottom tab bar must provide access to all major sections (currently missing Reports, Data Mgmt, and Settings — these must be accessible via a "More" menu item or equivalent).
- `[ENFORCED]` Data-dense screens must preserve clear hierarchy: title, filters, content, actions.
- `[ENFORCED]` Data tables on mobile must use a responsive pattern — either card-based views, expandable rows, or horizontally scrollable containers with a visible scroll affordance. Simply truncating columns without interaction is not acceptable.
- `[ENFORCED]` Form layout must use grouped card sections with labels and validation.
- `[SUGGESTED]` Prefer `.panel-card`/tokenized cards over ad hoc panel wrappers.
- `[ENFORCED]` Inconsistency: legacy utility classes in `q-srfm/src/css/budget.scss` (`rounded-3`, `rounded-5`, etc.) conflict with token radii; canonical direction is token radii only.

## 4. Component Architecture
- `[ENFORCED]` Base primitive layer is Quasar (`q-btn`, `q-input`, `q-select`, `q-card`, `q-dialog`, `q-banner`, `q-tabs`, `q-table`/`q-markup-table`).
- `[ENFORCED]` Reuse existing domain primitives before adding new ones, including `EntitySelector`, `MonthSelector`, `CurrencyInput`, `TransactionDialog`, `TransactionForm`, tile cards, and chart cards.
- `[ENFORCED]` Page components orchestrate state/data; reusable components focus on rendering/local interaction.
- `[ENFORCED]` Extend existing components when differences are minor; create a new component only when behavior/state is materially different and reused in 2+ contexts.
- `[ENFORCED]` Props/emits must be explicitly typed.
- `[ENFORCED]` Use Quasar variant system first (`flat`, `outlined`, `unelevated`, `dense`); new variants require token-backed styles plus this document update.
- `[ENFORCED]` Button hierarchy (three tiers, outline retired):
  - **Primary (filled)** — the single most important action on a surface. `color="primary" unelevated`. One per surface.
  - **Soft (tonal)** — standalone secondary and destructive-secondary actions. Filled with the semantic color's soft tint + strong-color text/icon. Classes: `btn-soft--primary`, `btn-soft--negative`, `btn-soft--warning` (defined in `app.scss`). Canonical treatment for page-header action clusters (Edit, Delete, Archive). Safe to pair primary-soft with negative-soft as a coordinated action pair.
  - **Flat (tertiary)** — `flat` or `flat color="primary"`. Used for: dialog Cancel, inline/row-level icon actions (edit, delete, refresh inside tables), close X buttons, repeated actions. Icon-only flat **must** have `<q-tooltip>` on desktop.
  - Destructive variant by context: page-header → soft-negative (`btn-soft--negative`); final-confirm dialog primary → filled-negative (`color="negative" unelevated`); row-level trash → flat-negative-icon with tooltip + confirm dialog.
  - **Outline is retired.** Never used anywhere in the codebase by the time it was evaluated. Do not introduce new `outline`/`outlined` buttons. If you see one during a refactor, migrate to soft or flat based on context.
- `[ENFORCED]` Inconsistency: some files mix Vuetify-era APIs/classes (`variant`, `.v-stepper-*`, `.v-table`) in Quasar code; canonical direction is Quasar-only API/classes. The `SetupWizardPage.vue` is a known offender using Vuetify CSS selectors and variables.

## 5. Interaction & States
- `[ENFORCED]` Hover and active interactions must be subtle and token-based.
- `[ENFORCED]` Focus visibility must remain visible for keyboard users.
- `[ENFORCED]` Disabled states must use component-native semantics and remain legible.
- `[ENFORCED]` Loading states must use one of three patterns: inline spinner/skeleton for component loads, centered spinner for page loads, button loading for action mutations.
- `[ENFORCED]` Empty states must include task-specific copy and next action.
- `[ENFORCED]` Error states must be explicit through banner/notify/dialog with actionable wording.
- `[ENFORCED]` Success states should use short `$q.notify` confirmations for write completion.
- `[ENFORCED]` Destructive operations (delete account, delete transaction, delete entity, dismiss/ignore items) must require a confirmation step that names the affected item and states the impact. The confirmation action label must be specific (e.g., "Delete Account" not just "OK").
- `[ENFORCED]` Destructive action buttons must be visually distinct — use `negative` color and never style them as plain text links or small icons without labels. The current transaction edit modal "Delete" link and budget page red trash icons for unmatched transactions are known violations.
- `[ENFORCED]` All successful write operations (save, delete, create) must provide visible feedback via `$q.notify` toast. Silent success is not acceptable — users must know their action worked.

## 6. Accessibility Standards
- `[ENFORCED]` WCAG minimum contrast is 4.5:1 for normal text and 3:1 for large text/UI components.
- `[ENFORCED]` All interactive elements must meet a 44×44px minimum tap target size for mobile usability.
- `[ENFORCED]` Form inputs must use correct mobile input types (`number`, `date`, `tel`, `email`) to trigger appropriate on-screen keyboards.
- `[ENFORCED]` All interactive controls must be keyboard reachable and operable.
- `[ENFORCED]` Inputs and icon-only actions must have semantic labeling (`label`, `aria-label`, `aria-required` when appropriate).
- `[ENFORCED]` Form inputs must have visible persistent labels — not placeholder-only labels that disappear on focus. Floating labels that remain visible when the field has content are acceptable.
- `[ENFORCED]` Dialogs must trap focus and support Escape close unless persistence is required for data safety.
- `[ENFORCED]` Validation and error text must be programmatically associated to fields where possible.
- `[ENFORCED]` Table/report interactions must be keyboard reachable.
- `[ENFORCED]` Custom motion must support reduction via `prefers-reduced-motion`.
- `[ENFORCED]` The viewport meta tag must NOT include `user-scalable=no` or `maximum-scale=1`. These prevent pinch-to-zoom accessibility and violate WCAG 2.1 AA Success Criterion 1.4.4 (Resize Text). The current viewport meta is a known violation requiring remediation.
- `[ENFORCED]` Every page must have a proper heading hierarchy starting with a single `h1` for the page title. The current dashboard uses only an `h4` — this is a known violation.
- `[ENFORCED]` Required form fields must be visually indicated (e.g., asterisk, "required" text) and programmatically marked with `aria-required`.
- `[ASSUMPTION/UNCLEAR]` No automated accessibility gate is defined in repo; manual keyboard and screen-reader verification is required per release until tooling is added.

## 7. Theming & Tokens
- `[ENFORCED]` Token definition locations are `q-srfm/src/css/quasar.variables.scss` for Quasar semantic theme tokens and `q-srfm/src/css/app.scss` for app semantic surface/text/radius/shadow tokens.
- `[ENFORCED]` Dark mode is not currently implemented in production.
- `[ENFORCED]` Do not add component-level dark mode forks; dark mode can only be added via global token sets and update to this document.
- `[ENFORCED]` Prefer token extension over per-component hardcoded overrides.
- `[ENFORCED]` Direct raw hex is prohibited where semantic tokens exist.
- `[ENFORCED]` Inconsistency: chart/report colors are widely hardcoded; canonical direction is centralized token-derived palette mapping.

## 8. Animation & Motion
- `[ENFORCED]` Global Quasar animation pack is disabled (`animations: []`); custom motion must be minimal and purposeful.
- `[ENFORCED]` Duration scale is 150ms (micro-feedback), 200ms (standard state changes), and 300ms (emphasis transitions such as drawer/dialog).
- `[ENFORCED]` Easing should use `ease` or `ease-in-out` unless library behavior already applies.
- `[ENFORCED]` Animation is allowed for navigation affordance and context continuity.
- `[ENFORCED]` Animation is prohibited for critical status messaging, numeric financial values, and high-frequency updates.
- `[ENFORCED]` Nonessential transitions must be disabled when reduced-motion preference is present.

## 9. Content & Microcopy Rules
- `[ENFORCED]` Tone is concise, direct, and non-marketing.
- `[ENFORCED]` Button labels must be verb-first and specific (`Save Budget`, `Delete`, `Finalize`, `Add Split`).
- `[ENFORCED]` Confirmation text must state impact and scope (`Delete 14 selected transactions?`).
- `[ENFORCED]` Error text must include what failed, why if known, and next step.
- `[ENFORCED]` Validation text must be field-specific plain language (`Date is required`, `Amount must be greater than 0`).
- `[SUGGESTED]` Avoid passive/system-internal phrasing in user-facing errors.
- `[ENFORCED]` Accounting terminology must be accompanied by tooltip help via Quasar `q-tooltip` on hover/tap. **Owner decision: labels remain as-is; tooltips bridge the gap (no renaming).** Specific tooltips: "Reconciled" → "Verified against your bank statement"; "Cleared" → "Transaction has posted to your account"; "Uncleared" → "Transaction hasn't posted yet"; "Budget Register" → "Transactions matched to your budget categories"; "Account Register" → "Transactions imported from your bank account"; "Match Bank Transactions" → "Link imported bank transactions to your budget entries."
- `[ENFORCED]` The Transactions page subtitle "Monitor budgets and accounts side by side" must be removed entirely. **Owner decision: no subtitle needed — the page title "Transactions" is sufficient.**
- `[SUGGESTED]` Consider offering a "simplified view" toggle for data-dense pages (Transactions, Reports) that reduces visible columns and hides advanced filters, serving non-expert users who just want to see "what did I spend."
- `[ENFORCED]` Dates are always displayed as `mm/dd/yyyy`. Storage format is `YYYY-MM-DD` (ISO) for sortability; conversion happens at the input/display boundary.

## 10. Agent Implementation Rules
- `[ENFORCED]` Reuse existing components and Quasar primitives before creating new components.
- `[ENFORCED]` Do not introduce inline styling when tokenized styles or scoped classes can express the same rule.
- `[ENFORCED]` Follow documented spacing scale and Quasar spacing utilities; avoid arbitrary pixel spacing.
- `[ENFORCED]` Keep border radius consistent with token values.
- `[ENFORCED]` Do not invent new visual variants without updating this file and token definitions.
- `[ENFORCED]` Maintain page-orchestration plus reusable-component pattern.
- `[ENFORCED]` When modifying inconsistent legacy styling, migrate touched code toward tokenized canonical patterns.

## 11. Extension Protocol
- `[ENFORCED]` New token process: add semantic token in `quasar.variables.scss` or `app.scss`, replace at least one real usage, and document purpose/permitted use in this file.
- `[ENFORCED]` New component process: verify no existing primitive/composite fits, define typed props/events/slots and variant bounds, and include responsive/accessibility behavior in PR notes.
- `[ENFORCED]` Human approval is required before adding dark mode, replacing global typography family/scale, introducing a second styling system, or altering primary navigation layout behavior.
- `[SUGGESTED]` Broad visual refactors should include desktop/mobile before-after screenshots.

## 12. Anti-Patterns to Avoid
- `[ENFORCED]` Direct hex color usage where semantic tokens exist.
- `[ENFORCED]` Arbitrary spacing values outside Quasar spacing/tokens.
- `[ENFORCED]` Component duplication for minor variant differences.
- `[ENFORCED]` Mixed radius systems (`rounded-*` legacy classes and token radii together).
- `[ENFORCED]` Non-accessible interactions (click-only controls without keyboard path, missing labels).
- `[ENFORCED]` Vuetify-era classes/APIs in Quasar components.
- `[ENFORCED]` Inline styles for layout/typography/colors except temporary debug code.
- `[ENFORCED]` Silent async failure states.
- `[ENFORCED]` Desktop-only layouts: any layout, dialog, or form that requires horizontal scrolling or produces broken/overlapping elements below 430px width.
- `[ENFORCED]` Tap targets below 44×44px on interactive elements.
- `[ENFORCED]` Using text input type for numeric or date fields (prevents appropriate mobile keyboard).
