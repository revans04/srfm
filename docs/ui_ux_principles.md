# UI/UX Design Principles

A reusable set of principles for building modern web applications. These are brand-agnostic — bring your own colors, typography accents, and product voice. The rules below govern *structure, hierarchy, and feel*, which is what makes an app look cohesive regardless of palette.

Treat these as defaults. Only deviate when there is a concrete reason.

---

## 1. Design tokens first

Define a small set of CSS custom properties at `:root` and reference them everywhere. Never hardcode colors, radii, shadows, or spacing in component styles.

- **Surfaces:** `--color-surface-page`, `--color-surface-card`, `--color-surface-subtle`, `--color-surface-muted`
- **Text:** `--color-text-primary`, `--color-text-muted`, `--color-text-subtle`
- **Lines:** `--color-divider`, `--color-outline-soft` (low-alpha neutral)
- **Radii:** `--radius-sm: 8px`, `--radius-md: 12px`, `--radius-lg: 18px`
- **Shadows:** `--shadow-subtle` (small ambient), `--shadow-soft` (elevated). Shadows are large, diffuse, and low-opacity — never harsh.

This makes theming, dark mode, and visual refactors trivial.

## 2. Color palette

Use a restrained palette built from three layers. The specific hues are project-defined — these rules govern *how* colors are structured, not which ones to pick.

### Layer 1: Brand
- **One** primary brand hue. Pick a single saturated color and commit to it.
- Optionally a secondary (lighter tint of primary) and an accent (a nearby but distinct hue) for variety in charts, highlights, and illustrations.
- The brand color is used sparingly: primary buttons, active states, key links, focus rings, and small accents. It should never dominate a screen.

### Layer 2: Neutrals (the workhorse)
Most of the UI is neutrals, not brand color. Define a neutral ramp with at least 5 steps from darkest to lightest:

- **Text primary** — near-black but not pure black (slightly cool or warm to match brand temperature)
- **Text muted** — mid-tone for supporting content
- **Text subtle** — light tone for placeholders, scaffolding, disabled states
- **Divider / outline** — very light, often with alpha
- **Page background** — an almost-white tint (never pure `#FFFFFF`); cards sit on top in pure white, creating subtle elevation without shadows doing all the work

Pick a neutral *family* that harmonizes with the brand hue (cool slate for blue brands, warm stone for orange/red brands, true gray for neutral brands). Avoid mixing warm and cool neutrals in the same app.

### Layer 3: Semantic
Reserved strictly for *meaning*, never decoration:
- **Positive** (success, gains, confirmations)
- **Negative** (errors, destructive actions, losses)
- **Warning** (caution, at-risk states)
- **Info** (neutral notifications, tips)

These should feel like they belong to the same family as the brand — adjust saturation/lightness so they don't clash. Don't use the brand color as a semantic color, and don't use semantic colors as brand accents.

### Rules that apply regardless of palette
- Page background is a subtle off-white (or off-black in dark mode), **never pure white/black**. Cards float above it in a purer tone.
- Text uses the neutral ramp, not the brand color. Links and primary CTAs are the only places brand color appears in typography.
- Every color lives in a CSS custom property. No hex codes in component styles.
- If you need "more colors," add neutrals or tints of existing colors — don't introduce new hues.
- Test every text/surface pairing at WCAG AA contrast minimums.

## 3. Typography

- **Font:** a clean modern sans-serif (Inter, Geist, or system stack). Line-height ~1.45 for body.
- **Scale:** h1 `2.5rem` / h2 `2rem` / h3 `1.5rem` / h4 `1.25rem`, all `font-weight: 600`.
- **Page title:** a distinct class (~`1.75rem`, `font-weight: 700`) for app headers.
- **Section title:** ~`1.1rem`, `font-weight: 600`.
- **Column/label headers:** small (`12px`), medium weight, muted color, used for table headers and form labels.
- Establish hierarchy through **size + weight + color**, never size alone.

## 4. Shape language

- **Cards / panels:** `border-radius: var(--radius-lg)` (~18px), no border, `--shadow-subtle`, card surface background, generous padding (~18–20px).
- **Inputs:** `border-radius: var(--radius-md)` (~12px).
- **Buttons, chips, badges, pills:** fully rounded (`border-radius: 999px`).
- **Buttons:** `font-weight: 600`, `text-transform: none`, slight letter-spacing (`0.01em`). No ALL CAPS.

## 5. Layout

- Use a **12-column responsive grid** with consistent gutters (~16–20px).
- Page content sits on the subtle page background; content groups live inside cards.
- **Section rhythm:** page title → inline controls (selectors, filters) → contextual banners/nudges → summary tiles → detail sections. Keep vertical spacing consistent between sections.
- On wide screens, split related content into 2-column card rows. Collapse gracefully on narrow screens.

## 6. Mobile-first, without apology

- Assume mobile users perform real work, not just viewing. Every interactive surface needs a ≥44px touch target.
- Increase base font to `16px` on narrow viewports (≤430px).
- Prefer stacking and full-width controls over cramped horizontal layouts.
- Dialogs become full-screen sheets on mobile.

## 7. Information density & hierarchy

- **Primary info** in `--color-text-primary`, **supporting info** in `--color-text-muted`, **scaffolding** (placeholders, metadata) in `--color-text-subtle`.
- Don't rely on dividers to separate content — use whitespace first, then subtle backgrounds (`--color-surface-subtle`), then low-alpha outlines. Dividers are a last resort.
- Tables should feel like lists of cards: row hover states, comfortable row height, column headers in the small muted style.

## 8. Feedback & state

- **Banners/nudges** for contextual guidance: dense, colored by semantic meaning, with an icon, placed above the relevant section — not as modal interruptions.
- **Empty states** always include an illustration or icon, a one-line explanation, and a primary CTA.
- **Loading states** use skeletons that match the final layout, not spinners blocking the whole page.
- **Destructive actions** require confirmation and use the negative color only at the confirmation step, not on the trigger button.

## 9. Forms

- Outlined or filled fields with `--radius-md` corners.
- Labels above inputs, not placeholder-as-label.
- Group related fields into cards with a section title.
- Primary submit is a filled pill button; secondary is outlined or flat. Never more than one primary action per view.

## 10. Motion

- Transitions are fast (150–200ms) and use ease-out.
- Hover states are subtle: shift background opacity by ~10%, not scale transforms.
- Dialogs fade + slide up; sheets slide from the bottom on mobile.

## 11. Consistency rules

- Every new component must reuse existing tokens and utility classes before introducing new ones.
- If you find yourself hardcoding a value, stop and add a token.
- If two components solve the same visual problem differently, unify them.
- Maintain a small set of utility classes for common needs: `.shadow-subtle`, `.rounded-sm/md/lg`, `.surface-subtle`, `.text-muted`, `.page-title`, `.section-title`, `.col-header`.

## 12. What to avoid

- Hard shadows, thick borders, or sharp corners anywhere.
- Pure black text or pure white page backgrounds.
- Decorative use of semantic colors (positive/negative/warning are for *meaning* only).
- ALL-CAPS buttons or labels.
- Stacked modals, nested dialogs, or toast spam.
- Dense tables without padding or hover affordance.

---

**Deliverable expectation:** When implementing a new screen, produce a single cohesive view where spacing, radii, colors, and typography all pull from the token set above. The result should feel calm, legible, and unmistakably part of the same app as every other screen.
