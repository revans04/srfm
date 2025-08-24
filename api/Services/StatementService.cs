using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;

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

    public Task<List<Statement>> GetStatements(string familyId, string accountNumber)
    {
        _logger.LogInformation("GetStatements called for family {FamilyId} account {Account}", familyId, accountNumber);
        throw new NotImplementedException();
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

