# Steady Rise Family Money — Design System

A design system for **Steady Rise Family Money (SRFM)**, a family-focused financial management web app. Built to serve two audiences with one interface: **family budgeters** (moving off EveryDollar / Mint) and **accountants / bookkeepers** (moving off Quicken / Excel).

> **Core tension solved by this system:** consistency and clarity over flexibility. Rules are opinionated by design.

---

## Product at a glance

- **App type:** SPA — Quasar (Vue 3) on the frontend, .NET 8 API on Cloud Run, Supabase/Postgres for data.
- **Shipping surface:** Web (`budget-buddy-a6b6c.web.app`) — desktop-first with a full mobile overhaul in progress.
- **Navigation sections:** Dashboard, **Budget** (default landing), Transactions, Accounts, Reports, Data Mgmt, Settings.
- **Personas (equal priority):**
  - **Family budget manager** — non-expert, mobile-first, coming from EveryDollar.
  - **Accountant / bookkeeper** — power user, desktop-heavy, expects dense ledgers, reconciliation, exports.
- **Key differentiators:** multi-entity support (family + rental properties), true ledger/register with reconciliation, import + match workflow.

### The two mental modes the system is built around

| Mode          | Where it appears           | Feel                               | Density     |
| ------------- | -------------------------- | ---------------------------------- | ----------- |
| **Coaching**  | Dashboard, Budget, Goals   | Encouraging, progress-oriented     | Comfortable |
| **Auditing**  | Transactions, Registers    | Precise, dense, accountant-friendly | Compact     |

Every page commits to one mode — they are never mixed inside the same view.

---

## Sources (for the reader who has access)

This system was distilled from:

- **Codebase:** `srfm/` (mounted via File System Access API) — the Quasar SPA at `srfm/q-srfm/`, API at `srfm/api/`.
- **Rules doc:** `srfm/steady_rise_ui_rules.md` — the authoritative v0.1 "Steady Rise UI Rules".
- **UX review:** `srfm/UI_UX_Review_SteadyRise.md` (March 29, 2026) — findings, severity ratings, and owner decisions.
- **Rework prompt:** `srfm/UI_UX_REWORK_PROMPT.md` — the 10-phase implementation plan currently in flight.
- **Mockup prompt:** `srfm/UI_MOCKUP_PROMPT.md` — target look/feel for the in-progress redesign.
- **Brand assets:** `uploads/family-funds{,-sm,-icon}.png` — the turtle-with-upward-arrow mascot.

Key files within the codebase:

- `q-srfm/src/css/quasar.variables.scss` — SCSS color source of truth.
- `q-srfm/quasar.config.ts` — `framework.config.brand` token map.
- `q-srfm/src/css/app.scss` — semantic CSS vars (`--color-surface-*`, `--shadow-*`, `--radius-*`).
- `q-srfm/src/layouts/MainLayout.vue` — nav shell, mobile header, bottom tab bar, More menu.
- `q-srfm/src/pages/{Budget,Transactions,Dashboard,Accounts}Page.vue` — reference implementations of the two modes.
- `q-srfm/src/components/LedgerTable.vue`, `TransactionDialog.vue`, `EntitySelector.vue` — key recurring components.

---

## Index of this design system

```
.
├── README.md                ← you are here
├── SKILL.md                 ← invokes this system as an Agent Skill
├── colors_and_type.css      ← CSS custom properties for color + type tokens
├── assets/                  ← logos, mascot (full, small, icon-only)
├── preview/                 ← HTML cards surfaced in the Design System tab
├── ui_kits/
│   └── srfm-app/            ← Quasar-style UI kit: sidebar shell, budget page,
│                              transactions ledger, inspector, forms
└── (references srfm/ mount, not copied)
```

Use the cards in `preview/` for a at-a-glance tour. For anything production-bound, reference the live codebase — this system documents intent, not implementation.

---

## CONTENT FUNDAMENTALS

Steady Rise copy is **calm, specific, and verb-forward**. It is written to be trusted by an accountant and decoded by a parent.

### Tone

- **Calm, not cheerleading.** No "🎉 Great job!" No celebration states that imply the product knows more than it does.
- **Professional but not cold.** "Verified against your bank statement" beats "Reconciled ✓" beats "All good!"
- **Accountant-literate, family-readable.** Keep accounting labels ("Reconciled", "Cleared", "Budget Register") — bridge the gap with **tooltips**, never by renaming.

### Voice

- **Second-person ("you", "your")** for coaching surfaces: empty states, guided tips, onboarding.
- **Impersonal / label-style** for ledgers: column headers, status codes, filter chips.
- Never "we" (except rare banners: *"We detected legacy Savings categories…"*).

### Casing

- **Sentence case** for page titles, card titles, body text, buttons: `Add Transaction`, `Left to Budget`, `Mark as deleted`.
- **UPPERCASE** only on tab labels in the Transactions page (`BUDGET REGISTER`, `ACCOUNT REGISTER`, `MATCH BANK TRANSACTIONS`) — an accountant-software convention worth keeping.
- **Title Case** for entity/account names (user-provided: *Evans Family*, *Walton RentalProperty*).

### Dates, numbers, currency

- **Dates are always `mm/dd/yyyy`** — inputs, tables, chips, tooltips. Never `yyyy-mm-dd`, never `Mar 15`, never `March 15, 2026` except in card titles like "March 2026 Budget".
- **Currency is always prefixed `$`** with two decimals, tabular-aligned, negatives as `-$42.19` (not `($42.19)`). Colored red only for explicitly-negative amounts; over-budget uses amber.
- **Numbers right-align** in tables, use `font-variant-numeric: tabular-nums`, never mix weights across a column.

### Buttons & actions (verbs)

Use concrete verbs. Avoid vague labels.

| ✅ Good                       | ❌ Avoid |
| ----------------------------- | ------- |
| Add Transaction               | Submit  |
| Save Budget                   | OK      |
| Mark as deleted               | Remove  |
| Delete Account                | Delete  |
| Import Data                   | Go      |
| Create Default Budget         | Start   |

### Empty states — actionable, never decorative

Every empty state points to the next action.

- **Bad:** "No active goals" + tiny `+` icon
- **Good:** "No active goals yet. **Set your first savings goal →**"
- **Bad:** "Connect or import statements to see upcoming bills" *(no link anywhere)*
- **Good:** "No upcoming bills yet. **Import data →**"

### Tooltips — the knowledge bridge

Every accounting term gets a tooltip on hover/tap. Fixed copy:

- **Cleared** → "Transaction has posted to your account"
- **Uncleared** → "Transaction hasn't posted yet"
- **Reconciled** → "Verified against your bank statement"
- **Budget Register** → "Transactions matched to your budget categories"
- **Account Register** → "Transactions imported from your bank account"
- **Match Bank Transactions** → "Link imported bank transactions to your budget entries"

### Vibe

Think **Monarch Money** or **YNAB**, not **Mint**. Utilitarian-polished: cards with breathing room, ledger rows that feel like a spreadsheet, color used almost only for state. No emoji in UI chrome. Numbers are the hero — they are always tabular, always right-aligned in tables.

### Sample copy lifted from the product

- Page titles: `Budget`, `Transactions`, `Dashboard`, `Accounts`, `Reports`.
- Stat labels: `Left to Budget`, `Income Received`, `Savings Goals`, `Net Worth`, `Budget Progress`.
- Status codes: `C` (cleared), `U` (uncleared) — letter + color + tooltip, never color alone.
- Banners (calm, single line): *"We detected legacy Savings categories. Convert them to the new Savings Goals feature."*

---

## VISUAL FOUNDATIONS

### Motif

There is no motif. That is the motif. Steady Rise looks like **a clean ledger on a calm blue-gray sheet of paper**. Information hierarchy (spacing, typography, alignment) does the work that borders, cards, and color do in lesser finance apps.

### Color

- **Neutrals carry ~90% of the UI.** Page background is a quiet blue-gray (`#F5F7FB`), cards are pure white, borders are near-invisible (`rgba(148, 163, 184, 0.24)`).
- **Color communicates state, never style.**
  - Primary `#1D4ED8` — interactive elements only (links, active nav, primary buttons, filter chips when applied, **budget progress while funding/spending is under target**).
  - Positive `#16A34A` — income received, fully-funded categories, completed goals.
  - Warning `#E65100` — **over-budget spending** (attention, not error) and needs-attention banners.
  - Negative `#DC2626` — destructive actions, errors, negative transaction amounts. **Never used for "over budget" — that's a warning, not an error.**
- **Never use color alone** for status — always pair with text or an icon.
- **Never hard-code hex** in components — reference Quasar brand tokens (`color="primary"`, `text-negative`).

### Type

- **Inter**, single family, across every surface (desktop + mobile, headings + body + numerals).
- One type scale — mobile reduces H1/H2 by ~2px via container sizing, does NOT introduce new styles.
- Body text **14px minimum** on mobile (16px on viewports ≤430px to prevent iOS auto-zoom).
- **Tabular numerals always** — `font-variant-numeric: tabular-nums` on every numeric column.
- Weights used: 400 (body), 500 (labels, chips), 600 (headings, buttons), 700 (page title — sparing).

### Spacing

Strict 4 / 8 / 12 / 16 / 24 / 32 px scale. Mapped 1:1 to Quasar utilities (`q-pa-xs` → `q-pa-xl`). **Arbitrary values are not permitted.** Prefer whitespace over dividers; prefer spacing over borders; prefer alignment over chrome.

### Backgrounds

- **Page:** flat blue-gray `#F5F7FB` — never gradients, never patterns, never full-bleed imagery.
- **Cards:** pure white, subtle shadow **or** 1px border — never both.
- **Selected/hover rows:** `#EEF2FF` (primary-soft indigo tint).
- **No illustrations, no decorative imagery, no background textures.** The mascot (turtle + arrow) appears only in the sidebar brand, the mobile header, and the favicon.

### Animation

- **Minimal and purposeful.** Drawer slide, dialog fade, tab panel fade, filter-chip color flip.
- Durations: 150ms (micro — hover/color), 300ms (drawer). Ease: `ease` / `ease-in-out` defaults.
- **No bounces, no parallax, no scroll-linked effects, no entrance animations.**

### Interactive states

- **Hover on cards / rows:** no lift, no shadow — just a light surface shift to `#EEF2FF`.
- **Hover on buttons:** darkens primary by one step (e.g. `#1D4ED8` → `#1E40AF`).
- **Hover on icon buttons:** light gray background (`#F8FAFC`).
- **Pressed:** no scale change; Quasar's default ripple is suppressed visually.
- **Active nav:** filled soft-indigo pill (`#EEF2FF` bg, `#1D4ED8` text + icon).

### Borders & dividers

- **Prefer whitespace**, then a 1px `#E2E8F0` divider when absolutely needed.
- **Table rows:** thin 1px row separators only — no column dividers, no outlined container.
- **Cards:** either a light shadow (`0 6px 16px rgba(15, 23, 42, 0.06)`) or a border — never both.
- **Never outline entire sections** — sections are defined by spacing.

### Shadows

Two elevations only:

- **Subtle** (default cards, tiles): `0 6px 16px rgba(15, 23, 42, 0.06)`
- **Soft** (dialogs, popups): `0 18px 36px rgba(15, 23, 42, 0.08)`

No inner shadows. No ambient ring. No glow.

### Corner radii

- **Inputs, small cards:** 12px
- **Primary cards:** 18px
- **Buttons & chips:** 999px (fully pill)
- **Page header band, sticky filters:** 0 (flush)

### Use of transparency & blur

- **No backdrop-blur.** No glass surfaces.
- Transparency is used **only** for borders (`rgba(148, 163, 184, 0.24)`) and occasional chip backgrounds (`rgba(37, 99, 235, 0.08)`).

### Imagery

There is effectively no imagery in-app. The only image asset is the mascot (a friendly green turtle with a blue upward arrow — "steady rise"). When imagery is ever needed (e.g. empty-state placeholder), it should be **neutral, 2D, and calm** — no photography, no gradients, no illustrations beyond the existing mascot.

### Layout rules

- **Persistent left drawer** (~220px) on desktop — white, low-chrome, soft indigo active state.
- **Mobile header** — solid primary-blue bar, white text, logo + product name.
- **Mobile bottom tab bar** — 5 items: Dashboard, Budget, Transactions, Accounts, **More** (opens a bottom sheet with Reports / Data Mgmt / Settings / Logout).
- **Sticky filters** at the top of every ledger page on desktop. On mobile they collapse into a bottom sheet.
- **Inspector pattern** (right-side drawer) preferred over modals for ledger editing on desktop. On mobile, the same component re-renders as a full-screen dialog.
- **One primary action per surface.** Secondary actions quieter. Destructive actions separated.

### Buttons — three tiers, no outline

Steady Rise uses **three** button variants. Earlier drafts included `outline`; it was never adopted in practice and is retired.

| Tier | Visual | Where | Quasar |
|---|---|---|---|
| **Primary** (filled) | Solid brand-blue pill, white text | The single most important action on a surface (Save, Create, Add Transaction) | `color="primary" unelevated` |
| **Soft** (tonal) | Soft-tint bg + strong-color text/icon, pill-rounded | Standalone secondary / destructive-secondary actions in page headers and action clusters (Edit, Delete, Archive) | `unelevated no-caps class="btn-soft--primary"` (or `--negative` / `--warning`) |
| **Flat** (tertiary) | Transparent bg, brand-color text/icon | Cancel in dialogs, close X, row-level icon actions, repeated inline controls | `flat` or `flat color="primary"` |

**Tokens used by soft:** `--color-surface-subtle` (primary-soft bg), `--color-negative-soft` (negative-soft bg), `--color-warning-soft` (warning-soft bg) + `--color-warning-strong-text`. All defined in `q-srfm/src/css/app.scss`.

**Pairing rules:**
- One primary per surface.
- Soft pairs are allowed across colors (Edit + Delete reads as a coordinated pair).
- Never two soft buttons of the *same* color on one surface — reads as indecision.
- Destructive → soft-negative for page-level, filled-negative only for final-confirm dialog primaries, flat-negative for row-level icons (always + tooltip + confirm dialog).

**Icon-only rule:** any icon-only button must have `<q-tooltip>` on desktop. Row-level destructive icons additionally require a confirmation dialog that names the affected item.

### Cards vs Tables — the rule that defines everything

> **Cards summarize. Tables audit.**

- **Cards** appear on Dashboard, Budget summary, Goals — 18px radius, subtle shadow, 4–6 max per view, minimal internal dividers.
- **Tables** appear on Transactions, Registers, Match Bank — flat surfaces, **never wrapped in decorative cards**, thin row separators, hover + selection as primary affordances.
- If a component starts as a summary and grows into detail, it **transitions from card → table** rather than expanding inside a card.

---

## ICONOGRAPHY

### The icon system

**Google Material Icons** — and nothing else.

- The app uses Quasar's built-in Material Icons integration (`material-icons` + `material-icons-outlined` extras in `quasar.config.ts`).
- Icons are referenced **by name**, not inlined as SVG: `<q-icon name="edit" />`, `icon="delete"`, `icon="refresh"`.
- **No mixing** of icon libraries on the same surface. No Font Awesome, Heroicons, Lucide, etc.
- **Custom icons** are introduced only when Material Icons has no equivalent and the concept is core to the product. (Currently: none.)

### Icon-usage principles

- **Icons reinforce text — they do not replace it.** Exception: dense tables, where icon-only cells get tooltips.
- **Icon-only buttons** require `q-tooltip` on desktop. Reserved for frequent actions (edit, delete, refresh, link).
- **Destructive actions are never icon-only** without confirmation.
- **Inline icons** are muted (`text-grey-7`) and slightly smaller than surrounding text unless conveying state.

### The icon set in use (by section)

From the codebase — these are the actual Material Icon names referenced:

- **Nav:** `dashboard`, `savings` (Budget), `format_list_bulleted` (Transactions), `account_balance` (Accounts), `trending_up` (Reports), `dataset` (Data Mgmt), `manage_accounts` (Settings), `more_horiz` (mobile More), `logout`.
- **Actions:** `edit`, `delete` / `delete_outline`, `close`, `add`, `refresh`, `search`, `event` (date), `schedule`, `clear_all`, `star` / `star_border`, `drag_indicator`.
- **Status / feedback:** `check_circle`, `error`, `warning`, `info`, `link` (matched), `link_off` (unmatched).

### Unicode / emoji

- **No emoji in UI chrome, ever.** Not in nav, not in buttons, not in empty states.
- Unicode characters may appear in status codes where density matters: the letter `C` / `U` / `R` in the Status column. These are always paired with color + tooltip.

### Logo / mascot

- **Primary mark:** a green turtle inside a white circle with a blue upward-rising arrow across the foreground. The name "Steady Rise" is always set in Inter 600.
- **Small mark (`logo-sm.png`):** the same mascot cropped tight — used in the desktop sidebar brand lockup and the mobile blue header.
- **Icon only (`logo-icon.png`):** used for favicons and tiny contexts.
- **Mascot tone:** friendly and literal to the name — turtles rise slowly but steadily. Do not redraw, re-color, or stylize. Use PNG assets as shipped.

### How to add a new icon

1. Find it in the Material Icons library (https://fonts.google.com/icons).
2. Reference by name — never inline the SVG.
3. If it doesn't exist in Material, pause and escalate. Substitute only the closest Material equivalent in the meantime, and flag it.

---

## Design caveats & open questions

- **Fonts:** Inter is loaded from Google Fonts via `<link>` in the UI kit — no local `.ttf` / `.woff` files are checked in. If the codebase eventually self-hosts Inter, add it to `assets/fonts/`.
- **Dark mode:** the rules spec dark mode as "future — remap tokens, not override components." No dark theme tokens exist yet in this system.
- **Tight CSS variable naming in-app:** the live app uses `--color-*` and `--shadow-*` prefixes; this design system uses `--sr-*` prefixes so the two can coexist in a single HTML file. Map accordingly when copying into production.
- **Mobile UI kit:** the kit here focuses on desktop. A mobile-specific kit (bottom tab bar, bottom sheets, card-based transaction list) is a natural v0.2 follow-up.
