using Google.Cloud.Firestore;
using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;

namespace FamilyBudgetApi.Services
{
    public class StatementService
    {
        private readonly FirestoreDb _db;
        private readonly BudgetService _budgetService;
        private readonly ILogger<StatementService> _logger;

        public StatementService(FirestoreDb db, BudgetService budgetService, ILogger<StatementService> logger)
        {
            _db = db;
            _budgetService = budgetService;
            _logger = logger;
        }

        public async Task<List<Statement>> GetStatements(string familyId, string accountNumber)
        {
            var colRef = _db.Collection("families").Document(familyId)
                .Collection("accounts").Document(accountNumber)
                .Collection("statements");
            var snap = await colRef.GetSnapshotAsync();
            return snap.Documents.Select(d =>
            {
                var st = d.ConvertTo<Statement>();
                return st;
            }).OrderBy(s => s.StartDate).ToList();
        }

        public async Task SaveStatement(string familyId, string accountNumber, Statement statement, List<(string budgetId, string transactionId)> txRefs, string userId, string userEmail)
        {
            var docRef = _db.Collection("families").Document(familyId)
                .Collection("accounts").Document(accountNumber)
                .Collection("statements").Document(statement.Id);
            await docRef.SetAsync(statement, SetOptions.MergeAll);

            if (txRefs == null || txRefs.Count == 0) return;

            var grouped = txRefs.GroupBy(t => t.budgetId);
            foreach (var group in grouped)
            {
                var budget = await _budgetService.GetBudget(group.Key);
                if (budget == null) continue;

                var list = budget.Transactions ?? new List<Models.Transaction>();
                foreach (var txRef in group)
                {
                    var idx = list.FindIndex(t => t.Id == txRef.transactionId);
                    if (idx >= 0)
                    {
                        list[idx].Status = "R";
                    }
                }
                budget.Transactions = list;
                await _budgetService.SaveBudget(group.Key, budget, userId, userEmail);
            }
        }
    }
}