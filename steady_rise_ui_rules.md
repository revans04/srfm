# Steady Rise UI Rules

**Purpose**  
This document defines the foundational UI/UX rules for Steady Rise. It exists to ensure the product remains **simple, trustworthy, efficient, and scalable** as features grow and as the user base spans families, power users, and accounting professionals.

These rules are **opinionated by design**. Consistency and clarity are more important than flexibility.

---

## 1. Core Design Principles

### 1.1 Calm, Not Decorative
Steady Rise should feel calm, stable, and professional. Visual excitement should never compete with financial clarity.

- Neutral surfaces are the default
- Color is used intentionally, not expressively
- Motion is minimal and purposeful

---

### 1.2 Hierarchy Over Chrome
Information hierarchy (spacing, typography, alignment) should do more work than borders, cards, or backgrounds.

- Prefer spacing over dividers
- Prefer typography over color
- Prefer alignment over decoration

---

### 1.3 Coaching vs Auditing
Steady Rise serves two mental modes:

- **Coaching mode** (Budget, Dashboard): encouraging, progress-oriented
- **Auditing mode** (Transactions, Registers): precise, dense, familiar to accountants

Each page must clearly commit to one primary mode.

---

## 2. Color Usage Rules

### 2.1 Color Philosophy
> Color communicates *state*, not *style*.

If an element is colored, it should answer a question for the user.

---

### 2.2 Quasar Color Package (Authoritative)

Steady Rise defines an explicit color palette mapped to Quasar’s brand tokens. These values are **authoritative** and should be defined once in `quasar.conf.js`.

---

#### Brand & Interaction Colors

| Token | Hex | Usage |
|------|-----|------|
| `primary` | `#2563EB` | Primary actions, links, active states |
| `secondary` | `#0F766E` | Rare secondary emphasis (optional) |
| `accent` | `#6366F1` | Very limited highlights only |

**Rules**
- `primary` is the dominant interaction color
- `secondary` and `accent` are optional and must remain rare
- Never introduce additional brand colors

---

#### Semantic Colors

| Meaning | Token | Hex | Usage |
|-------|------|-----|------|
| Success | `positive` | `#16A34A` | Income received, completed goals |
| Warning | `warning` | `#F59E0B` | Needs attention or review |
| Error / Destructive | `negative` | `#DC2626` | Over budget, delete actions |
| Info | `info` | `#0284C7` | Informational messaging |

**Rules**
- Semantic colors always pair with text or icons
- Never use semantic colors as table or card backgrounds
- `negative` should be rare and intentional

---

#### Neutral & Surface Colors

| Purpose | Token / Utility | Hex | Usage |
|-------|----------------|-----|------|
| Page background | `bg-grey-1` | `#F8FAFC` | App background |
| Card / table surface | `white` | `#FFFFFF` | Primary reading surfaces |
| Border / divider | `grey-3` | `#E2E8F0` | Subtle separation |
| Muted text | `grey-7` | `#475569` | Metadata, captions |
| Primary text | `grey-9` | `#0F172A` | Main body text |

**Rules**
- Neutral colors should represent ~90% of the UI
- Prefer contrast via typography and spacing
- Avoid colored backgrounds for data-dense areas

---

### 2.3 Quasar Implementation Guidance

Define these tokens in `quasar.conf.js`:

```js
brand: {
  primary: '#2563EB',
  secondary: '#0F766E',
  accent: '#6366F1',
  positive: '#16A34A',
  warning: '#F59E0B',
  negative: '#DC2626',
  info: '#0284C7'
}
```

Rules:
- Reference colors by token name (`color="primary"`, `text-negative`)
- Do not hard-code hex values in components
- Dark mode should remap tokens, not override components

---

- Define colors once in `quasar.conf.js` under `framework.config.brand`
- Reference colors by token name (`color="primary"`, `text-negative`)
- Do not hard-code hex values in components
- Dark mode (future) should remap tokens, not override components

---

### 2.4 Color Anti-Patterns

- Hard-coded hex colors in components
- Decorative colored cards
- Color-only status indicators
- Using `secondary` or `accent` to imply importance

---

## 3. Card and Table Usage

### 3.1 Core Rule
> Cards summarize. Tables audit.

---

### 3.2 Card Usage

Cards are appropriate for:
- Dashboards
- High-level summaries
- Goal progress
- KPIs and trends

Card rules:
- Soft radius (10–12px)
- Either a light shadow *or* a border — never both
- Minimal headers
- Avoid internal dividers; use spacing instead
- Limit visible cards per view (4–6 max)

---

### 3.3 Table Usage

Tables are appropriate for:
- Transactions
- Registers
- Imports and reconciliation
- Anything requiring precision or bulk review

Table rules:
- Flat surfaces (no card chrome)
- Thin row separators
- No heavy borders
- Hover and selection are primary affordances

Typography rules:
- Slightly smaller than standard body text
- Numeric columns right-aligned
- Use tabular numbers

---

### 3.4 Hybrid Rule
If a component begins as a summary and evolves into detail, it should transition from a card to a table — not expand endlessly inside a card.

---

## 4. Filter Design Rules

### 4.1 Filter Philosophy
> Filters define scope. They are not decoration.

Filters must feel explicit, persistent, and reversible.

---

### 4.2 Placement

- Filters always live above tables
- Filters are sticky on desktop
- Filters collapse into sheets on mobile

---

### 4.3 Visual Style

- Flat inputs
- Low-radius controls
- No colored backgrounds
- Applied filters appear as removable chips

---

### 4.4 Behavioral Rules

- Date range is always visible
- "Clear all" is always available
- Filters apply immediately or via a single Apply action

---

### 4.5 Anti-Patterns

- Hiding filters behind icons on desktop
- Nesting filters inside cards
- Excessive animation

---

## 5. Page Styling Rules

### 5.1 Page Hierarchy

Every page should clearly answer:
1. Where am I?
2. What context am I in?
3. What can I do?
4. What data am I viewing?

---

### 5.2 Page Headers

Rules:
- One page title
- Context (entity, month) as secondary text
- Primary actions aligned right
- Avoid multiple stacked action rows

---

### 5.3 Spacing and Density

- Use whitespace before borders
- Dense pages may reduce padding
- Friendly pages may increase breathing room

---

### 5.4 Dividers and Borders

- Prefer whitespace over lines
- When necessary, use 1px light dividers
- Avoid outlining entire sections

---

## 6. Desktop vs Mobile Layout Rules

### 6.1 Desktop Philosophy
> Efficiency over friendliness.

- Higher information density is acceptable
- Side-by-side layouts encouraged
- Tables are first-class citizens
- Inspectors preferred over modals for power workflows

---

### 6.2 Desktop Layout Patterns

- 2–3 column layouts using `QLayout`, `QPage`, and `QDrawer`
- Persistent left navigation via `QDrawer`
- Right-side panels implemented with conditional drawers or layout slots
- Sticky headers and filter bars using `position: sticky` combined with Quasar spacing utilities

**Quasar guidance:**
- Prefer `QPage` padding controls over manual container divs
- Use layout slots and flex utilities (`row`, `col`, `items-center`, `justify-between`) for alignment

---


- 2–3 column layouts
- Persistent left navigation
- Right-side panels for inspectors or summaries
- Sticky headers and filter bars

---

### 6.3 Mobile Philosophy
> Focus over completeness.

Mobile users are reviewing, adding, and spot-checking — not auditing.

---

### 6.4 Mobile Layout Rules

- Single-column layouts using `col-12`
- Cards stack vertically using `q-gutter-md`
- Tables simplify or drill into detail views
- Filters appear in bottom sheets via `QDialog` + `position: bottom`
- Inspectors become full-screen dialogs on mobile

**Quasar guidance:**
- Use Quasar breakpoint helpers (`$q.screen`) instead of custom media queries
- Layout changes should be declarative in templates, not duplicated components

---


- Single-column layouts
- Cards stack vertically
- Tables simplify or drill into detail views
- Filters appear in bottom sheets
- Inspectors become full-screen sheets

---

### 6.5 Component Reuse Rule

The same component should:
- Render as a sidebar on desktop
- Render as a bottom or full-screen sheet on mobile

Behavior and logic remain shared.

---

## 7. Typography & Spacing Rules

### 7.1 Typography Philosophy
> Typography is the primary visual system.

If the UI feels noisy, unclear, or heavy, the first fix should be typography and spacing — not color, borders, or cards.

Steady Rise typography should feel:
- Calm
- Modern
- Neutral
- Highly readable for dense financial data

---

### 7.2 Font Family

**Primary font:** Inter (preferred)

Fallbacks:
- system-ui
- -apple-system
- BlinkMacSystemFont
- "Segoe UI"

Rules:
- Use a single font family across the application
- Avoid mixing display and body fonts
- Prioritize legibility over personality

**Quasar guidance:**
- Set the font family globally via Quasar CSS variables or main layout stylesheet
- Do not override fonts at the component level
- Typography consistency should come from Quasar utility classes, not custom CSS overrides

---


**Primary font:** Inter (preferred)

Fallbacks:
- system-ui
- -apple-system
- BlinkMacSystemFont
- "Segoe UI"

Rules:
- Use a single font family across the application
- Avoid mixing display and body fonts
- Prioritize legibility over personality

---

### 7.3 Type Scale

The Steady Rise type scale is defined **once** and shared across desktop and mobile. Mobile makes small size adjustments, but **does not introduce new styles**.

#### Base Type Scale (All Platforms)

| Token | Size | Weight | Usage | Quasar Class |
|------|------|--------|------|--------------|
| H1 | 28px | 600 | Page titles | `text-h4` |
| H2 | 22px | 600 | Section headers | `text-h5` |
| H3 | 18px | 600 | Card and group titles | `text-subtitle1` |
| Body | 14–15px | 400 | Default text | `text-body2` |
| Label | 12px | 500 | Inputs, filters, metadata | `text-caption` + `text-weight-medium` |
| Caption | 11px | 400 | Secondary or helper text | `text-caption` |

**Rules**
- Prefer Quasar typography utility classes over custom font-size CSS
- Use weight utilities (`text-weight-medium`, `text-weight-bold`) sparingly
- Do not skip hierarchy levels
- Do not introduce mobile-only heading styles

---

#### Mobile Adjustments

Mobile uses the same hierarchy with modest size reductions.

Rules:
- Reduce H1 and H2 by ~2px via responsive container sizing, not new text classes
- Keep body text at **14px minimum**
- Maintain the same Quasar typography classes

---

### 7.4 Numeric Typography

Financial applications live or die by numeric clarity.

Rules:
- Enable tabular numbers globally where possible
- Right-align numeric columns in tables
- Use consistent decimal precision within a column
- Avoid mixing font weights within numeric values

---

### 7.5 Line Height & Readability

Recommended line heights:
- Headings: 1.2–1.3
- Body text: 1.45–1.6
- Dense tables: 1.3–1.4

Rules:
- Dense does not mean cramped
- Increase line height before increasing font size

---

### 7.6 Spacing Philosophy
> Spacing creates structure before borders or backgrounds.

Whitespace is the primary separator in Steady Rise.

**Quasar guidance:**
- Use Quasar spacing utility classes (`q-pa-*`, `q-ma-*`, `q-gutter-*`) instead of custom margins and padding
- Spacing decisions should be visible in templates, not hidden in CSS files

---

> Spacing creates structure before borders or backgrounds.

Whitespace is the primary separator in Steady Rise.

---

### 7.7 Spacing Scale

Use a small, consistent spacing scale.

Recommended tokens:
- 4px — micro spacing → `q-pa-xs`, `q-ma-xs`
- 8px — tight spacing → `q-pa-sm`, `q-ma-sm`
- 12px — default internal spacing → `q-pa-md`, `q-ma-md`
- 16px — section spacing → `q-pa-lg`, `q-ma-lg`
- 24px — major section breaks → combine `q-mt-lg` + `q-mb-md`
- 32px — page-level separation → `q-mt-xl`, `q-mb-xl`

Rules:
- Avoid arbitrary spacing values in CSS
- Prefer semantic Quasar spacing classes
- Vertical rhythm is more important than horizontal symmetry

---


Use a small, consistent spacing scale.

Recommended tokens:
- 4px — micro spacing (icons, inline gaps)
- 8px — tight spacing (labels, compact UI)
- 12px — default internal spacing
- 16px — section spacing
- 24px — major section breaks
- 32px — page-level separation

Rules:
- Avoid arbitrary spacing values
- Stick to the defined scale
- Vertical rhythm matters more than horizontal symmetry

---

### 7.8 Density Levels

Steady Rise supports multiple density contexts.

#### Comfortable (default)
- Dashboards
- Budgets
- Forms

#### Compact (data-heavy)
- Transactions
- Registers
- Imports

Rules:
- Density is set per page, not per component
- Compact mode reduces padding, not font size
- Never mix density modes within the same table

---

### 7.9 Alignment Rules

- Text aligns left by default
- Numbers align right
- Actions align right
- Labels align above inputs, not inline

Consistency in alignment reduces cognitive load.

---

## 8. Buttons & Actions Rules

### 8.1 Action Philosophy
> Actions should be obvious, predictable, and minimal.

If a screen feels busy, reduce actions before changing layout.

Rules:
- One primary action per surface (page, card, modal, panel)
- Secondary actions are visually quieter
- Destructive actions are explicit and separated

---

### 8.2 Action Hierarchy

Steady Rise uses three action tiers:

1. **Primary** — the main next step (solid)
2. **Secondary** — alternative actions (outline or flat)
3. **Tertiary** — low-emphasis actions (text/flat)

Rules:
- Do not place multiple primary actions adjacent
- Avoid mixing more than two action tiers in the same small area

---

### 8.3 Quasar Button Standards

Preferred component: `QBtn`

#### Default Button Pattern
- Use Quasar props instead of custom CSS
- Prefer consistent sizes and density

Recommended defaults:
- Height: Quasar default (or consistent app-wide)
- Radius: inherit app style (avoid per-button rounding)

**Quasar guidance**
- Use `unelevated` for primary buttons (modern, calm)
- Use `outline` for secondary buttons
- Use `flat` for tertiary buttons
- Use `dense` only in tables, filter bars, and tight toolbars

---

### 8.4 Button Variants

#### Primary
Used for the single most important action.

Rules:
- One primary per surface
- Primary uses the brand/primary color
- Primary text should be a verb (e.g., Save, Add Transaction)

Quasar examples:
- `color="primary" unelevated`

---

#### Secondary
Used for alternative actions.

Rules:
- Secondary buttons are quieter than primary
- Use outline or flat depending on density

Quasar examples:
- `color="primary" outline`

---

#### Tertiary
Used for low-emphasis actions, links, and inline controls.

Rules:
- Prefer text/flat styling
- Do not use tertiary for key flows

Quasar examples:
- `flat` or `flat color="primary"`

---

#### Destructive
Used for irreversible actions.

Rules:
- Destructive actions should never be the default primary action
- Use clear labels (Delete budget, Remove member)
- Require confirmation for irreversible actions

Quasar examples:
- `color="negative" flat` (or `outline`)

---

### 8.5 Icons (Buttons and Inline)

### 8.5.1 Icon System Standard

**Default icon set:** Google Material Icons

Rules:
- Material Icons are the default and preferred icon set across Steady Rise
- Do not mix icon libraries within the same surface or flow
- Custom icons should be introduced only when a Material Icon does not exist and the concept is core to the product

**Quasar guidance:**
- Use Quasar’s built-in Material Icons integration
- Reference icons by name (e.g. `icon="edit"`, `icon="delete"`, `icon="refresh"`)
- Do not inline SVGs for common actions

---

### 8.5.2 Icon Usage Principles

Icons are **supporting cues**, not primary communication.

Rules:
- Icons must reinforce text, not replace it (except in dense tables)
- Never rely on icon meaning alone for destructive or critical actions
- Icons should feel familiar to users of Google, Android, and accounting software

---

### 8.5.3 Icon Buttons

Rules:
- Icon-only buttons must have tooltips on desktop
- Icon-only actions should be reserved for frequent actions (edit, delete, link, refresh)
- Do not use icon-only buttons for destructive actions without confirmation

Quasar guidance:
- Use `QBtn` with `icon` and `flat` or `dense flat`
- Use `round` sparingly (primarily for floating or primary creation actions)
- Always pair with `QTooltip` on desktop

---

### 8.5.4 Inline Icons

Rules:
- Inline icons may appear next to labels, statuses, or values
- Inline icons should be smaller and lower contrast than surrounding text
- Inline icons must not disrupt text alignment

Quasar guidance:
- Use `QIcon` with Material icon names
- Prefer muted colors (`text-grey`, `text-grey-7`) unless conveying state

---

Rules:
- Icon-only buttons must have tooltips on desktop
- Icon-only actions should be reserved for frequent actions (edit, delete, link, refresh)
- Do not use icon-only buttons for destructive actions without confirmation

Quasar guidance:
- Use `QBtn` with `icon` and `round` only when appropriate
- Prefer `dense flat` for toolbar icon actions
- Always pair with `QTooltip` on desktop

---

### 8.6 Button Placement

#### Page Header
Rules:
- Page title on the left
- Primary action on the right
- Secondary actions grouped near primary or in an overflow menu

Quasar guidance:
- Use `row items-center justify-between`
- Use `q-gutter-sm` between buttons

---

#### Modals & Sheets
Rules:
- Actions live in a footer
- Footer is sticky when forms scroll
- Cancel is left, primary is right
- Avoid more than 2–3 actions

Quasar guidance:
- Use `QCardActions align="right"` (or a custom sticky footer wrapper)

---

#### Tables & Ledgers
Rules:
- Use compact actions
- Prefer bulk actions in a toolbar rather than per-row button clusters
- Per-row actions should be icons with tooltips

Quasar guidance:
- Use `dense flat` icon buttons in rows
- Use selection toolbars for bulk actions

---

### 8.7 Labels and Microcopy

Rules:
- Use verbs for actions (Add, Save, Import, Match)
- Avoid vague labels (OK, Submit)
- Use specific destructive labels (Delete Transaction)

---

### 8.8 Action Anti-Patterns

- Multiple primaries on one surface
- Icon-only destructive actions with no tooltip or confirmation
- Using color alone to differentiate action importance
- Dense buttons in non-dense contexts

---

## 9. Forms & Modals Rules

### 9.1 Forms Philosophy
> Forms should feel fast, predictable, and low-friction.

Steady Rise forms often appear in high-frequency workflows (transactions, matching, reconciliation). The design should prioritize:
- Speed
- Error prevention
- Consistency
- Minimal visual weight

---

### 9.2 Form Layout Standards

Rules:
- Use a single-column layout by default
- Labels sit **above** inputs (not inline)
- Group related fields with spacing, not borders
- Keep helper text short and close to the input

**Quasar guidance:**
- Use `QForm` + `QInput`, `QSelect`, `QDate`, `QToggle`, `QCheckbox`
- Use Quasar spacing utilities (`q-gutter-md`, `q-mb-md`) for grouping

---

### 9.3 Input Density

Steady Rise supports two form densities:

- **Comfortable (default):** settings, onboarding, budgets
- **Compact:** transactions, match flows, reconciliation

Rules:
- Density is chosen per page/flow, not per field
- Compact reduces padding and vertical spacing, not font size

**Quasar guidance:**
- Use `dense` consistently within a compact form
- Avoid mixing dense and non-dense fields within the same form section

---

### 9.4 Validation and Error Messaging

Rules:
- Validate as early as possible without being noisy
- Error messages must be specific and actionable
- Avoid blocking the user with errors until submission unless the error is critical
- Do not rely on color alone for errors

**Quasar guidance:**
- Use `rules` on inputs for validation
- Use `error` and `error-message` for clarity
- Prefer inline errors over toast errors for form validation

---

### 9.5 Defaults and Autocomplete

Rules:
- Pre-fill what you can (date, entity, last-used category)
- Remember last-used choices where safe and helpful
- Use searchable selects for long lists (categories, merchants)

**Quasar guidance:**
- Use `use-input` on `QSelect` for search
- Prefer `emit-value` + `map-options` to keep state clean

---

### 9.6 Modal Philosophy
> Modals are for focused tasks. Sheets are for frequent tasks.

In Steady Rise, transaction entry and editing are frequent actions. Use patterns that reduce friction.

---

### 9.7 Modal vs Sheet Standards

#### Desktop
- Use centered dialogs for focused tasks
- Prefer a right-side inspector for dense editing (ledger workflows)
- Max width: ~420–560px for typical forms

#### Mobile
- Use bottom sheets or full-screen dialogs
- Avoid multi-column layouts
- Keep primary action reachable without scrolling when possible

**Quasar guidance:**
- Use `QDialog` for modals and sheets
- For bottom sheets, use `position="bottom"` (or equivalent pattern) and full-width cards
- Use `$q.screen` breakpoints to switch presentation

---

### 9.8 Modal Structure

Standard structure:
1. Title
2. Optional helper line
3. Form body
4. Sticky action footer

Rules:
- One primary action
- Cancel is always available
- Destructive actions are separated from primary actions

**Quasar guidance:**
- Use `QCard` inside `QDialog`
- Use `QCardSection` for content
- Use `QCardActions` for footer actions

---

### 9.9 Action Footers (Sticky)

Rules:
- Footers should remain visible when form content scrolls
- Primary action aligns right
- Cancel aligns left

**Quasar guidance:**
- Use a sticky footer wrapper with `position: sticky; bottom: 0;`
- Keep footer padding consistent using `q-pa-sm`/`q-pa-md`

---

### 9.10 Transaction-Specific Form Rules

Transaction forms are high frequency and must be optimized.

Rules:
- Keep required fields minimal: Date, Payee, Amount, Category (or split)
- Use progressive disclosure for advanced fields (notes, tags, goal funding, recurring)
- Support split entry without opening multiple nested dialogs
- Preserve keyboard flow and tab order

**Quasar guidance:**
- Use expandable sections (`QExpansionItem`) for advanced fields
- Keep split lines inline within the form (sub-table pattern)

---

### 9.11 Confirmation Dialogs

Rules:
- Confirmation is required for destructive and irreversible actions
- Dialog copy must state what will happen
- Provide a safe default (Cancel)

**Quasar guidance:**
- Use `Dialog.create()` or `QDialog` confirmation patterns
- Destructive confirm button uses `color="negative"`

---

### 9.12 Form Anti-Patterns

- Multi-column forms on mobile
- Multiple primary actions in one modal
- Nested modals for split entry or advanced options
- Dense + non-dense inputs mixed in the same section
- Validation errors shown only via toast notifications

---

## 10. Ledger / Register Appendix (Accountant Mode)

This appendix defines interaction and layout rules for **ledger-style views** (Transactions, Account Register, Match Bank Transactions). These screens must feel familiar to accountants and bookkeepers.

---

### 10.1 Ledger Philosophy
> Ledgers prioritize certainty, density, and control over friendliness.

Ledger views are where users:
- Audit data
- Reconcile accounts
- Resolve discrepancies
- Perform bulk actions

Rules:
- Favor clarity over visual delight
- Reduce visual chrome
- Expose power features without hiding them

---

### 10.2 Ledger Layout Standards

Rules:
- Ledger tables are the primary surface
- Filters define scope and remain visible
- Summary information is secondary and compact

**Quasar guidance:**
- Use `QTable` (or virtualized table) as the main surface
- Avoid wrapping ledger tables in decorative cards

---

### 10.3 Density and Typography

Rules:
- Ledger pages use **Compact** density
- Reduce padding, not font size
- Use tabular numbers for all numeric columns
- Maintain consistent column alignment

**Quasar guidance:**
- Use `dense` on tables and controls
- Use typography utilities (`text-body2`, `text-caption`) consistently

---

### 10.4 Columns and Alignment

Rules:
- Dates align left
- Text aligns left
- Amounts align right
- Status is icon + tooltip
- Notes truncate with hover or expand-on-focus

**Quasar guidance:**
- Use scoped slots for column formatting
- Avoid inline CSS for alignment; use utility classes

---

### 10.5 Row Interaction

Rules:
- Row hover highlights subtly
- Row selection is explicit via checkbox
- Clicking a row selects it; double-click opens details
- Do not overload single-click with destructive actions

**Quasar guidance:**
- Use `selection="multiple"` where appropriate
- Use selection state to drive bulk actions

---

### 10.6 Bulk Actions

Rules:
- Bulk actions appear only when rows are selected
- Bulk actions live in a contextual toolbar
- Clearly state how many rows are affected

**Quasar guidance:**
- Use `QToolbar` or table `top` slot for bulk actions
- Avoid per-row destructive buttons when bulk actions exist

---

### 10.7 Status Indicators

Rules:
- Status must be explicit and understandable without color
- Use icons plus tooltips
- Color reinforces status but does not replace it

Common statuses:
- Cleared
- Uncleared
- Reconciled
- Duplicate
- Matched

---

### 10.8 Inspector Pattern

Rules:
- Editing details should not remove the user from the ledger context
- Prefer a right-side inspector over modals for desktop
- Inspector content updates based on selected row(s)

**Desktop:**
- Inspector appears as a right-side panel

**Mobile:**
- Inspector becomes a full-screen or bottom-sheet dialog

**Quasar guidance:**
- Use conditional `QDrawer` or layout slots for inspectors
- Share inspector component logic across desktop and mobile

---

### 10.9 Keyboard and Power User Considerations

Rules:
- Design with keyboard navigation in mind
- Preserve predictable tab order
- Avoid interactions that require precise pointer movement

Future considerations:
- Arrow-key navigation
- Keyboard shortcuts for common actions

---

### 10.10 Ledger Anti-Patterns

- Decorative cards around tables
- Excessive color or badges
- Per-row button clutter
- Hidden bulk actions
- Forcing modals for every edit

---

## 11. Transactions Refactor Checklist

This checklist translates the Steady Rise UI Rules into **concrete refactor and review criteria** for Transactions, Registers, and Ledger-style pages.

It should be used:
- During Transactions refactors
- During PR reviews
- When adding new ledger features

The goal is to preserve **accountant-grade familiarity, density, and trust**.

---

### 11.1 Page Intent & Mode

- Page is explicitly treated as **Auditing Mode**
- No dashboard-style cards used for primary data
- Visual density is **Compact**
- Language is precise, not motivational

---

### 11.2 Page Header & Context

- One clear page title (`text-h4`)
- Context (entity, date range, register type) is visible but secondary
- No more than one primary action
- No more than one row of actions

---

### 11.3 Summary Metrics (If Present)

- Metrics are inline text, not cards
- Metrics are visually secondary to the ledger
- No colored backgrounds
- Maximum of 4 summary values

---

### 11.4 Filter Bar

- Filters are always visible on desktop
- Filter bar is sticky during scroll
- Date range is always present
- Applied filters appear as removable chips
- “Clear all” is always visible

---

### 11.5 Ledger Table Surface

- Ledger uses `QTable` (or virtualized equivalent)
- Table is not wrapped in decorative cards
- Compact density is used consistently
- No zebra striping (or extremely subtle only)

---

### 11.6 Columns & Alignment

- Date columns left-aligned
- Text columns left-aligned
- Amount columns right-aligned
- Status uses icon + tooltip
- Notes truncate with hover or expand behavior

---

### 11.7 Row Interaction

- Hover state is subtle
- Selection requires explicit checkbox
- Single-click selects row
- Double-click opens details
- No destructive action on single click

---

### 11.8 Bulk Actions

- Bulk actions appear only when rows are selected
- Bulk toolbar clearly states selection count
- Bulk actions are grouped and labeled
- Destructive bulk actions require confirmation

---

### 11.9 Status Indicators

- Status is understandable without color
- Icon + tooltip explain meaning
- Color reinforces but does not replace meaning
- Status labels use accounting terminology

---

### 11.10 Inspector / Detail Editing

- Editing does not remove user from ledger context
- Desktop uses right-side inspector
- Inspector updates with row selection
- Mobile uses full-screen or bottom-sheet dialog
- Inspector logic is shared across platforms

---

### 11.11 Forms Within Ledger

- Compact density used consistently
- Required fields are minimal
- Advanced fields use progressive disclosure
- Split entry handled inline
- Keyboard tab order is logical

---

### 11.12 Buttons & Actions

- One primary action per surface
- Destructive actions are explicit and separated
- Icon-only buttons have tooltips (desktop)
- All icons use Material Icons

---

### 11.13 Color & Visual Noise

- No decorative color
- No colored card backgrounds
- Semantic colors used sparingly and correctly
- Emphasis achieved via spacing and typography

---

### 11.14 Accessibility & Clarity (Baseline)

- Text contrast is sufficient
- Icons have text equivalents or tooltips
- Focus states are visible
- Core actions are keyboard reachable

---

### 11.15 Final Sanity Checks

- Page remains usable with 1,000+ rows
- Accountant can understand the page without explanation
- No interaction requires guessing
- No UI element violates Steady Rise UI Rules

---

## 12. Page-Specific Intent

### Dashboard
- Card-heavy
- Visual summaries
- Low density
- Coaching tone

### Budget
- Mixed cards and tables
- Progress-focused
- Moderate density
- Coaching + control

### Transactions
- Table-first
- Filter-driven
- High density
- Auditing tone

---

## 8. Status of This Document

This is **v0.1** of the Steady Rise UI Rules.

Future additions may include:
- Typography scale and rules
- Button and action hierarchy
- Form and modal standards
- Accessibility guidelines
- Pro Mode vs Family Mode distinctions

This document should evolve, but changes should be deliberate and rare.

