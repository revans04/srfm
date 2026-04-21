---
name: srfm-design
description: Use this skill to generate well-branded interfaces and assets for Steady Rise Family Money (SRFM), either for production or throwaway prototypes/mocks/etc. Contains essential design guidelines, colors, type, fonts, assets, and UI kit components for prototyping.
user-invocable: true
---

Read the README.md file within this skill, and explore the other available files.

If creating visual artifacts (slides, mocks, throwaway prototypes, etc), copy assets out and create static HTML files for the user to view. If working on production code, you can copy assets and read the rules here to become an expert in designing with this brand.

If the user invokes this skill without any other guidance, ask them what they want to build or design, ask some questions, and act as an expert designer who outputs HTML artifacts _or_ production code, depending on the need.

## Quick orientation

- **Two modes, never mixed per page:** Coaching (Dashboard, Budget — comfortable density, encouraging) vs Auditing (Transactions, Registers — compact density, precise).
- **Color = state, not style.** Neutrals carry ~90% of the UI. `--sr-primary` (#1D4ED8) is reserved for interactive elements. Semantic colors (positive/warning/negative) always pair with text or icon.
- **Cards summarize. Tables audit.** Ledgers are never wrapped in decorative cards.
- **Inter only**, single type scale, tabular numerals on every numeric column.
- **Google Material Icons only**, referenced by name, never inlined SVG. No emoji in chrome.
- **Spacing is strict 4/8/12/16/24/32** — map to Quasar `q-pa-*` / `q-ma-*`. No arbitrary values.
- **One primary action per surface.** Secondary = outline/flat. Destructive = separated, always confirmed.
- **Inspector pattern** (right drawer) beats modals for ledger editing on desktop; same component re-renders as full-screen dialog on mobile.

## Files

- `README.md` — full brand, content, visual, and iconography foundations.
- `colors_and_type.css` — CSS custom properties for every token. Import this in every artifact.
- `assets/` — logo-full.png, logo-sm.png, logo-icon.png (turtle + upward arrow mascot).
- `preview/` — small HTML cards documenting the system (colors, type, spacing, components).
- `ui_kits/srfm-app/` — React/JSX recreation of the app shell, Dashboard, Budget, Transactions + Inspector. Start here when mocking real screens.

## Starter snippet

```html
<link rel="stylesheet" href="colors_and_type.css">
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
<link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet">
```

Then compose with tokens: `background: var(--sr-surface)`, `color: var(--sr-primary)`, `border-radius: var(--sr-radius-lg)`, `box-shadow: var(--sr-shadow-subtle)`.
