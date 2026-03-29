# Bug Tracker

## Open

_None currently._

---

## Resolved

### BUG-001 — Google Auth avatar not displaying
**Status:** Resolved
**Area:** Authentication / Header UI
**Description:** After sign-in with Google, the user's avatar/profile photo from their Google account is not shown next to their email address. Only the email appears.
**Fix:** Updated `loginWithGoogle()` in auth store to set `avatarSrc` immediately from `firebaseUser.photoURL`. Updated MainLayout to read `photoURL` directly from the Firebase user with `referrerpolicy="no-referrer"` and fall back to user initials when no photo is available.

---

### BUG-002 — Target Ending Balance sign not intuitive for account type
**Status:** Resolved
**Area:** Account Reconciliation / Statement Reconcile Panel
**Description:** When reconciling a statement, the Target Ending Balance field expects a negative value for credit cards and a positive value for bank accounts, but there is no hint or guidance in the UI to communicate this.
**Fix:** Added a dynamic hint below the Target Ending Balance field that shows "Enter as negative for credit cards" or "Enter as negative for loans" based on the selected account type.

---

### BUG-003 — Batch Match only shows "Income" in category dropdown
**Status:** Resolved
**Area:** Transaction Matching / Batch Match Dialog
**Description:** When creating a budget transaction from one or more imported account transactions via the Batch Match dialog, the Category dropdown only shows "Income" — all other budget categories are missing.
**Fix:** Made category collection more robust in MatchBankPanel by also reading categories from the budget store after full budgets are loaded. Fixed sort order to alphabetical. Prevented duplicate "Income" entry.

---

### BUG-004 — Category picker is not searchable and has no sort order
**Status:** Resolved
**Area:** Transaction Entry / Category Selection
**Description:** When entering a budget transaction, categories are displayed in a plain dropdown with no search/autocomplete and no apparent sort order.
**Fix:** Added `use-input` and `@filter` to the category `q-select` in TransactionForm for type-to-search filtering. Sorted remaining categories alphabetically.

---

### BUG-005 — Add Transaction dialog does not pre-select the category context
**Status:** Resolved
**Area:** Transaction Entry / Budget Page
**Description:** When opening the Add Transaction dialog from within a specific category section on the Budget page, the Category field is blank.
**Fix:** Added an "Add transaction" button to CategoryTransactions panel that emits the `add-transaction` event (was defined but never wired up). Updated the Budget page FAB to use `addTransactionForCategory` when a category is selected.

---

### BUG-006 — Duplicate "transaction saved" toast on add
**Status:** Resolved
**Area:** Transaction Entry
**Description:** After saving a new budget transaction, two success toast notifications appear instead of one.
**Fix:** Removed the duplicate toast from BudgetPage's `onTransactionSaved` handler. TransactionForm already fires a success toast on save.

---

### BUG-007 — Merchant field is not an autocomplete
**Status:** Resolved
**Area:** Transaction Entry
**Description:** The Merchant field when adding a transaction is a plain text input with no autocomplete.
**Fix:** Replaced the native `<datalist>` with a Quasar `q-select` using `use-input` and `new-value-mode="add-unique"` for proper autocomplete with type-to-filter from the merchant store.

---
