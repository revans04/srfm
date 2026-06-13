# SRFM — Domain Glossary (CONTEXT.md)

> A glossary, not a spec. It defines the **language** of SteadyRise Family Money
> so humans and agents use the same words for the same concepts. Definitions are
> in domain terms (what a thing means to a household), with the distinction a
> newcomer most often gets wrong. No implementation details — for architecture,
> invariants, and contracts see `AGENTS.md` / `CLAUDE.md`.

---

## People & Tenancy

- **Family** — the household tenant and top-level collaboration boundary. Every
  piece of shared financial data (accounts, budgets, transactions, goals) is
  scoped to one family.
- **Family Owner** — the single member who created the family. Owns a few
  elevated operations (invite members, delete the family). Ownership is
  singular.
- **Family Member** — any user belonging to the family. *Gotcha:* there is no
  granular role model today — every member sees and edits **all** shared family
  data, owner or not. Shared data is scoped by family membership, never by who
  created the record.
- **Entity** — a partition *within* a family (e.g. the household itself, a
  business, a rental property). Budgets, goals, and category taxonomy are
  entity-scoped, so one family can keep separate books per entity. *Gotcha:*
  `entity_id` is a first-class key — mixing entity-scoped data across entities
  is a bug.
- **Membership** — the relation that grants a user access to a family's data.
  *Gotcha:* it is the authorization primitive; absence of membership denies
  access regardless of being signed in.
- **Invite** — an emailed invitation to join a family. *Gotcha:* it does **not**
  create an account; the invitee must already have a sign-in, and accepting with
  a different email than was invited fails.

---

## Budgeting

- **Budget** — one entity's spending plan for a single month, identified by a
  `YYYY-MM` month. Holds that month's categories and transactions. *Gotcha:*
  months sort lexicographically as `YYYY-MM` strings.
- **Budget Group** (category group) — an entity-level grouping of categories with
  a **kind**: `income`, `expense`, or `savings`. Groups persist across all
  months, so renaming or reordering one applies everywhere at once. *Gotcha:*
  "is this income?" is answered by `kind === 'income'`, **never** by the group's
  name.
- **Budget Category** — a single planned line within a group (e.g. "Groceries").
  Carries a target and may be a Fund.
- **Fund** — a category whose unspent balance rolls forward month to month
  (`IsFund`), as opposed to one that resets each month.
- **Carryover** — the balance a Fund carries into the next month. *Gotcha:*
  carryover can be **negative** — an overspent fund carries its deficit forward
  so the household sees it is still in the hole, rather than silently resetting
  to zero.
- **Income Target** — a budget's expected total income for the month. *Gotcha:*
  it is a target figure on the budget, not a category; actual income lands in
  categories whose group `kind` is `income`.

---

## Transactions

- **Budget Transaction** — a household money event entered in the app (income
  received, expense paid, or a transfer). The thing a household "records."
- **Category Split** — the allocation of a transaction's amount across one or
  more budget categories. *Gotcha:* **every** transaction has at least one split
  — even a single-category transaction is modeled as one split; there is no
  "split-less" transaction.
- **Income vs Expense** — money in vs money out. *Gotcha:* determined by the
  transaction's income flag together with its category group `kind`, not by
  reading category names.
- **Transfer** — money moved between two categories, recorded as **one** entry
  with signed splits: negative on the source, positive on the destination, net
  zero. *Gotcha:* a transfer is **not** an income/expense pair — recording it as
  two opposite transactions double-counts. Aggregations must branch on the
  transfer type, not just the income flag.
- **Goal-Funded Expense** — a normal expense recorded on its destination
  category (so it counts toward that category's spend) that is *tagged* with the
  goal it was paid from. *Gotcha:* this is **not** a transfer — the tag only
  drives the goal's spend rollup; do not convert it to a transfer.
- **Recurring Transaction** — a transaction the household expects to repeat
  (mortgage, subscriptions, utilities). It is pre-entered with an **estimated**
  amount and can be propagated as independent copies into existing future-month
  budgets. *Gotcha:* because the amount is an estimate, a recurring transaction
  rarely equals the bank's posted amount to the penny — that is the normal case,
  not an error (it is why recurring bills often don't auto-match; see *Amount
  Tolerance*).
- **Soft Delete / Ignored** — removing a transaction hides it but keeps it
  recoverable (a "Deleted" tab), rather than erasing it. On the bank side, an
  imported row can be **ignored** so it stops prompting to be matched. *Gotcha:*
  neither is a hard delete — both are filter states.

---

## Status & Matching

A transaction's **Status** is its position in the reconciliation lifecycle;
**Matched** is a separate fact about linkage. They are independent.

- **Uncleared (U)** — recorded but not confirmed against the bank.
- **Cleared (C)** — confirmed to have posted at the bank.
- **Reconciled (R)** — verified as part of a finalized bank statement.
- **Matched** — an Imported (bank) Transaction is linked to a Budget
  Transaction. *Gotcha:* matched ≠ reconciled — a row can be matched while still
  not part of a finalized statement. Matching links; reconciling finalizes.
- **Match / Unmatch** — linking a bank row to a budget transaction, or breaking
  that link. *Gotcha:* Unmatch returns both sides toward **Uncleared**; the UI
  prevents unmatching a Reconciled row (you must unreconcile first).
- **Smart Match** — an automatic 1:1 suggestion: a bank row that has **exactly
  one** qualifying budget candidate (close date, close amount, matching type).
  Zero or multiple candidates are not Smart Matches.
- **Remaining Transactions** — bank rows that did not Smart Match and need manual
  attention (no candidate, or an ambiguous conflict).
- **Amount Tolerance** — how close a budget amount must be to the bank amount to
  be treated as the same event. *Gotcha:* exact-to-the-penny is deliberately
  required for auto-suggestion — a recurring bill whose estimate drifted (e.g.
  $218.63 budgeted vs $218.39 posted) correctly will **not** auto-match;
  loosening the tolerance would silently pair genuinely different amounts.

> ⚠ **"Match" is overloaded in the UI.** The *Match Bank Transactions* surface
> links a bank row to an **existing** budget transaction. A separate bulk "Match"
> action **creates a new** budget transaction from a bank row. Different
> operations — keep them distinct in speech and design.

---

## Accounts & Reconciliation

- **Account** — a household/business financial account. Types: `Bank`,
  `CreditCard`, `Investment`, `Property`, `Loan`. Each is an **Asset** (you own
  it) or a **Liability** (you owe it). *Gotcha:* a Loan is a Liability, not an
  Asset; sign conventions follow that.
- **Imported Transaction** (Bank row) — a transaction pulled from a bank/card
  import, carrying the real posted amount and date. Distinct from the budget
  transaction it may be matched to. *Gotcha:* the household's entered amount and
  the bank's posted amount are not assumed equal.
- **Account Register** — the view of Imported (bank) Transactions for an account.
- **Budget Register** — the view of Budget Transactions for an entity/month.
- **Reconciliation** — confirming that recorded transactions agree with what the
  bank actually posted.
- **Statement** — a bank statement period with a beginning and ending balance.
  **Finalizing** a statement marks its transactions Reconciled (`R`);
  **unreconciling** reverts them. *Gotcha:* reconciled transactions are meant to
  be stable — reverse the statement rather than editing them in place.
- **Snapshot / Net Worth** — a point-in-time capture of account balances; net
  worth is total Assets minus Liabilities. A summary metric, not a
  transaction-level control.

---

## Goals

- **Goal** — a savings or spending target for an entity (e.g. "Vacation $5,000").
  Tracks **saved-to-date** (contributions) and **spent-to-date** (withdrawals /
  goal-funded expenses).
- **Goal Funding** — money reaches a goal either through contributions on the
  goal's own linked category, or through ordinary expenses tagged as funded by
  the goal. *Gotcha:* both feed the goal's rollups; a goal-funded expense stays a
  standard expense (see *Goal-Funded Expense*).
- **Goal-Linked Budget Category** — a category the system maintains for a goal's
  contributions/withdrawals. *Gotcha:* these are hidden from the regular
  Budget/Dashboard views so pure goal movements don't clutter the budget;
  reconciliation/match surfaces opt back in to keep those rows clearable.

---

## Reports

- **Payee** — the merchant/counterparty on a transaction.
- **Payee Canonicalization** — collapsing raw merchant text variants (case,
  whitespace, curly vs straight apostrophes) to one key so a report groups
  "McDonald's", "MCDONALD'S", and "McDonald's" together. *Gotcha:* grouping by
  raw merchant text fragments the report; any user-supplied merchant filter must
  be canonicalized the same way or it silently misses rows.

---

## Onboarding & Authentication

- **Onboarding Seed** — the single-page `/setup` form that creates a family +
  entity + first budget (+ optional accounts) in one transactional submit. Runs
  in **seed** mode for brand-new users and **add-entity** mode for existing users
  adding another entity — same form, different headline. *Gotcha:* it is not a
  multi-step wizard; don't reintroduce a stepper.
- **Getting Started Checklist** — a progress list (link a bank, set up a goal,
  verify email, reconcile, invite a partner) that ticks automatically as the
  underlying data appears. *Gotcha:* items are aspirational progress and stay
  ticked — they don't un-tick if the triggering data is later removed.
- **Email Verification** — confirms the user owns their email; surfaced via a
  banner until verified. *Gotcha:* it is driven entirely by the auth provider;
  verifying on another device may not reflect until the app reloads the user.
- **Firebase Bearer Token** — the identity claim every protected API call
  carries. *Gotcha:* it is the auth primitive; it can legitimately lack an email
  claim (anonymous/phone sign-in), so an email is not guaranteed to be present.
