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
                const string insertSql = @"INSERT INTO goals (id, entity_id, name, total_target, monthly_target, target_date, archived, created_at, updated_at)
VALUES (@id,@entity_id,@name,@total_target,@monthly_target,@target_date,@archived, now(), now());";

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
                    insertCmd.Parameters.AddWithValue("target_date", string.IsNullOrEmpty(goal.TargetDate) ? (object)DBNull.Value : DateTime.Parse(goal.TargetDate));
                    insertCmd.Parameters.AddWithValue("archived", goal.Archived);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                // Ensure a matching budget category exists for all budgets under the entity
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

                const string findCatSql = "SELECT id FROM budget_categories WHERE budget_id=@bid AND name=@name";
                const string updateCatSql = "UPDATE budget_categories SET target=@target, is_fund=true, \"group\"=@group WHERE id=@id";
                const string insertCatSql = @"INSERT INTO budget_categories (budget_id, name, target, is_fund, ""group"", carryover)
VALUES (@bid, @name, @target, true, @group, 0) RETURNING id";
                const string deleteAssocSql = "DELETE FROM goals_budget_categories WHERE budget_cat_id=@catId";
                const string insertAssocSql = @"INSERT INTO goals_budget_categories (goal_id, budget_cat_id) VALUES (@goal_id, @budget_cat_id)";

                foreach (var bid in budgetIds)
                {
                    long budgetCatId;

                    // Look for an existing category with this goal's name
                    await using (var findCmd = new NpgsqlCommand(findCatSql, conn))
                    {
                        findCmd.Parameters.AddWithValue("bid", bid);
                        findCmd.Parameters.AddWithValue("name", (object?)goal.Name ?? DBNull.Value);
                        var idObj = await findCmd.ExecuteScalarAsync();
                        if (idObj != null)
                        {
                            budgetCatId = idObj is long l ? l : Convert.ToInt64(idObj);
                            // Update existing category so transactions remain linked
                            await using var updCmd = new NpgsqlCommand(updateCatSql, conn);
                            updCmd.Parameters.AddWithValue("id", budgetCatId);
                            updCmd.Parameters.AddWithValue("target", (decimal)goal.MonthlyTarget);
                            updCmd.Parameters.AddWithValue("group", "Savings");
                            await updCmd.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            await using var insCmd = new NpgsqlCommand(insertCatSql, conn);
                            insCmd.Parameters.AddWithValue("bid", bid);
                            insCmd.Parameters.AddWithValue("name", (object?)goal.Name ?? DBNull.Value);
                            insCmd.Parameters.AddWithValue("target", (decimal)goal.MonthlyTarget);
                            insCmd.Parameters.AddWithValue("group", "Savings");
                            var newId = await insCmd.ExecuteScalarAsync();
                            budgetCatId = newId is long l ? l : Convert.ToInt64(newId);
                        }
                    }

                    // Ensure the category is associated with this goal
                    await using (var delAssocCmd = new NpgsqlCommand(deleteAssocSql, conn))
                    {
                        delAssocCmd.Parameters.AddWithValue("catId", budgetCatId);
                        await delAssocCmd.ExecuteNonQueryAsync();
                    }

                    await using (var assocCmd = new NpgsqlCommand(insertAssocSql, conn))
                    {
                        assocCmd.Parameters.AddWithValue("goal_id", goalId);
                        assocCmd.Parameters.AddWithValue("budget_cat_id", budgetCatId);
                        await assocCmd.ExecuteNonQueryAsync();
                    }
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
                const string updateSql = @"UPDATE goals SET entity_id=@entity_id, name=@name, total_target=@total_target, monthly_target=@monthly_target, target_date=@target_date, archived=@archived, updated_at=now() WHERE id=@id";

                if (!Guid.TryParse(goal.Id, out var goalId))
                {
                    throw new ArgumentException($"Invalid goal ID: {goal.Id}");
                }

                if (!Guid.TryParse(goal.EntityId, out var entityId))
                {
                    throw new ArgumentException($"Invalid entity ID: {goal.EntityId}");
                }

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("id", goalId);
                cmd.Parameters.AddWithValue("entity_id", entityId);
                cmd.Parameters.AddWithValue("name", (object?)goal.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("total_target", (decimal)goal.TotalTarget);
                cmd.Parameters.AddWithValue("monthly_target", (decimal)goal.MonthlyTarget);
                cmd.Parameters.AddWithValue("target_date", string.IsNullOrEmpty(goal.TargetDate) ? (object)DBNull.Value : DateTime.Parse(goal.TargetDate));
                cmd.Parameters.AddWithValue("archived", goal.Archived);
                await cmd.ExecuteNonQueryAsync();

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

                const string sql = @"SELECT g.id, g.entity_id, g.name, g.total_target, g.monthly_target, g.target_date, g.archived,
                                            COALESCE(SUM(CASE WHEN tc.amount > 0 THEN tc.amount ELSE 0 END),0) AS saved,
                                            COALESCE(SUM(CASE WHEN tc.amount < 0 THEN -tc.amount ELSE 0 END),0) AS spent
                                     FROM goals g
                                     LEFT JOIN goals_budget_categories gbc ON gbc.goal_id = g.id
                                     LEFT JOIN budget_categories bc ON bc.id = gbc.budget_cat_id
                                     LEFT JOIN transactions t ON t.budget_id = bc.budget_id
                                     LEFT JOIN transaction_categories tc ON tc.transaction_id = t.id AND tc.category_name = bc.name
                                     WHERE g.entity_id=@eid
                                     GROUP BY g.id, g.entity_id, g.name, g.total_target, g.monthly_target, g.target_date, g.archived";
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
                        SpentToDate = reader.IsDBNull(8) ? 0 : (double)reader.GetDecimal(8)
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

                const string sql = @"SELECT t.id, t.date, t.merchant, tc.amount
                                           FROM transactions t
                                           JOIN transaction_categories tc ON tc.transaction_id = t.id
                                           JOIN budget_categories bc ON bc.budget_id = t.budget_id AND bc.name = tc.category_name
                                           JOIN goals_budget_categories gbc ON gbc.budget_cat_id = bc.id
                                           WHERE gbc.goal_id=@gid
                                           ORDER BY t.date";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("gid", gid);
                await using var reader = await cmd.ExecuteReaderAsync();

                var contribByMonth = new Dictionary<string, double>();
                while (await reader.ReadAsync())
                {
                    var txId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    var txDate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                    var merchant = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    var amount = reader.IsDBNull(3) ? 0 : (double)reader.GetDecimal(3);

                    if (amount > 0)
                    {
                        var month = txDate?.ToString("yyyy-MM") ?? string.Empty;
                        if (contribByMonth.ContainsKey(month))
                        {
                            contribByMonth[month] += amount;
                        }
                        else
                        {
                            contribByMonth[month] = amount;
                        }
                    }
                    else if (amount < 0)
                    {
                        details.Spend.Add(new GoalSpend
                        {
                            TxId = txId,
                            TxDate = txDate?.ToString("yyyy-MM-dd"),
                            Merchant = merchant,
                            Amount = Math.Abs(amount)
                        });
                    }
                }

                foreach (var kvp in contribByMonth)
                {
                    details.Contributions.Add(new GoalContribution
                    {
                        Month = kvp.Key,
                        Amount = kvp.Value
                    });
                }

                details.Contributions.Sort((a, b) => string.CompareOrdinal(a.Month, b.Month));
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
