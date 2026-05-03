using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FamilyBudgetApi.Services
{
    public class GoalService
    {
        private readonly SupabaseDbService _db;
        private readonly ILogger<GoalService> _logger;

        public GoalService(SupabaseDbService db, ILogger<GoalService> logger)
        {
            _db = db;
            _logger = logger;
        }
        public async Task InsertGoal(Goal goal)
        {
            try
            {
                _logger.LogInformation("Inserting goal {GoalId}", goal.Id);
                await using var conn = await _db.GetOpenConnectionAsync();
                const string insertSql = @"INSERT INTO goals (id, entity_id, name, total_target, monthly_target, opening_balance, target_date, archived, created_at, updated_at)
VALUES (@id,@entity_id,@name,@total_target,@monthly_target,@opening_balance,@target_date,@archived, now(), now());";

                if (!Guid.TryParse(goal.Id, out var goalId))
                {
                    throw new ArgumentException($"Invalid goal ID: {goal.Id}");
                }

                if (!Guid.TryParse(goal.EntityId, out var entityId))
                {
                    throw new ArgumentException($"Invalid entity ID: {goal.EntityId}");
                }

                await using (var insertCmd = new NpgsqlCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("id", goalId);
                    insertCmd.Parameters.AddWithValue("entity_id", entityId);
                    insertCmd.Parameters.AddWithValue("name", (object?)goal.Name ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("total_target", (decimal)goal.TotalTarget);
                    insertCmd.Parameters.AddWithValue("monthly_target", (decimal)goal.MonthlyTarget);
                    insertCmd.Parameters.AddWithValue("opening_balance", (decimal)goal.OpeningBalance);
                    insertCmd.Parameters.AddWithValue("target_date", string.IsNullOrEmpty(goal.TargetDate) ? (object)DBNull.Value : DateTime.Parse(goal.TargetDate));
                    insertCmd.Parameters.AddWithValue("archived", goal.Archived);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                // Walk every existing budget under this entity and ensure a
                // budget_categories row + goals_budget_categories link exists
                // for the new goal. Shared with BudgetService.SaveBudget so a
                // budget created LATER (after the goal) also gets linked on
                // its first save — no special-case code path here for "future
                // budgets," it just falls out of the same helper running on
                // every save.
                var budgetIds = new List<string>();
                const string budgetSql = "SELECT id FROM budgets WHERE entity_id=@eid";
                await using (var budgetCmd = new NpgsqlCommand(budgetSql, conn))
                {
                    budgetCmd.Parameters.AddWithValue("eid", entityId);
                    await using var reader = await budgetCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        budgetIds.Add(reader.GetString(0));
                    }
                }

                foreach (var bid in budgetIds)
                {
                    await BudgetService.EnsureGoalCategoriesForBudgetAsync(conn, null, bid, entityId);
                }

                _logger.LogInformation("Goal {GoalId} inserted and categories updated", goal.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert goal {GoalId}", goal?.Id);
                throw;
            }
        }

        public async Task UpdateGoal(Goal goal)
        {
            try
            {
                _logger.LogInformation("Updating goal {GoalId}", goal.Id);
                await using var conn = await _db.GetOpenConnectionAsync();

                if (!Guid.TryParse(goal.Id, out var goalId))
                {
                    throw new ArgumentException($"Invalid goal ID: {goal.Id}");
                }

                if (!Guid.TryParse(goal.EntityId, out var entityId))
                {
                    throw new ArgumentException($"Invalid entity ID: {goal.EntityId}");
                }

                // Read the existing name first. If the user renames the goal we
                // must rename every linked budget_categories row too, otherwise
                // the goal rollup join (which is name-based) silently breaks
                // and savedToDate resets to 0 — exactly the same failure mode
                // we just fixed for missing links. Doing it inside one
                // database transaction so a partial rename can't strand the
                // data half-renamed.
                string? existingName = null;
                const string readNameSql = "SELECT name FROM goals WHERE id=@id";
                await using (var readCmd = new NpgsqlCommand(readNameSql, conn))
                {
                    readCmd.Parameters.AddWithValue("id", goalId);
                    var obj = await readCmd.ExecuteScalarAsync();
                    if (obj is string s) existingName = s;
                }

                await using var dbTx = await conn.BeginTransactionAsync();

                const string updateSql = @"UPDATE goals SET entity_id=@entity_id, name=@name, total_target=@total_target, monthly_target=@monthly_target, opening_balance=@opening_balance, target_date=@target_date, archived=@archived, updated_at=now() WHERE id=@id";
                await using (var cmd = new NpgsqlCommand(updateSql, conn, dbTx))
                {
                    cmd.Parameters.AddWithValue("id", goalId);
                    cmd.Parameters.AddWithValue("entity_id", entityId);
                    cmd.Parameters.AddWithValue("name", (object?)goal.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("total_target", (decimal)goal.TotalTarget);
                    cmd.Parameters.AddWithValue("monthly_target", (decimal)goal.MonthlyTarget);
                    cmd.Parameters.AddWithValue("opening_balance", (decimal)goal.OpeningBalance);
                    cmd.Parameters.AddWithValue("target_date", string.IsNullOrEmpty(goal.TargetDate) ? (object)DBNull.Value : DateTime.Parse(goal.TargetDate));
                    cmd.Parameters.AddWithValue("archived", goal.Archived);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (!string.IsNullOrEmpty(existingName)
                    && !string.IsNullOrEmpty(goal.Name)
                    && !string.Equals(existingName, goal.Name, StringComparison.Ordinal))
                {
                    // Rename every budget_categories row that the goal points
                    // to. Scoped to (id ∈ goals_budget_categories for this
                    // goal AND name = old goal name) so we don't accidentally
                    // overwrite an unrelated category that happens to share
                    // the bc id (defensive — the link table should be 1:1).
                    const string renameCatSql = @"UPDATE budget_categories
                        SET name = @new_name
                        WHERE id IN (
                            SELECT budget_cat_id FROM goals_budget_categories WHERE goal_id = @goal_id
                        )
                        AND name = @old_name";
                    await using var renameCmd = new NpgsqlCommand(renameCatSql, conn, dbTx);
                    renameCmd.Parameters.AddWithValue("goal_id", goalId);
                    renameCmd.Parameters.AddWithValue("old_name", existingName);
                    renameCmd.Parameters.AddWithValue("new_name", goal.Name);
                    await renameCmd.ExecuteNonQueryAsync();
                    _logger.LogInformation("Renamed budget_categories for goal {GoalId}: '{OldName}' → '{NewName}'", goal.Id, existingName, goal.Name);
                }

                await dbTx.CommitAsync();
                _logger.LogInformation("Goal {GoalId} updated", goal.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update goal {GoalId}", goal?.Id);
                throw;
            }
        }

        public async Task<List<Goal>> GetGoals(string entityId)
        {
            var goals = new List<Goal>();
            try
            {
                _logger.LogInformation("Fetching goals for entity {EntityId}", entityId);
                await using var conn = await _db.GetOpenConnectionAsync();

                // Classify each transaction_category row by transaction type:
                //  - Transfers: signed splits (positive=contribution, negative=withdrawal)
                //  - Standard income (is_income=true): contribution
                //  - Standard expense (is_income=false): withdrawal
                //
                // Two spend signals contribute to `spent`:
                //  (a) Existing path — splits on the goal's auto-synced budget
                //      category (joined via goals_budget_categories).
                //  (b) New path — standard expenses anywhere whose
                //      `funded_by_goal_id` matches this goal. The whole expense
                //      counts (the field is on the transaction, not the split),
                //      modeling "Housekeeping spent $150 funded from Bonus."
                // Aggregated separately in `funded_agg` so the cross-product
                // with the existing path doesn't multiply rows; folded in via
                // MAX (constant within each goal's group).
                //
                // `opening_balance` is added into `saved` so a goal pre-seeded
                // with prior savings reads correctly even when no transactions
                // exist yet. It carries through GROUP BY (per-row constant).
                const string sql = @"SELECT g.id, g.entity_id, g.name, g.total_target, g.monthly_target, g.target_date, g.archived,
                                            COALESCE(g.opening_balance,0) + COALESCE(SUM(CASE
                                                WHEN COALESCE(t.transaction_type,'standard') = 'transfer' AND tc.amount > 0 THEN tc.amount
                                                WHEN COALESCE(t.transaction_type,'standard') <> 'transfer' AND COALESCE(t.is_income,FALSE) = TRUE THEN tc.amount
                                                ELSE 0
                                            END),0) AS saved,
                                            COALESCE(SUM(CASE
                                                WHEN COALESCE(t.transaction_type,'standard') = 'transfer' AND tc.amount < 0 THEN -tc.amount
                                                WHEN COALESCE(t.transaction_type,'standard') <> 'transfer' AND COALESCE(t.is_income,FALSE) = FALSE THEN tc.amount
                                                ELSE 0
                                            END),0) + COALESCE(MAX(funded_agg.spent), 0) AS spent,
                                            COALESCE(g.opening_balance,0) AS opening_balance
                                     FROM goals g
                                     LEFT JOIN goals_budget_categories gbc ON gbc.goal_id = g.id
                                     LEFT JOIN budget_categories bc ON bc.id = gbc.budget_cat_id
                                     LEFT JOIN transactions t ON t.budget_id = bc.budget_id
                                       AND COALESCE(t.deleted, FALSE) = FALSE
                                     LEFT JOIN transaction_categories tc ON tc.transaction_id = t.id AND tc.category_name = bc.name
                                     LEFT JOIN (
                                         SELECT funded_by_goal_id AS goal_id, SUM(amount) AS spent
                                         FROM transactions
                                         WHERE COALESCE(deleted, FALSE) = FALSE
                                           AND funded_by_goal_id IS NOT NULL
                                           AND COALESCE(transaction_type, 'standard') = 'standard'
                                           AND COALESCE(is_income, FALSE) = FALSE
                                         GROUP BY funded_by_goal_id
                                     ) funded_agg ON funded_agg.goal_id = g.id
                                     WHERE g.entity_id=@eid
                                     GROUP BY g.id, g.entity_id, g.name, g.total_target, g.monthly_target, g.target_date, g.archived, g.opening_balance";
                await using var cmd = new NpgsqlCommand(sql, conn);
                if (!Guid.TryParse(entityId, out var eid))
                {
                    throw new ArgumentException($"Invalid entity ID: {entityId}");
                }
                cmd.Parameters.AddWithValue("eid", eid);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    goals.Add(new Goal
                    {
                        Id = reader.GetGuid(0).ToString(),
                        EntityId = reader.GetGuid(1).ToString(),
                        Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        TotalTarget = reader.IsDBNull(3) ? 0 : (double)reader.GetDecimal(3),
                        MonthlyTarget = reader.IsDBNull(4) ? 0 : (double)reader.GetDecimal(4),
                        TargetDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("o"),
                        Archived = reader.IsDBNull(6) ? false : reader.GetBoolean(6),
                        SavedToDate = reader.IsDBNull(7) ? 0 : (double)reader.GetDecimal(7),
                        SpentToDate = reader.IsDBNull(8) ? 0 : (double)reader.GetDecimal(8),
                        OpeningBalance = reader.IsDBNull(9) ? 0 : (double)reader.GetDecimal(9)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch goals for entity {EntityId}", entityId);
                throw;
            }
            return goals;
        }

        public async Task<GoalDetails> GetGoalDetails(string goalId)
        {
            var details = new GoalDetails();
            try
            {
                _logger.LogInformation("Fetching goal details for {GoalId}", goalId);
                await using var conn = await _db.GetOpenConnectionAsync();
                if (!Guid.TryParse(goalId, out var gid))
                {
                    throw new ArgumentException($"Invalid goal ID: {goalId}");
                }

                // Two row sources:
                //   (a) `linked` — transactions whose split lands on the goal's
                //       auto-synced category (joined via goals_budget_categories).
                //       Classified by transaction type / is_income.
                //   (b) `funded` — standard expenses whose `funded_by_goal_id`
                //       points at this goal. Always spend (the user funded an
                //       expense from the goal). Use t.amount because the field
                //       is on the transaction, not on a split.
                // UNION ALL is safe because the two paths don't overlap in
                // practice — the `linked` path joins on the goal's own category
                // name, while `funded` rows record the expense on a different
                // category (e.g., Housekeeping) and only carry the goal id as
                // metadata.
                const string sql = @"
                    SELECT t.id, t.date, t.merchant, tc.amount, t.budget_id,
                           COALESCE(t.is_income, FALSE) AS is_income,
                           COALESCE(t.transaction_type, 'standard') AS tx_type,
                           'linked' AS source
                    FROM transactions t
                    JOIN transaction_categories tc ON tc.transaction_id = t.id
                    JOIN budget_categories bc ON bc.budget_id = t.budget_id AND bc.name = tc.category_name
                    JOIN goals_budget_categories gbc ON gbc.budget_cat_id = bc.id
                    WHERE gbc.goal_id=@gid
                      AND COALESCE(t.deleted, FALSE) = FALSE

                    UNION ALL

                    SELECT t.id, t.date, t.merchant, t.amount AS amount, t.budget_id,
                           COALESCE(t.is_income, FALSE) AS is_income,
                           COALESCE(t.transaction_type, 'standard') AS tx_type,
                           'funded' AS source
                    FROM transactions t
                    WHERE t.funded_by_goal_id=@gid
                      AND COALESCE(t.deleted, FALSE) = FALSE
                      AND COALESCE(t.transaction_type, 'standard') = 'standard'
                      AND COALESCE(t.is_income, FALSE) = FALSE

                    ORDER BY date DESC";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("gid", gid);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var txId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    var txDate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                    var merchant = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    var amount = reader.IsDBNull(3) ? 0 : (double)reader.GetDecimal(3);
                    var budgetId = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                    var isIncome = !reader.IsDBNull(5) && reader.GetBoolean(5);
                    var txType = reader.IsDBNull(6) ? "standard" : reader.GetString(6);
                    var source = reader.IsDBNull(7) ? "linked" : reader.GetString(7);

                    // Classify the row:
                    //  - 'funded' source: always spend (a standard expense
                    //    explicitly tagged as drawn from this goal).
                    //  - 'linked' transfer: signed split (positive = contribution, negative = withdrawal)
                    //  - 'linked' standard income: contribution
                    //  - 'linked' standard expense: withdrawal
                    bool isContribution;
                    if (source == "funded")
                    {
                        if (amount == 0) continue;
                        isContribution = false;
                    }
                    else if (txType == "transfer")
                    {
                        if (amount == 0) continue;
                        isContribution = amount > 0;
                    }
                    else
                    {
                        isContribution = isIncome;
                    }

                    if (isContribution)
                    {
                        details.Contributions.Add(new GoalContribution
                        {
                            TxId = txId,
                            TxDate = txDate?.ToString("yyyy-MM-dd"),
                            Merchant = merchant,
                            Month = txDate?.ToString("yyyy-MM"),
                            BudgetId = budgetId,
                            Amount = Math.Abs(amount)
                        });
                    }
                    else
                    {
                        details.Spend.Add(new GoalSpend
                        {
                            TxId = txId,
                            TxDate = txDate?.ToString("yyyy-MM-dd"),
                            Merchant = merchant,
                            BudgetId = budgetId,
                            Amount = Math.Abs(amount)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch goal details for {GoalId}", goalId);
                throw;
            }

            return details;
        }
    }
}
