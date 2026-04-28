-- ============================================================================
-- Migration: Add `opening_balance` to goals.
--
-- Background. Users coming from spreadsheets / other tools commonly already
-- have money saved toward a goal — e.g. "I have $5,000 in my house fund
-- already." There was no way to record this without writing a transaction
-- in the current month, which (a) made it look like the contribution
-- happened this month and (b) overcounted the source category as spent.
--
-- `opening_balance` is a starting balance baked into the goal itself. It's
-- added to the saved-to-date rollup (see `GoalService.GetGoalsForEntity`)
-- so the goal shows the right total without affecting any monthly budget.
-- It is intentionally NOT a transaction — there is no double-entry
-- counterpart, because the money came from outside this app's accounting
-- horizon.
--
-- Defaults to 0 so existing goals are unaffected. NOT NULL with a default
-- so callers don't have to differentiate NULL from 0.
--
-- Safe to re-run (IF NOT EXISTS guard).
-- ============================================================================

BEGIN;

ALTER TABLE goals
  ADD COLUMN IF NOT EXISTS opening_balance NUMERIC(14, 2) NOT NULL DEFAULT 0;

COMMIT;
