-- ============================================================================
-- Migration: Add `funding_source_goal_id` to budget_categories.
--
-- Background. `budget_categories` already carries `funding_source_category` —
-- the name of another budget category that defaults as the source of expenses
-- recorded against this category (forming a transfer at save time). The same
-- shortcut should be available for savings goals, so a user can declare e.g.
-- "Vacation Spending is funded from the Vacation goal" once and skip the
-- per-transaction tagging.
--
-- The new column is a UUID FK to goals(id). ON DELETE SET NULL is defensive:
-- goals are archived (not hard-deleted) today, so this branch is unreachable
-- from current code. If a future hard-delete is added, the budget category
-- silently loses its funding hint rather than blocking deletion or breaking
-- inserts.
--
-- A CHECK constraint enforces mutual exclusion with `funding_source_category`
-- — only one source kind per category at a time. Enforcing in the DB (not just
-- the UI) protects against direct API calls and bulk imports that bypass the
-- form.
--
-- Entity-scoping (the goal must belong to the same entity as the budget) is
-- enforced at the application layer in BudgetService.SaveBudget — encoding it
-- in SQL would require a trigger and isn't worth the complexity.
--
-- Safe to re-run (IF NOT EXISTS guards on column + constraints).
-- ============================================================================

BEGIN;

ALTER TABLE budget_categories
  ADD COLUMN IF NOT EXISTS funding_source_goal_id UUID NULL;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'budget_categories_funding_source_goal_fk'
  ) THEN
    ALTER TABLE budget_categories
      ADD CONSTRAINT budget_categories_funding_source_goal_fk
      FOREIGN KEY (funding_source_goal_id) REFERENCES goals(id) ON DELETE SET NULL;
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'budget_categories_funding_source_exclusive'
  ) THEN
    ALTER TABLE budget_categories
      ADD CONSTRAINT budget_categories_funding_source_exclusive
      CHECK (funding_source_category IS NULL OR funding_source_goal_id IS NULL);
  END IF;
END $$;

COMMIT;
