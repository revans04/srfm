-- ============================================================================
-- Migration: Add `imported_transaction_id` to transactions.
--
-- Background. A budget transaction and the imported (bank) transaction it is
-- matched to had NO direct link. Match copied the imported row's account/date/
-- payee fields onto the budget row, and unmatch had to *reconstruct* the link
-- heuristically (account_number + imported_merchant === payee + date). That
-- heuristic was fragile (edited payees, recurring duplicates) and the UI path
-- bailed entirely when no single account was selected ("All accounts"), so
-- unmatch silently did nothing. See CONTEXT.md (Status vs Matched) and
-- docs/adr/0001-unmatch-reverts-status-to-uncleared.md.
--
-- This adds an explicit link from the budget transaction to the imported row.
--
-- Soft reference, not a hard FK. `imported_transactions.id` is a composite TEXT
-- id ("{document_id}-{transactionId}"), and imported rows are hard-deleted when
-- their document is deleted. A cascading FK is therefore neither clean (text
-- composite key) nor desirable (we want the budget transaction to survive an
-- import-doc deletion, just unlinked). The column is an indexed soft link; the
-- application nulls it on unmatch.
--
-- Best-effort backfill. Existing matched budget transactions are linked back to
-- their imported row ONLY where the legacy heuristic resolves to exactly one
-- imported transaction. Ambiguous cases (e.g. recurring duplicates) are left
-- NULL and fall back to the legacy heuristic at unmatch time.
--
-- Safe to re-run (IF NOT EXISTS guards; backfill only fills NULLs).
-- ============================================================================

BEGIN;

ALTER TABLE transactions
  ADD COLUMN IF NOT EXISTS imported_transaction_id TEXT NULL;

CREATE INDEX IF NOT EXISTS idx_transactions_imported_transaction_id
  ON transactions(imported_transaction_id)
  WHERE imported_transaction_id IS NOT NULL;

-- Backfill unambiguous existing links.
UPDATE transactions t
SET imported_transaction_id = sub.iid
FROM (
  SELECT t2.id AS tid, MIN(i.id) AS iid
  FROM transactions t2
  JOIN imported_transactions i
    ON i.account_number = t2.account_number
   AND i.payee = t2.imported_merchant
   AND COALESCE(i.transaction_date, i.posted_date) = COALESCE(t2.transaction_date, t2.posted_date)
   AND i.matched = true
  WHERE t2.account_number IS NOT NULL
    AND t2.imported_merchant IS NOT NULL
    AND t2.status IN ('C', 'R')
    AND t2.imported_transaction_id IS NULL
  GROUP BY t2.id
  HAVING COUNT(*) = 1
) sub
WHERE t.id = sub.tid
  AND t.imported_transaction_id IS NULL;

COMMIT;
