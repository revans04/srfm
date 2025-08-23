// FamilyBudgetApi/Services/SyncService.cs
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Add Supabase namespaces if you have referenced Supabase in your project.
using Supabase;
using Postgrest;
using Postgrest.Models;

using FamilyBudgetApi.Models;

namespace FamilyBudgetApi.Services
{
    /// <summary>
    /// Service to synchronize Firestore data into a Supabase/PostgreSQL database.
    /// </summary>
    public class SyncService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<SyncService> _logger;

        public SyncService(FirestoreDb firestoreDb, ILogger<SyncService> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        /// <summary>
        /// Perform a full synchronization of budgets and transactions from Firestore into Supabase.
        /// </summary>
        public async Task FullSyncFirestoreToSupabaseAsync()
        {
            _logger.LogInformation("Starting full Firestore → Supabase sync.");
            await SyncBudgetsAsync(null);
            await SyncTransactionsAsync(null);
            _logger.LogInformation("Completed full Firestore → Supabase sync.");
        }

        /// <summary>
        /// Perform an incremental synchronization from Firestore into Supabase.
        /// Only records updated since the provided timestamp will be synchronized.
        /// </summary>
        /// <param name="since">The timestamp after which updated records should be synced.</param>
        public async Task IncrementalSyncFirestoreToSupabaseAsync(DateTime since)
        {
            _logger.LogInformation("Starting incremental Firestore → Supabase sync since {Since}.", since);
            await SyncBudgetsAsync(since);
            await SyncTransactionsAsync(since);
            _logger.LogInformation("Completed incremental Firestore → Supabase sync.");
        }

        private async Task SyncBudgetsAsync(DateTime? updatedSince)
        {
            var query = _firestoreDb.Collection("budgets");
            if (updatedSince.HasValue)
            {
                // Firestore stores timestamps as 'updatedAt'
                query = query.WhereGreaterThanOrEqualTo("updatedAt", updatedSince.Value);
            }

            var snapshot = await query.GetSnapshotAsync();
            var supabaseBudgets = new List<PgBudget>();

            foreach (var doc in snapshot.Documents)
            {
                var budget = doc.ConvertTo<Budget>();
                var pgBudget = new PgBudget
                {
                    Id = budget.Id,
                    FamilyId = TryParseGuid(budget.FamilyId),
                    EntityId = TryParseGuid(budget.EntityId),
                    Month = budget.Month,
                    Label = budget.Label,
                    IncomeTarget = budget.IncomeTarget,
                    OriginalBudgetId = budget.OriginalBudgetId,
                    CreatedAt = budget.CreatedAt,
                    UpdatedAt = budget.UpdatedAt
                };
                supabaseBudgets.Add(pgBudget);
            }

            if (supabaseBudgets.Count == 0)
            {
                _logger.LogInformation("No budgets to upsert into Supabase.");
                return;
            }

            using var supabase = CreateSupabaseClient();
            await supabase.InitializeAsync();
            // Upsert using Postgrest (on conflict by primary key 'id')
            await supabase.From<PgBudget>().Upsert(supabaseBudgets, onConflict: new[] { "id" });
        }

        private async Task SyncTransactionsAsync(DateTime? updatedSince)
        {
            var query = _firestoreDb.Collection("transactions");
            if (updatedSince.HasValue)
            {
                query = query.WhereGreaterThanOrEqualTo("updatedAt", updatedSince.Value);
            }
            var snapshot = await query.GetSnapshotAsync();
            var supabaseTransactions = new List<PgTransaction>();

            foreach (var doc in snapshot.Documents)
            {
                var transaction = doc.ConvertTo<Transaction>();
                var pgTransaction = new PgTransaction
                {
                    Id = transaction.Id,
                    BudgetId = transaction.BudgetId,
                    Date = transaction.Date,
                    BudgetMonth = transaction.BudgetMonth,
                    Merchant = transaction.Merchant,
                    Amount = transaction.Amount,
                    Notes = transaction.Notes,
                    Recurring = transaction.Recurring,
                    RecurringInterval = transaction.RecurringInterval,
                    UserId = transaction.UserId,
                    IsIncome = transaction.IsIncome,
                    AccountNumber = transaction.AccountNumber,
                    AccountSource = transaction.AccountSource,
                    PostedDate = transaction.PostedDate,
                    ImportedMerchant = transaction.ImportedMerchant,
                    Status = transaction.Status,
                    CheckNumber = transaction.CheckNumber,
                    Deleted = transaction.Deleted,
                    EntityId = TryParseGuid(transaction.EntityId),
                    CreatedAt = transaction.CreatedAt,
                    UpdatedAt = transaction.UpdatedAt
                };
                supabaseTransactions.Add(pgTransaction);
            }

            if (supabaseTransactions.Count == 0)
            {
                _logger.LogInformation("No transactions to upsert into Supabase.");
                return;
            }

            using var supabase = CreateSupabaseClient();
            await supabase.InitializeAsync();
            await supabase.From<PgTransaction>().Upsert(supabaseTransactions, onConflict: new[] { "id" });
        }

        /// <summary>
        /// Helper to create a Supabase client from environment variables.
        /// </summary>
        private Supabase.Client CreateSupabaseClient()
        {
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") ?? Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Supabase configuration missing. Please set SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY or SUPABASE_ANON_KEY.");
            }
            return new Supabase.Client(url, key);
        }

        private Guid? TryParseGuid(string? input)
        {
            return Guid.TryParse(input, out var guid) ? guid : (Guid?)null;
        }
    }

    /// <summary>
    /// Supabase model representing the 'budgets' table.
    /// </summary>
    [Table("budgets")]
    public class PgBudget : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; }

        [Column("family_id")]
        public Guid? FamilyId { get; set; }

        [Column("entity_id")]
        public Guid? EntityId { get; set; }

        [Column("month")]
        public string? Month { get; set; }

        [Column("label")]
        public string? Label { get; set; }

        [Column("income_target")]
        public decimal? IncomeTarget { get; set; }

        [Column("original_budget_id")]
        public string? OriginalBudgetId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Supabase model representing the 'transactions' table.
    /// </summary>
    [Table("transactions")]
    public class PgTransaction : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; }

        [Column("budget_id")]
        public string BudgetId { get; set; } = string.Empty;

        [Column("date")]
        public DateTime? Date { get; set; }

        [Column("budget_month")]
        public string? BudgetMonth { get; set; }

        [Column("merchant")]
        public string? Merchant { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("recurring")]
        public bool Recurring { get; set; }

        [Column("recurring_interval")]
        public string? RecurringInterval { get; set; }

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("is_income")]
        public bool IsIncome { get; set; }

        [Column("account_number")]
        public string? AccountNumber { get; set; }

        [Column("account_source")]
        public string? AccountSource { get; set; }

        [Column("posted_date")]
        public DateTime? PostedDate { get; set; }

        [Column("imported_merchant")]
        public string? ImportedMerchant { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("check_number")]
        public string? CheckNumber { get; set; }

        [Column("deleted")]
        public bool? Deleted { get; set; }

        [Column("entity_id")]
        public Guid? EntityId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}