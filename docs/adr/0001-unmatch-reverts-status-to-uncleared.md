# Unmatch reverts status to Uncleared; reconciled-row protection lives in the UI

## Status

accepted

## Context

`Status` (Uncleared `U` → Cleared `C` → Reconciled `R`) and `matched` are
independent concepts on a transaction (see `CONTEXT.md`). Matching a bank
(imported) transaction to a budget transaction sets both sides to `C`. Unmatch,
however, only flipped `matched` back to `false` and deliberately preserved
`status` — so an unmatched row stayed `Cleared` forever and the budget side kept
its bank-linkage fields. Users reported that "marking a transaction uncleared
doesn't stick." The fix lives entirely in
`BudgetService.BatchReconcileTransactions`.

## Decision

On unmatch, revert **both** the imported row and the linked budget transaction to
`Uncleared` (`U`) and clear the budget transaction's bank-linkage fields
(`account_number`, `account_source`, `transaction_date`, `posted_date`,
`imported_merchant`, `check_number`). The status revert is **unconditional** — it
does **not** guard the `Reconciled` (`R`) state in SQL:

```sql
status = CASE WHEN d.match THEN 'C' ELSE 'U' END
```

## Why (the trade-off)

We chose the simpler unconditional revert over a guarded
`CASE ... WHEN i.status = 'R' THEN 'R' ELSE 'U' END`. The protection that keeps a
finalized statement's reconciliation from being silently broken is enforced in
the UI, not the data layer: `LedgerTable` only renders the Unmatch control when
`row.matched && row.status !== 'R'`, so a plain Unmatch can never reach an `R`
row. Pushing the same guard into SQL would be redundant with that invariant and
add branching to a hot reconciliation path.

The same unconditional-revert rule applies in both unmatch code paths:
`BudgetService.UnmatchImported` (the canonical, account-agnostic path keyed on
the imported transaction id, used by the register Unmatch) and the unmatch
branch of `BatchReconcileTransactions`. The explicit
`transactions.imported_transaction_id` link (added 2026-06-12) is what lets
`UnmatchImported` locate the budget row without the old account + payee + date
heuristic; legacy rows whose link is NULL fall back to that heuristic.

## Consequence (the thing a future reader will wonder about)

The data layer trusts a UI invariant. If a future caller invokes
`UnmatchImported` / `BatchReconcileTransactions` with `match=false` on a row that
is `R` (e.g. a new API client, a script, or a UI change that exposes Unmatch on
reconciled rows), that row **will** be downgraded `R → U` with no warning.
Anyone adding such a caller must re-introduce the `R` guard, or enforce the
"don't unmatch reconciled rows" rule at that new boundary.
