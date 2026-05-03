-- ============================================================================
-- Migration: Add `funded_by_goal_id` to transactions.
--
-- Background. The "fund this expense from a goal" feature originally rewrote
-- the transaction into a transfer at save time. The transfer mechanic credits
-- the destination category's available, so a $150 goal-funded Housekeeping
-- expense showed as +$150 transfer-in (not as $150 spent), and the category
-- target progress was wrong for the user's mental model.
--
-- The fix is to keep the transaction as a regular expense (counts toward the
-- category's spent/available math) and persist the goal id alongside it. The
-- goal then derives "Goal Spend" from any expense whose `funded_by_goal_id`
-- matches it. Per-transaction explicit goal selection and category-level
-- defaults both ride this column.
--
-- ON DELETE SET NULL is defensive — goals are archived rather than hard-
-- deleted today, so this branch is unreachable from current code; the FK
-- just keeps the relation safe if a future hard-delete is ever added.
--
-- Existing transfer-shaped goal-funded transactions stay intact. The goal
-- spend reader still recognizes them; only new writes use this column.
--
-- Safe to re-run (IF NOT EXISTS guards on column + constraint).
-- ============================================================================

BEGIN;

ALTER TABLE transactions
  ADD COLUMN IF NOT EXISTS funded_by_goal_id UUID NULL;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'transactions_funded_by_goal_fk'
  ) THEN
    ALTER TABLE transactions
      ADD CONSTRAINT transactions_funded_by_goal_fk
      FOREIGN KEY (funded_by_goal_id) REFERENCES goals(id) ON DELETE SET NULL;
  END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_transactions_funded_by_goal_id
  ON transactions(funded_by_goal_id)
  WHERE funded_by_goal_id IS NOT NULL;

COMMIT;
