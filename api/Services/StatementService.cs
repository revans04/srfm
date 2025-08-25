using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FamilyBudgetApi.Services;

/// <summary>
/// Statement service placeholder for Supabase migration.
/// </summary>
public class StatementService
{
    private readonly SupabaseDbService _db;
    private readonly BudgetService _budgetService;
    private readonly ILogger<StatementService> _logger;

    public StatementService(SupabaseDbService db, BudgetService budgetService, ILogger<StatementService> logger)
    {
        _db = db;
        _budgetService = budgetService;
        _logger = logger;
    }

    public async Task<List<Statement>> GetStatements(string familyId, string accountNumber)
    {
        _logger.LogInformation("GetStatements called for family {FamilyId} account {Account}", familyId, accountNumber);

        await using var conn = await _db.GetOpenConnectionAsync();

        if (!Guid.TryParse(familyId, out var fid))
        {
            _logger.LogWarning("Invalid familyId {FamilyId} when fetching statements", familyId);
            return new List<Statement>();
        }

        if (!Guid.TryParse(accountNumber, out var aid))
        {
            _logger.LogWarning("Invalid account id {Account}", accountNumber);
            return new List<Statement>();
        }

        const string accountCheck = "SELECT 1 FROM accounts WHERE family_id=@fid AND id=@aid";
        await using (var check = new NpgsqlCommand(accountCheck, conn))
        {
            check.Parameters.AddWithValue("fid", fid);
            check.Parameters.AddWithValue("aid", aid);
            var exists = await check.ExecuteScalarAsync();
            if (exists == null)
            {
                _logger.LogInformation("Account {Account} not found for family {FamilyId}", accountNumber, familyId);
                return new List<Statement>();
            }
        }

        const string sql = @"SELECT id, account_id, start_date, end_date, starting_balance, ending_balance, reconciled
                              FROM account_statements WHERE account_id=@aid ORDER BY start_date";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("aid", accountNumber);
        await using var reader = await cmd.ExecuteReaderAsync();

        var results = new List<Statement>();
        while (await reader.ReadAsync())
        {
            var st = new Statement
            {
                Id = reader.GetInt64(0).ToString(),
                AccountNumber = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                StartDate = reader.IsDBNull(2) ? string.Empty : reader.GetDateTime(2).ToString("yyyy-MM-dd"),
                EndDate = reader.IsDBNull(3) ? string.Empty : reader.GetDateTime(3).ToString("yyyy-MM-dd"),
                StartingBalance = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                EndingBalance = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                Reconciled = !reader.IsDBNull(6) && reader.GetBoolean(6)
            };
            results.Add(st);
        }

        _logger.LogInformation("Retrieved {Count} statements for account {Account}", results.Count, accountNumber);
        return results;
    }
    public Task SaveStatement(string familyId, string accountNumber, Statement statement, List<(string budgetId, string transactionId)> txRefs, string userId, string userEmail)
    {
        _logger.LogInformation("SaveStatement called for family {FamilyId} account {Account}", familyId, accountNumber);
        throw new NotImplementedException();
    }
    public Task DeleteStatement(string familyId, string accountNumber, string statementId, List<(string budgetId, string transactionId)> txRefs, string userId, string userEmail)
    {
        _logger.LogInformation("DeleteStatement called for family {FamilyId} account {Account} statement {StatementId}", familyId, accountNumber, statementId);
        throw new NotImplementedException();
    }
    public Task UnreconcileStatement(string familyId, string accountNumber, string statementId, List<(string budgetId, string transactionId)> txRefs, string userId, string userEmail)
    {
        _logger.LogInformation("UnreconcileStatement called for family {FamilyId} account {Account} statement {StatementId}", familyId, accountNumber, statementId);
        throw new NotImplementedException();
    }
}

