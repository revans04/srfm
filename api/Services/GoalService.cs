using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
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
            _logger.LogInformation("Saving goal {GoalId}", goal.Id);
            await using var conn = await _db.GetOpenConnectionAsync();
            const string sql = @"INSERT INTO goals (id, entity_id, name, total_target, monthly_target, target_date, notes, archived, created_at, updated_at)
VALUES (@id,@entity_id,@name,@total_target,@monthly_target,@target_date,@notes,@archived, now(), now())
ON CONFLICT (id) DO UPDATE SET entity_id=EXCLUDED.entity_id, name=EXCLUDED.name, total_target=EXCLUDED.total_target, monthly_target=EXCLUDED.monthly_target, target_date=EXCLUDED.target_date, notes=EXCLUDED.notes, archived=EXCLUDED.archived, updated_at=now();";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", Guid.Parse(goal.Id));
            cmd.Parameters.AddWithValue("entity_id", Guid.Parse(goal.EntityId));
            cmd.Parameters.AddWithValue("name", (object?)goal.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("total_target", (decimal)goal.TotalTarget);
            cmd.Parameters.AddWithValue("monthly_target", (decimal)goal.MonthlyTarget);
            cmd.Parameters.AddWithValue("target_date", string.IsNullOrEmpty(goal.TargetDate) ? (object)DBNull.Value : DateTime.Parse(goal.TargetDate));
            cmd.Parameters.AddWithValue("notes", (object?)goal.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("archived", goal.Archived);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
