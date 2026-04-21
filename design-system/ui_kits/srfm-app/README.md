# SRFM App — UI Kit

Pixel-level recreation of the Steady Rise Family Money web app (Quasar SPA), simplified to HTML/React for prototyping.

## What's here

- `index.html` — interactive click-through: sidebar shell + Budget page (Coaching mode) + Transactions page (Auditing mode) + an Inspector drawer. Use the sidebar and filter chips to navigate.
- `AppShell.jsx` — persistent left drawer with brand lockup + nav list.
- `BudgetPage.jsx` — coaching surface: "Left to Budget" hero, progress bars, summary cards.
- `TransactionsPage.jsx` — auditing surface: sticky filter bar + ledger table + inspector.
- `Inspector.jsx` — right-side edit drawer (replaces modal on desktop).
- `Primitives.jsx` — SrButton, SrChip, SrInput, StatBadge, ProgressRow.

## Conventions

- All color, type, spacing from `colors_and_type.css` at the project root — never hard-coded.
- Google Material Icons via webfont — referenced by name (`<span class="material-icons">edit</span>`).
- Inter via Google Fonts `<link>`.
- Density: BudgetPage uses Comfortable; TransactionsPage uses Compact — per the "never mix" rule.
