-- ============================================================================
-- Migration: Backfill missing goal ↔ budget links.
--
-- Background. `GoalService.InsertGoal` walks every existing budget under the
-- goal's entity and writes a `budget_categories` row + `goals_budget_categories`
-- link, but only at goal-creation time. Two paths produce orphaned/missing
-- links over the app's lifecycle:
--
--   1. A budget for a later month is created AFTER the goal exists. Nothing
--      seeds the link in the new budget, so contributions to that goal in the
--      newer month never roll up into `savedToDate` (the goal panel shows
--      $0 contributed even though the transaction persisted).
--   2. `BudgetService.SaveBudget` previously did a bulk
--      `DELETE FROM budget_categories WHERE budget_id=@bid` and re-inserted
--      only what the FE sent. The FE's payload comes from `LoadBudgetDetails`,
--      which hides goal-linked categories — so every save destroyed the
--      goal's budget_category row in that budget and orphaned its
--      `goals_budget_categories` row.
--
-- Both code paths are now fixed in `BudgetService.WriteBudgetAndCategoriesAsync`
-- (DELETE preserves goal-linked rows, and `EnsureGoalCategoriesForBudgetAsync`
-- runs after every save). This migration repairs the legacy data so existing
-- contributions start showing up retroactively.
--
-- Steps:
--   0. Ensure every entity that has at least one active goal has a 'Savings'
--      budget_groups row. Required for step 1's foreign key.
--   1. Insert a budget_categories row in every (budget, goal) pair where the
--      budget's entity matches the goal's entity AND no row with that name
--      exists yet.
--   2. Insert a goals_budget_categories row for every (goal, matching
--      budget_category) pair where no link exists yet.
--
-- Idempotent. Re-runnable. Touches no transactions, only structural rows.
-- ============================================================================

BEGIN;

-- 0. Materialize a 'Savings' group per entity that owns goals.
INSERT INTO budget_groups (entity_id, name, kind, sort_order)
SELECT DISTINCT g.entity_id, 'Savings', 'savings', 1000
FROM goals g
WHERE COALESCE(g.archived, FALSE) = FALSE
  AND NOT EXISTS (
      SELECT 1 FROM budget_groups bg
      WHERE bg.entity_id = g.entity_id
        AND bg.name = 'Savings'
  );

-- 1. Create the missing budget_categories rows. is_fund=true matches what
--    InsertGoal/EnsureGoalCategoriesForBudgetAsync write. carryover stays 0
--    — the goal's true balance lives in the rollup of its transactions, not
--    in any single month's category carryover.
INSERT INTO budget_categories
    (budget_id, name, target, is_fund, group_id, sort_order, carryover)
SELECT
    b.id,
    g.name,
    g.monthly_target,
    TRUE,
    bg.id,
    0,
    0
FROM goals g
JOIN budgets b
  ON b.entity_id = g.entity_id
JOIN budget_groups bg
  ON bg.entity_id = g.entity_id
 AND bg.name = 'Savings'
LEFT JOIN budget_categories existing
  ON existing.budget_id = b.id
 AND existing.name = g.name
WHERE COALESCE(g.archived, FALSE) = FALSE
  AND existing.id IS NULL;

-- 2. Link every goal to its matching budget_category in each budget.
INSERT INTO goals_budget_categories (goal_id, budget_cat_id)
SELECT g.id, bc.id
FROM goals g
JOIN budgets b
  ON b.entity_id = g.entity_id
JOIN budget_categories bc
  ON bc.budget_id = b.id
 AND bc.name = g.name
WHERE COALESCE(g.archived, FALSE) = FALSE
  AND NOT EXISTS (
      SELECT 1 FROM goals_budget_categories gbc
      WHERE gbc.goal_id = g.id
        AND gbc.budget_cat_id = bc.id
  );

COMMIT;
