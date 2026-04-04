# Steady Rise — UI Mockup Generation Prompt

Use this prompt with an image-generating LLM (Claude, GPT, Midjourney, etc.) to produce realistic UI mockups of the redesigned Steady Rise Financial Management app. Generate separate images for each screen described below: one desktop version and one mobile version per screen.

---

## Global Design Language

**App name:** Steady Rise Financial Management
**Logo:** Small circular mascot icon (green frog/lizard character) paired with the text "Steady Rise" in the sidebar header. On mobile, the logo + "Steady Rise Financial Management" appears in a solid blue top header bar.

**Visual identity:**
- Font: Inter (sans-serif), weight 600 for headings, 400/500 for body
- Primary blue: #1D4ED8 (deep blue — used for active nav items, primary buttons, links)
- Secondary blue: #60A5FA (lighter blue — used for secondary accents, selected chips)
- Accent: #0EA5E9 (sky blue)
- Positive/success: #16A34A (green)
- Negative/destructive: #DC2626 (red)
- Warning: #F59E0B (amber)
- Dark text: #0F172A
- Muted text: #64748B
- Page background: #F5F7FB (very light blue-gray)
- Card background: #FFFFFF with border `rgba(148, 163, 184, 0.24)` and border-radius 18px
- Subtle surface: #EEF2FF (light indigo tint, used for hover states and selected items)
- Shadows: soft and subtle — `0 6px 16px rgba(15, 23, 42, 0.06)`
- Buttons: fully rounded (pill shape, border-radius: 999px), font-weight 600, no text-transform
- Input fields: rounded corners (12px radius), outlined or filled variants
- Chips/badges: fully rounded (pill shape)

**Design mood:** Clean, professional, trustworthy. Similar to modern fintech apps like Copilot Money, Monarch Money, or YNAB. Not flashy — utilitarian but polished. Cards with subtle borders, plenty of whitespace, clear data hierarchy. The design should feel equally credible to a parent managing a family budget and an accountant reconciling bank statements.

---

## Desktop Layout Structure

**Viewport:** 1440 x 900px

**Shell:**
- Left sidebar: ~200px wide, white background, full height
  - Top: Steady Rise logo (small circular mascot) + "Steady Rise" text
  - Navigation list with Material icon + label for each: Dashboard, Budget (active/highlighted in primary blue), Transactions, Accounts, Reports, Data Mgmt, Settings
  - Bottom of sidebar: user avatar (small circle) + email address, version number in muted text
- Main content area: fills remaining width, light gray-blue background (#F5F7FB)
  - Page title as h1 at top left
  - Content below in white cards with 18px border-radius

---

## Mobile Layout Structure

**Viewport:** 375 x 812px (iPhone-sized)

**Shell:**
- Top header bar: solid primary blue (#1D4ED8), white text, contains:
  - Left: small hamburger menu icon (opens sidebar as overlay drawer when tapped — but drawer is NOT open by default)
  - Center: Steady Rise logo + "Steady Rise Financial Management" in white
- No sidebar visible by default — it's a hidden drawer
- Bottom tab bar: fixed at bottom, white background with top border, 5 items:
  - Dashboard (grid icon)
  - Budget (piggy bank icon) — highlighted when active
  - Transactions (list icon)
  - Accounts (bank icon)
  - More (three-dot icon) — tapping opens a bottom sheet with: Reports, Data Mgmt, Settings, Logout
- Main content area: light gray-blue background, content in white cards that span full width minus small margins

---

## Screen 1: Budget Page (DEFAULT LANDING PAGE)

This is the most important screen — it's where users land after login.

### Desktop Version

**Header area:**
- h1 page title "Budget" at top left
- "Evans Family" dropdown (primary blue text) with chevron
- "March 2026" shown in blue text with dropdown chevron
- Top right: "Edit" button (text with pencil icon) and "Delete" button (text with trash icon, red)
- Search bar below: "Search categories or groups" with rounded input field

**Summary row** (3 cards in a horizontal row):
- "Left to Budget" card: large green dollar amount "$16,281.58"
- "Income Received" card: "$0.00" with smaller text "Planned $15,561.58"
- "Savings Goals" card: "$0.00" with "Monthly total" subtitle

**Income section** (white card):
- Table header: "Income for Evans Family" with columns "Planned" and "Received"
- Rows: income sources (Signing Checks $525.00, The Flippen Group $15,036.58, etc.)
- Totals row at bottom

**Yellow info banner:**
- Amber/yellow background: "We detected legacy Savings categories. Convert them to the new Savings Goals feature."
- List of category names, dismiss X button

**Budget categories section** (white card):
- Section headers in blue text: "Favorites," "Food," "Housing," "Lifestyle," etc.
- Each category row: star icon (if favorited), category name, progress bar (yellow/gold fill), Planned amount, Remaining amount
- Progress bars show spending progress — yellow when under budget

**Right sidebar panel** (about 30% width):
- "Transactions" header with "March 2026" subtitle, "21 items" count
- Three tab pills: "Unmatched (21)" (active, filled blue), "Matched (45)" (outline), "Deleted (0)" (outline)
- Search field: "Search transactions"
- Transaction list: each item shows date badge (circle with "MAR 15"), merchant name, negative dollar amount in red, and a small muted/outlined trash icon (NOT solid red — styled to feel like a soft action, with tooltip "Mark as deleted — can be restored from Deleted tab")
- Floating blue "+" button at bottom right corner for adding transactions

### Mobile Version

- Blue header bar at top with "Steady Rise Financial Management"
- Page title "Budget" as h1 with "Evans Family" dropdown and "March 2026" below
- Edit and Delete actions in top right
- Search bar full width
- Summary cards stacked vertically (Left to Budget, Income Received, Savings Goals — each full width)
- Income table: full width card, showing all columns (Planned and Received both visible)
- Yellow info banner: full width
- Budget categories: full width card, each category row with name, progress bar, planned, remaining
- Unmatched transactions panel: stacks BELOW the budget categories (not beside them), full width card with the same tabs and transaction list
- Bottom tab bar: Dashboard, Budget (active/highlighted), Transactions, Accounts, More
- Floating blue "+" FAB for adding transactions, positioned above the bottom tab bar

---

## Screen 2: Transactions Page

### Desktop Version

**Header:**
- h1 "Transactions" (no subtitle — subtitle has been removed)
- Three tab buttons: "BUDGET REGISTER", "ACCOUNT REGISTER", "MATCH BANK TRANSACTIONS"

**Summary row** (4 colored stat cards):
- "TOTAL RECORDS: 329" (light blue background)
- "CLEARED: 263" (light blue)
- "RECONCILED: 0" (light blue)
- "UNCLEARED: 66" (slightly different blue tint)

**Filter area:**
- "Evans Family" dropdown in blue text
- Search input field with refresh and filter icons
- Month chips: "February 2026 x" "January 2026 x" (removable blue chips) with dropdown to add more
- Filter row: Merchant, Min $, Max $, Start date, End date inputs
- Filter pills: "Cleared" "Uncleared" "Reconciled" "Duplicates" (outline blue pill buttons)

**Data table:**
- Column headers: checkbox, Date, Payee, Category, Entity/Budget, Amount, Status, Notes
- Each row: checkbox, date, merchant name, category name, entity name, dollar amount, status icon (green "C" circle for cleared, orange "U" for uncleared), chain link icon, notes text
- Rows are clickable (entire row opens edit modal)
- Table has accounting terminology with small info-circle icons next to column headers that show tooltips on hover:
  - Next to "Status" header: tooltip icon
  - The status letters "C" and "U" should have tooltips: "C = Cleared: Transaction has posted to your account" and "U = Uncleared: Transaction hasn't posted yet"

### Mobile Version

- Blue header bar
- h1 "Transactions"
- Tab buttons horizontally scrollable if needed
- Summary stat cards: 2x2 grid (Total Records + Cleared on top row, Reconciled + Uncleared on bottom)
- "Evans Family" dropdown, search bar full width
- Month chips and filter inputs stacked vertically
- Filter pills in a horizontally scrollable row
- **CARD-BASED transaction list instead of table:** each transaction is a card showing:
  - Left: date in muted text
  - Center: merchant name (bold), category below in muted text
  - Right: amount (red for negative, dark for positive), small status badge
  - Tapping a card opens the edit modal
  - Cards have adequate spacing between them (no mis-tap risk)
- Bottom tab bar: Dashboard, Budget, Transactions (active), Accounts, More

---

## Screen 3: Accounts Page

### Desktop Version

**Header:**
- h1 "Accounts"
- Tab row: "BANK ACCOUNTS", "CREDIT CARDS", "INVESTMENTS", "PROPERTIES", "LOANS", "SNAPSHOTS"

**Content card:**
- Sub-header: "Bank Accounts" with total "$184,891.11" and "+ Add Account" button (blue text with plus icon)
- Table: Name, Institution, Balance, Account Number (masked as *1234), Owner, Actions
- Actions column: small edit icon button (flat, muted) and small delete icon button (flat, muted) — consistently sized, NOT oversized colored circles
- Rows for each account

### Mobile Version

- Blue header bar
- h1 "Accounts"
- Tab row: horizontally scrollable, showing all tabs without truncation
- Sub-header with total and "+ Add Account"
- **CARD-BASED account list instead of table:** each account is a card showing:
  - Account name (bold) and institution name below
  - Balance prominently displayed on the right
  - Tapping the card expands to reveal: account number (masked), owner, edit and delete action buttons with 44x44px tap targets
- Bottom tab bar: Dashboard, Budget, Transactions, Accounts (active), More

---

## Screen 4: Dashboard Page

Note: This is NO LONGER the landing page, but remains accessible from navigation.

### Desktop Version

**Header:**
- h1 "Dashboard" at top left
- "Evans Family" dropdown and "March 2026" dropdown at top right

**Top row** (4 summary cards in a row):
- "Budget Progress": shows Planned Expenses ($16,281.58), Actual Income ($0.00), progress bar at 0%, "Over budget" text
- "Net Worth": large number "$2,070,366.38"
- "Upcoming Bills": placeholder text "Connect or import statements to see upcoming bills" with a blue "Import Data" link/button (actionable empty state)
- "Goals": "No active goals" with a prominent "Set your first savings goal" link with arrow icon (guided empty state — not just a tiny + icon)

**Bottom row** (2 cards side by side):
- Left: "Spending by Category" donut chart with legend listing categories and amounts
- Right: "Income vs. Expenses" line chart (if data exists — if empty, show message: "No income data recorded for this period. Add income to your budget to see trends here." instead of a flat $0 line)

### Mobile Version

- Blue header bar
- h1 "Dashboard" with entity/month dropdowns below
- Summary cards stacked vertically, full width
- Donut chart full width in its own card
- Income vs. Expenses chart full width below
- Category legend below the donut chart as a list
- Bottom tab bar: Dashboard (active), Budget, Transactions, Accounts, More

---

## Screen 5: Transaction Edit Modal

### Desktop Version

- Modal overlay with white card, centered, ~500px wide
- Title: "Edit [Merchant Name]" with X close button
- Form fields (each with visible floating labels, not placeholder-only):
  - Date (date input with calendar icon)
  - Merchant (text input with clear X and dropdown arrow)
  - Amount (number input)
  - "CATEGORIES" section with "+ Add Split" button
    - Category dropdown + Split Amount input, red X to remove
  - Type toggle: "Income" | "Expense" pill buttons (Expense filled blue when active)
  - Notes textarea
- Bottom actions: "Cancel" (flat secondary button), "Delete" (outlined red/negative button — NOT a plain text link), "Save Transaction" (filled primary blue button)
- The Delete button should look like a proper destructive action — outlined with red border, not easily mistaken for a text link

### Mobile Version

- Full-screen modal or bottom sheet that slides up
- Same form fields but stacked vertically, full width
- Date, Merchant, Amount fields use native mobile input types
- Category section with adequate spacing
- Action buttons at bottom: Cancel (flat), Delete (outlined negative), Save (filled primary) — all with 44px+ height for easy tapping
- On-screen keyboard should not obscure the active input field

---

## Screen 6: "More" Menu (Mobile Only)

- When user taps "More" in the bottom tab bar
- A bottom sheet slides up with a list of additional navigation items:
  - Reports (trending up icon)
  - Data Mgmt (dataset icon)
  - Settings (manage accounts icon)
  - Divider line
  - Logout (logout icon, in muted/red text)
- Each item is a full-width row with icon + label, adequate tap target height (48px+)
- Tapping outside or swiping down dismisses the sheet

---

## Screen 7: Settings > Manage Family/Group

### Desktop Version

- h1 "Settings"
- Tab row: "MANAGE FAMILY/GROUP" (active), "MANAGE ENTITIES", "MANAGE IMPORTS", "MANAGE BUDGETS"
- Card: "Family/Group Information"
  - **Member list:** a `q-list` showing current family members:
    - Each row: avatar/initial circle, name or email, "Owner" badge (blue chip) or "Member" badge (muted chip), join date in muted text
    - Owner row is visually distinct (bold name)
    - Non-owner rows have a "Remove" icon button (visible to owner only)
  - Divider
  - "Invite Email" input field with "Invite" button below

### Mobile Version

- Blue header bar
- h1 "Settings"
- Tab row horizontally scrollable
- Member list full width in a card, same layout but stacked
- Invite field and button full width
- Bottom tab bar (via More menu since Settings isn't in the primary bar)

---

## Image Generation Instructions

For each screen, generate TWO images:
1. **Desktop** at roughly 1440x900 aspect ratio
2. **Mobile** at roughly 375x812 aspect ratio (iPhone proportions)

Style requirements:
- Render as a realistic high-fidelity UI mockup, not a wireframe
- Use the exact color palette specified above
- Show realistic financial data (dollar amounts, merchant names, category names)
- Cards should have subtle shadows and rounded corners (18px)
- Buttons should be pill-shaped
- Typography should look like Inter font
- The overall feel should be clean, professional, fintech-quality — think Monarch Money or YNAB, not a generic admin template
- Include the bottom tab bar on all mobile screens
- Do NOT show the sidebar open on mobile screens (it's hidden by default)
- Show tooltips rendered on at least one accounting term per screen where applicable (e.g., a tooltip bubble appearing over a "Cleared" status badge)
