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

        public async Task SaveGoal(Goal goal)
        {
            try
            {
                _logger.LogInformation("Saving goal {GoalId}", goal.Id);
                await using var conn = await _db.GetOpenConnectionAsync();
                const string sql = @"INSERT INTO goals (id, entity_id, name, total_target, monthly_target, target_date, archived, created_at, updated_at)
VALUES (@id,@entity_id,@name,@total_target,@monthly_target,@target_date,@archived, now(), now())
ON CONFLICT (id) DO UPDATE SET entity_id=EXCLUDED.entity_id, name=EXCLUDED.name, total_target=EXCLUDED.total_target, monthly_target=EXCLUDED.monthly_target, target_date=EXCLUDED.target_date, archived=EXCLUDED.archived, updated_at=now();";
                await using var cmd = new NpgsqlCommand(sql, conn);

                if (!Guid.TryParse(goal.Id, out var goalId))
                {
                    throw new ArgumentException($"Invalid goal ID: {goal.Id}");
                }

                if (!Guid.TryParse(goal.EntityId, out var entityId))
                {
                    throw new ArgumentException($"Invalid entity ID: {goal.EntityId}");
                }

                cmd.Parameters.AddWithValue("id", goalId);
                cmd.Parameters.AddWithValue("entity_id", entityId);
                cmd.Parameters.AddWithValue("name", (object?)goal.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("total_target", (decimal)goal.TotalTarget);
                cmd.Parameters.AddWithValue("monthly_target", (decimal)goal.MonthlyTarget);
                cmd.Parameters.AddWithValue("target_date", string.IsNullOrEmpty(goal.TargetDate) ? (object)DBNull.Value : DateTime.Parse(goal.TargetDate));
                cmd.Parameters.AddWithValue("archived", goal.Archived);
                await cmd.ExecuteNonQueryAsync();

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

                const string catSql = @"INSERT INTO budget_categories (budget_id, name, target, is_fund, ""group"", carryover)
VALUES (@bid, @name, @target, true, @group, 0)
ON CONFLICT (budget_id, name) DO UPDATE SET target=EXCLUDED.target, is_fund=true, ""group""=EXCLUDED.""group""";

                foreach (var bid in budgetIds)
                {
                    await using var catCmd = new NpgsqlCommand(catSql, conn);
                    catCmd.Parameters.AddWithValue("bid", bid);
                    catCmd.Parameters.AddWithValue("name", (object?)goal.Name ?? DBNull.Value);
                    catCmd.Parameters.AddWithValue("target", (decimal)goal.MonthlyTarget);
                    catCmd.Parameters.AddWithValue("group", "Savings");
                    await catCmd.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Goal {GoalId} saved and categories updated", goal.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save goal {GoalId}", goal?.Id);
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
                const string sql = "SELECT id, entity_id, name, total_target, monthly_target, target_date, archived FROM goals WHERE entity_id=@eid";
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
                        Archived = reader.IsDBNull(6) ? false : reader.GetBoolean(6)
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
    }
}
