// FamilyBudgetApi/Services/SyncService.cs
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Npgsql;

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
        /// Perform a full synchronization of budgets and transactions from Firestore into the PostgreSQL database.
        /// </summary>
        public async Task FullSyncFirestoreToSupabaseAsync()
        {
            _logger.LogInformation("Starting full Firestore → Supabase sync.");
            await SyncBudgetsAsync(null);
            await SyncTransactionsAsync(null);
            _logger.LogInformation("Completed full Firestore → Supabase sync.");
        }

        /// <summary>
        /// Perform an incremental synchronization from Firestore into the PostgreSQL database.
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
            Query query = _firestoreDb.Collection("budgets");
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
                    Id = budget.BudgetId,
                    FamilyId = TryParseGuid(budget.FamilyId),
                    EntityId = TryParseGuid(budget.EntityId),
                    Month = budget.Month,
                    Label = budget.Label,
                    IncomeTarget = (decimal)budget.IncomeTarget,
                    OriginalBudgetId = budget.OriginalBudgetId,
                    CreatedAt = doc.CreateTime?.ToDateTime(),
                    UpdatedAt = doc.UpdateTime?.ToDateTime()
                };
                supabaseBudgets.Add(pgBudget);
            }

            if (supabaseBudgets.Count == 0)
            {
                _logger.LogInformation("No budgets to upsert into Supabase.");
                return;
            }

            await using var conn = CreateNpgsqlConnection();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO budgets
                (id, family_id, entity_id, month, label, income_target, original_budget_id, created_at, updated_at)
                VALUES (@id, @family_id, @entity_id, @month, @label, @income_target, @original_budget_id, @created_at, @updated_at)
                ON CONFLICT (id) DO UPDATE SET
                    family_id = EXCLUDED.family_id,
                    entity_id = EXCLUDED.entity_id,
                    month = EXCLUDED.month,
                    label = EXCLUDED.label,
                    income_target = EXCLUDED.income_target,
                    original_budget_id = EXCLUDED.original_budget_id,
                    created_at = EXCLUDED.created_at,
                    updated_at = EXCLUDED.updated_at;";

            await using var transaction = await conn.BeginTransactionAsync();
            foreach (var b in supabaseBudgets)
            {
                await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("id", b.Id);
                cmd.Parameters.AddWithValue("family_id", (object?)b.FamilyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("entity_id", (object?)b.EntityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("month", (object?)b.Month ?? DBNull.Value);
                cmd.Parameters.AddWithValue("label", (object?)b.Label ?? DBNull.Value);
                cmd.Parameters.AddWithValue("income_target", (object?)b.IncomeTarget ?? DBNull.Value);
                cmd.Parameters.AddWithValue("original_budget_id", (object?)b.OriginalBudgetId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("created_at", (object?)b.CreatedAt ?? DBNull.Value);
                cmd.Parameters.AddWithValue("updated_at", (object?)b.UpdatedAt ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }

        private async Task SyncTransactionsAsync(DateTime? updatedSince)
        {
            Query query = _firestoreDb.Collection("transactions");
            if (updatedSince.HasValue)
            {
                query = query.WhereGreaterThanOrEqualTo("updatedAt", updatedSince.Value);
            }
            var snapshot = await query.GetSnapshotAsync();
            var supabaseTransactions = new List<PgTransaction>();

            foreach (var doc in snapshot.Documents)
            {
                var firestoreTransaction = doc.ConvertTo<FamilyBudgetApi.Models.Transaction>();
                var pgTransaction = new PgTransaction
                {
                    Id = firestoreTransaction.Id ?? doc.Id,
                    BudgetId = firestoreTransaction.BudgetId,
                    Date = TryParseDate(firestoreTransaction.Date),
                    BudgetMonth = firestoreTransaction.BudgetMonth,
                    Merchant = firestoreTransaction.Merchant,
                    Amount = (decimal)firestoreTransaction.Amount,
                    Notes = firestoreTransaction.Notes,
                    Recurring = firestoreTransaction.Recurring,
                    RecurringInterval = firestoreTransaction.RecurringInterval,
                    UserId = firestoreTransaction.UserId,
                    IsIncome = firestoreTransaction.IsIncome,
                    AccountNumber = firestoreTransaction.AccountNumber,
                    AccountSource = firestoreTransaction.AccountSource,
                    PostedDate = TryParseDate(firestoreTransaction.PostedDate),
                    ImportedMerchant = firestoreTransaction.ImportedMerchant,
                    Status = firestoreTransaction.Status,
                    CheckNumber = firestoreTransaction.CheckNumber,
                    Deleted = firestoreTransaction.Deleted,
                    EntityId = TryParseGuid(firestoreTransaction.EntityId),
                    CreatedAt = doc.CreateTime?.ToDateTime(),
                    UpdatedAt = doc.UpdateTime?.ToDateTime()
                };
                supabaseTransactions.Add(pgTransaction);
            }

            if (supabaseTransactions.Count == 0)
            {
                _logger.LogInformation("No transactions to upsert into Supabase.");
                return;
            }

            await using var conn = CreateNpgsqlConnection();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO transactions
                (id, budget_id, date, budget_month, merchant, amount, notes, recurring, recurring_interval, user_id,
                 is_income, account_number, account_source, posted_date, imported_merchant, status, check_number,
                 deleted, entity_id, created_at, updated_at)
                VALUES (@id, @budget_id, @date, @budget_month, @merchant, @amount, @notes, @recurring, @recurring_interval, @user_id,
                        @is_income, @account_number, @account_source, @posted_date, @imported_merchant, @status, @check_number,
                        @deleted, @entity_id, @created_at, @updated_at)
                ON CONFLICT (id) DO UPDATE SET
                    budget_id = EXCLUDED.budget_id,
                    date = EXCLUDED.date,
                    budget_month = EXCLUDED.budget_month,
                    merchant = EXCLUDED.merchant,
                    amount = EXCLUDED.amount,
                    notes = EXCLUDED.notes,
                    recurring = EXCLUDED.recurring,
                    recurring_interval = EXCLUDED.recurring_interval,
                    user_id = EXCLUDED.user_id,
                    is_income = EXCLUDED.is_income,
                    account_number = EXCLUDED.account_number,
                    account_source = EXCLUDED.account_source,
                    posted_date = EXCLUDED.posted_date,
                    imported_merchant = EXCLUDED.imported_merchant,
                    status = EXCLUDED.status,
                    check_number = EXCLUDED.check_number,
                    deleted = EXCLUDED.deleted,
                    entity_id = EXCLUDED.entity_id,
                    created_at = EXCLUDED.created_at,
                    updated_at = EXCLUDED.updated_at;";

            await using var dbTransaction = await conn.BeginTransactionAsync();
            foreach (var t in supabaseTransactions)
            {
                await using var cmd = new NpgsqlCommand(sql, conn, dbTransaction);
                cmd.Parameters.AddWithValue("id", t.Id);
                cmd.Parameters.AddWithValue("budget_id", t.BudgetId);
                cmd.Parameters.AddWithValue("date", (object?)t.Date ?? DBNull.Value);
                cmd.Parameters.AddWithValue("budget_month", (object?)t.BudgetMonth ?? DBNull.Value);
                cmd.Parameters.AddWithValue("merchant", (object?)t.Merchant ?? DBNull.Value);
                cmd.Parameters.AddWithValue("amount", t.Amount);
                cmd.Parameters.AddWithValue("notes", (object?)t.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("recurring", t.Recurring);
                cmd.Parameters.AddWithValue("recurring_interval", (object?)t.RecurringInterval ?? DBNull.Value);
                cmd.Parameters.AddWithValue("user_id", (object?)t.UserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("is_income", t.IsIncome);
                cmd.Parameters.AddWithValue("account_number", (object?)t.AccountNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("account_source", (object?)t.AccountSource ?? DBNull.Value);
                cmd.Parameters.AddWithValue("posted_date", (object?)t.PostedDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("imported_merchant", (object?)t.ImportedMerchant ?? DBNull.Value);
                cmd.Parameters.AddWithValue("status", (object?)t.Status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("check_number", (object?)t.CheckNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("deleted", (object?)t.Deleted ?? DBNull.Value);
                cmd.Parameters.AddWithValue("entity_id", (object?)t.EntityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("created_at", (object?)t.CreatedAt ?? DBNull.Value);
                cmd.Parameters.AddWithValue("updated_at", (object?)t.UpdatedAt ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
            await dbTransaction.CommitAsync();
        }

        /// <summary>
        /// Helper to create an Npgsql connection from the SUPABASE_DB_CONNECTION environment variable.
        /// </summary>
        private NpgsqlConnection CreateNpgsqlConnection()
        {
            var connectionString = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Supabase DB connection string missing. Please set SUPABASE_DB_CONNECTION.");
            }
            return new NpgsqlConnection(connectionString);
        }

        private Guid? TryParseGuid(string? input)
        {
            return Guid.TryParse(input, out var guid) ? guid : (Guid?)null;
        }

        private DateTime? TryParseDate(string? input)
        {
            return DateTime.TryParse(input, out var dt) ? dt : (DateTime?)null;
        }
    }

    /// <summary>
    /// Represents the 'budgets' table.
    /// </summary>
    public class PgBudget
    {
        public string Id { get; set; } = string.Empty;
        public Guid? FamilyId { get; set; }
        public Guid? EntityId { get; set; }
        public string? Month { get; set; }
        public string? Label { get; set; }
        public decimal? IncomeTarget { get; set; }
        public string? OriginalBudgetId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents the 'transactions' table.
    /// </summary>
    public class PgTransaction
    {
        public string Id { get; set; } = string.Empty;
        public string BudgetId { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string? BudgetMonth { get; set; }
        public string? Merchant { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public bool Recurring { get; set; }
        public string? RecurringInterval { get; set; }
        public string? UserId { get; set; }
        public bool IsIncome { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountSource { get; set; }
        public DateTime? PostedDate { get; set; }
        public string? ImportedMerchant { get; set; }
        public string? Status { get; set; }
        public string? CheckNumber { get; set; }
        public bool? Deleted { get; set; }
        public Guid? EntityId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
