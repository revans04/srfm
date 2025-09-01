using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Linq;

namespace FamilyBudgetApi.Services;

/// <summary>
/// Budget service backed by Supabase/PostgreSQL.
/// Only a subset of operations are implemented; additional endpoints
/// still require migration from the legacy Firestore implementation.
/// </summary>
public class BudgetService
{
    private readonly SupabaseDbService _db;
    private readonly ILogger<BudgetService> _logger;

    public BudgetService(SupabaseDbService db, ILogger<BudgetService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Load budgets belonging to the user's family. Optionally filter by entity.
    /// </summary>
    public async Task<List<BudgetInfo>> LoadAccessibleBudgets(string userId, string? entityId = null)
    {
        _logger.LogInformation("Loading budgets for user {UserId} and entity {EntityId}", userId, entityId);
        await using var conn = await _db.GetOpenConnectionAsync();

        // Resolve family id for the user
        const string sqlFamily = "SELECT family_id FROM family_members WHERE user_id=@uid LIMIT 1";
        await using var famCmd = new NpgsqlCommand(sqlFamily, conn);
        famCmd.Parameters.AddWithValue("uid", userId);
        var familyIdObj = await famCmd.ExecuteScalarAsync();
        if (familyIdObj is not Guid familyId)
        {
            _logger.LogWarning("No family found for user {UserId}", userId);
            return new List<BudgetInfo>();
        }

        var sql = "SELECT id, family_id, entity_id, month, label, income_target, original_budget_id FROM budgets WHERE family_id=@fid";
        if (!string.IsNullOrEmpty(entityId)) sql += " AND entity_id=@eid";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fid", familyId);
        if (!string.IsNullOrEmpty(entityId) && Guid.TryParse(entityId, out var eid))
            cmd.Parameters.AddWithValue("eid", eid);

        await using var reader = await cmd.ExecuteReaderAsync();
        var budgets = new List<BudgetInfo>();
        while (await reader.ReadAsync())
        {
            budgets.Add(new BudgetInfo
            {
                BudgetId = reader.GetString(0),
                FamilyId = reader.GetGuid(1).ToString(),
                EntityId = reader.IsDBNull(2) ? null : reader.GetGuid(2).ToString(),
                Month = reader.GetString(3),
                Label = reader.IsDBNull(4) ? null : reader.GetString(4),
                IncomeTarget = reader.IsDBNull(5) ? 0 : (double)reader.GetDecimal(5),
                OriginalBudgetId = reader.IsDBNull(6) ? null : reader.GetString(6),
                IsOwner = true
            });
        }
        await reader.DisposeAsync();

        // Performance: return thin summaries for list view; 
        // load full budget details on demand via GetBudget.

        _logger.LogInformation("Loaded {Count} budgets for user {UserId}", budgets.Count, userId);
        return budgets;
    }

    /// <summary>
    /// Budgets that have been shared with the user by others.
    /// </summary>
    public async Task<List<SharedBudget>> GetSharedBudgets(string userId)
    {
        _logger.LogInformation("Fetching shared budgets for user {UserId}", userId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "SELECT owner_uid, budget_id FROM shared_budgets WHERE user_id=@uid";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);
        await using var reader = await cmd.ExecuteReaderAsync();

        var map = new Dictionary<string, SharedBudget>();
        while (await reader.ReadAsync())
        {
            var owner = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var budgetId = reader.GetString(1);
            if (!map.TryGetValue(owner, out var sb))
            {
                sb = new SharedBudget { OwnerUid = owner, UserId = userId };
                map[owner] = sb;
            }
            sb.BudgetIds.Add(budgetId);
        }
        _logger.LogInformation("User {UserId} has access to {Count} shared budgets", userId, map.Count);
        return map.Values.ToList();
    }

    private async Task LoadBudgetDetails(NpgsqlConnection conn, Budget budget, bool includeTransactions = true)
    {
        if (includeTransactions)
        {
            const string sqlTx = "SELECT id, date, budget_month, merchant, amount, notes, recurring, recurring_interval, user_id, is_income, account_number, account_source, posted_date, imported_merchant, status, check_number, deleted, entity_id FROM transactions WHERE budget_id=@id";
            await using (var txCmd = new NpgsqlCommand(sqlTx, conn))
            {
                txCmd.Parameters.AddWithValue("id", budget.BudgetId);
                await using var txReader = await txCmd.ExecuteReaderAsync();
                while (await txReader.ReadAsync())
                {
                    var tx = new Transaction
                    {
                        Id = txReader.GetString(0),
                        BudgetId = budget.BudgetId,
                        Date = txReader.IsDBNull(1) ? null : txReader.GetDateTime(1).ToString("yyyy-MM-dd"),
                        BudgetMonth = txReader.IsDBNull(2) ? null : txReader.GetString(2),
                        Merchant = txReader.IsDBNull(3) ? null : txReader.GetString(3),
                        Amount = (double)txReader.GetDecimal(4),
                        Notes = txReader.IsDBNull(5) ? null : txReader.GetString(5),
                        Recurring = txReader.GetBoolean(6),
                        RecurringInterval = txReader.IsDBNull(7) ? null : txReader.GetString(7),
                        UserId = txReader.IsDBNull(8) ? null : txReader.GetString(8),
                        IsIncome = txReader.GetBoolean(9),
                        AccountNumber = txReader.IsDBNull(10) ? null : txReader.GetString(10),
                        AccountSource = txReader.IsDBNull(11) ? null : txReader.GetString(11),
                        PostedDate = txReader.IsDBNull(12) ? null : txReader.GetDateTime(12).ToString("yyyy-MM-dd"),
                        ImportedMerchant = txReader.IsDBNull(13) ? null : txReader.GetString(13),
                        Status = txReader.IsDBNull(14) ? null : txReader.GetString(14),
                        CheckNumber = txReader.IsDBNull(15) ? null : txReader.GetString(15),
                        Deleted = txReader.IsDBNull(16) ? (bool?)null : txReader.GetBoolean(16),
                        EntityId = txReader.IsDBNull(17) ? null : txReader.GetGuid(17).ToString()
                    };
                    budget.Transactions.Add(tx);
                }
            }

            if (budget.Transactions.Count > 0)
            {
                var ids = budget.Transactions.Select(t => t.Id).ToArray();
                const string sqlTxCats = "SELECT transaction_id, category_name, amount FROM transaction_categories WHERE transaction_id = ANY(@ids)";
                await using var catCmd = new NpgsqlCommand(sqlTxCats, conn);
                catCmd.Parameters.AddWithValue("ids", ids);
                await using var catReader = await catCmd.ExecuteReaderAsync();
                var txMap = budget.Transactions.ToDictionary(t => t.Id!);
                while (await catReader.ReadAsync())
                {
                    var txId = catReader.GetString(0);
                    if (!txMap.TryGetValue(txId, out var tx)) continue;
                    tx.Categories ??= new List<TransactionCategory>();
                    tx.Categories.Add(new TransactionCategory
                    {
                        Category = catReader.IsDBNull(1) ? null : catReader.GetString(1),
                        Amount = catReader.IsDBNull(2) ? 0 : (double)catReader.GetDecimal(2)
                    });
                }
            }
        }

        const string sqlCats = "SELECT name, target, is_fund, \"group\", carryover FROM budget_categories WHERE budget_id=@id";
        await using (var catCmd = new NpgsqlCommand(sqlCats, conn))
        {
            catCmd.Parameters.AddWithValue("id", budget.BudgetId);
            await using var catReader = await catCmd.ExecuteReaderAsync();
            while (await catReader.ReadAsync())
            {
                budget.Categories.Add(new BudgetCategory
                {
                    Name = catReader.IsDBNull(0) ? null : catReader.GetString(0),
                    Target = catReader.IsDBNull(1) ? 0 : (double)catReader.GetDecimal(1),
                    IsFund = catReader.IsDBNull(2) ? false : catReader.GetBoolean(2),
                    Group = catReader.IsDBNull(3) ? null : catReader.GetString(3),
                    Carryover = catReader.IsDBNull(4) ? null : (double?)catReader.GetDecimal(4)
                });
            }
        }

        const string sqlMerchants = "SELECT name, usage_count FROM merchants WHERE budget_id=@id";
        await using (var merchCmd = new NpgsqlCommand(sqlMerchants, conn))
        {
            merchCmd.Parameters.AddWithValue("id", budget.BudgetId);
            await using var merchReader = await merchCmd.ExecuteReaderAsync();
            while (await merchReader.ReadAsync())
            {
                budget.Merchants.Add(new Merchant
                {
                    Name = merchReader.GetString(0),
                    UsageCount = merchReader.IsDBNull(1) ? 0 : merchReader.GetInt32(1)
                });
            }
        }
    }

    /// <summary>
    /// Fetch a budget and its transactions from Supabase.
    /// </summary>
    public async Task<Budget?> GetBudget(string budgetId)
    {
        _logger.LogInformation("Fetching budget {BudgetId}", budgetId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sqlBudget = "SELECT id, family_id, entity_id, month, label, income_target, original_budget_id FROM budgets WHERE id=@id";
        await using var cmd = new NpgsqlCommand(sqlBudget, conn);
        cmd.Parameters.AddWithValue("id", budgetId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            _logger.LogWarning("Budget {BudgetId} not found", budgetId);
            return null;
        }

        var budget = new Budget
        {
            BudgetId = reader.GetString(0),
            FamilyId = reader.IsDBNull(1) ? string.Empty : reader.GetGuid(1).ToString(),
            EntityId = reader.IsDBNull(2) ? null : reader.GetGuid(2).ToString(),
            Month = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Label = reader.IsDBNull(4) ? null : reader.GetString(4),
            IncomeTarget = reader.IsDBNull(5) ? 0 : (double)reader.GetDecimal(5),
            OriginalBudgetId = reader.IsDBNull(6) ? null : reader.GetString(6),
            Transactions = new List<Transaction>(),
            Categories = new List<BudgetCategory>(),
            Merchants = new List<Merchant>()
        };
        await reader.CloseAsync();

        await LoadBudgetDetails(conn, budget);

        _logger.LogInformation("Loaded budget {BudgetId} with {TxCount} transactions, {CatCount} categories and {MerchCount} merchants", budgetId, budget.Transactions.Count, budget.Categories.Count, budget.Merchants.Count);
        return budget;
    }

    /// <summary>
    /// Upsert a budget and its transactions into Supabase.
    /// </summary>
    public async Task SaveBudget(string budgetId, Budget budget, string userId, string userEmail)
    {
        _logger.LogInformation("Saving budget {BudgetId}", budgetId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = @"INSERT INTO budgets (id, family_id, entity_id, month, label, income_target, original_budget_id, created_at, updated_at)
VALUES (@id,@family_id,@entity_id,@month,@label,@income_target,@original_budget_id, now(), now())
ON CONFLICT (id) DO UPDATE SET family_id=EXCLUDED.family_id, entity_id=EXCLUDED.entity_id, month=EXCLUDED.month, label=EXCLUDED.label, income_target=EXCLUDED.income_target, original_budget_id=EXCLUDED.original_budget_id, updated_at=now();";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", budgetId);
        cmd.Parameters.AddWithValue("family_id", Guid.Parse(budget.FamilyId));
        cmd.Parameters.AddWithValue("entity_id", string.IsNullOrEmpty(budget.EntityId) ? (object)DBNull.Value : Guid.Parse(budget.EntityId));
        cmd.Parameters.AddWithValue("month", budget.Month);
        cmd.Parameters.AddWithValue("label", (object?)budget.Label ?? DBNull.Value);
        cmd.Parameters.AddWithValue("income_target", (decimal)budget.IncomeTarget);
        cmd.Parameters.AddWithValue("original_budget_id", (object?)budget.OriginalBudgetId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();

        if (budget.Transactions != null && budget.Transactions.Count > 0)
        {
            await BatchSaveTransactions(budgetId, budget.Transactions, userId, userEmail);
        }
        await LogEdit(conn, budgetId, userId, userEmail, "save-budget");
        _logger.LogInformation("Budget {BudgetId} saved with {TxCount} transactions", budgetId, budget.Transactions?.Count ?? 0);
    }

    private DateTime? ParseDate(string? input) =>
        DateTime.TryParse(input, out var dt) ? dt : (DateTime?)null;

    private async Task LogEdit(NpgsqlConnection conn, string budgetId, string userId, string userEmail, string action)
    {
        const string sql = "INSERT INTO budget_edit_history (budget_id, user_id, user_email, timestamp, action) VALUES (@bid,@uid,@email, now(), @action)";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("bid", budgetId);
        cmd.Parameters.AddWithValue("uid", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("email", (object?)userEmail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("action", action);
        try { await cmd.ExecuteNonQueryAsync(); } catch { /* ignore if table missing */ }
    }

    private void BindTransactionParameters(NpgsqlParameterCollection parameters, string budgetId, Transaction tx)
    {
        parameters.AddWithValue("id", tx.Id);
        parameters.AddWithValue("budget_id", budgetId);
        parameters.AddWithValue("date", (object?)ParseDate(tx.Date) ?? DBNull.Value);
        parameters.AddWithValue("budget_month", (object?)tx.BudgetMonth ?? DBNull.Value);
        parameters.AddWithValue("merchant", (object?)tx.Merchant ?? DBNull.Value);
        parameters.AddWithValue("amount", (decimal)tx.Amount);
        parameters.AddWithValue("notes", (object?)tx.Notes ?? DBNull.Value);
        parameters.AddWithValue("recurring", tx.Recurring);
        parameters.AddWithValue("recurring_interval", (object?)tx.RecurringInterval ?? DBNull.Value);
        parameters.AddWithValue("user_id", (object?)tx.UserId ?? DBNull.Value);
        parameters.AddWithValue("is_income", tx.IsIncome);
        parameters.AddWithValue("account_number", (object?)tx.AccountNumber ?? DBNull.Value);
        parameters.AddWithValue("account_source", (object?)tx.AccountSource ?? DBNull.Value);
        parameters.AddWithValue("posted_date", (object?)ParseDate(tx.PostedDate) ?? DBNull.Value);
        parameters.AddWithValue("imported_merchant", (object?)tx.ImportedMerchant ?? DBNull.Value);
        parameters.AddWithValue("status", (object?)tx.Status ?? DBNull.Value);
        parameters.AddWithValue("check_number", (object?)tx.CheckNumber ?? DBNull.Value);
        parameters.AddWithValue("deleted", tx.Deleted.HasValue ? (object)tx.Deleted.Value : DBNull.Value);
        parameters.AddWithValue("entity_id", string.IsNullOrEmpty(tx.EntityId) ? (object)DBNull.Value : Guid.Parse(tx.EntityId));
    }

    private void AddCategoryCommands(NpgsqlBatch batch, string txId, List<TransactionCategory>? categories)
    {
        const string delSql = "DELETE FROM transaction_categories WHERE transaction_id=@id";
        var delCmd = new NpgsqlBatchCommand(delSql);
        delCmd.Parameters.AddWithValue("id", txId);
        batch.BatchCommands.Add(delCmd);

        if (categories == null) return;

        const string insSql = "INSERT INTO transaction_categories (transaction_id, category_name, amount) VALUES (@id,@name,@amount)";
        foreach (var cat in categories)
        {
            var insCmd = new NpgsqlBatchCommand(insSql);
            insCmd.Parameters.AddWithValue("id", txId);
            insCmd.Parameters.AddWithValue("name", cat.Category ?? string.Empty);
            insCmd.Parameters.AddWithValue("amount", (decimal)cat.Amount);
            batch.BatchCommands.Add(insCmd);
        }
    }

    private async Task SaveCategories(NpgsqlConnection conn, string txId, List<TransactionCategory>? categories)
    {
        const string delSql = "DELETE FROM transaction_categories WHERE transaction_id=@id";
        await using (var delCmd = new NpgsqlCommand(delSql, conn))
        {
            delCmd.Parameters.AddWithValue("id", txId);
            await delCmd.ExecuteNonQueryAsync();
        }

        if (categories == null) return;

        const string insSql = "INSERT INTO transaction_categories (transaction_id, category_name, amount) VALUES (@id,@name,@amount)";
        foreach (var cat in categories)
        {
            await using var insCmd = new NpgsqlCommand(insSql, conn);
            insCmd.Parameters.AddWithValue("id", txId);
            insCmd.Parameters.AddWithValue("name", cat.Category ?? string.Empty);
            insCmd.Parameters.AddWithValue("amount", (decimal)cat.Amount);
            await insCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task DeleteBudget(string budgetId, string userId, string userEmail)
    {
        _logger.LogInformation("Deleting budget {BudgetId}", budgetId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = @"DELETE FROM transaction_categories WHERE transaction_id IN (SELECT id FROM transactions WHERE budget_id=@bid);
                             DELETE FROM transactions WHERE budget_id=@bid;
                             DELETE FROM budget_categories WHERE budget_id=@bid;
                             DELETE FROM merchants WHERE budget_id=@bid;
                             DELETE FROM shared_budgets WHERE budget_id=@bid;
                             DELETE FROM budgets WHERE id=@bid;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("bid", budgetId);
        await cmd.ExecuteNonQueryAsync();
        await LogEdit(conn, budgetId, userId, userEmail, "delete-budget");
    }

    public async Task<List<EditEvent>> GetEditHistory(string budgetId, DateTime since)
    {
        _logger.LogInformation("Getting edit history for budget {BudgetId} since {Since}", budgetId, since);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "SELECT user_id, user_email, timestamp, action FROM budget_edit_history WHERE budget_id=@bid AND timestamp>=@since ORDER BY timestamp";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("bid", budgetId);
        cmd.Parameters.AddWithValue("since", since);
        var results = new List<EditEvent>();
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new EditEvent
                {
                    UserId = reader.IsDBNull(0) ? null : reader.GetString(0),
                    UserEmail = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Timestamp = reader.GetDateTime(2),
                    Action = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
        }
        catch { /* table might not exist */ }
        return results;
    }

    public async Task AddTransaction(string budgetId, Transaction transaction, string userId, string userEmail)
    {
        await SaveTransaction(budgetId, transaction, userId, userEmail);
    }


    public async Task SaveTransaction(string budgetId, Transaction transaction, string userId, string userEmail)
    {
        if (string.IsNullOrEmpty(transaction.Id))
            transaction.Id = Guid.NewGuid().ToString();
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = @"INSERT INTO transactions (id, budget_id, date, budget_month, merchant, amount, notes, recurring, recurring_interval, user_id, is_income, account_number, account_source, posted_date, imported_merchant, status, check_number, deleted, entity_id, created_at, updated_at)
VALUES (@id,@budget_id,@date,@budget_month,@merchant,@amount,@notes,@recurring,@recurring_interval,@user_id,@is_income,@account_number,@account_source,@posted_date,@imported_merchant,@status,@check_number,@deleted,@entity_id, now(), now())
ON CONFLICT (id) DO UPDATE SET budget_id=EXCLUDED.budget_id, date=EXCLUDED.date, budget_month=EXCLUDED.budget_month, merchant=EXCLUDED.merchant, amount=EXCLUDED.amount, notes=EXCLUDED.notes, recurring=EXCLUDED.recurring, recurring_interval=EXCLUDED.recurring_interval, user_id=EXCLUDED.user_id, is_income=EXCLUDED.is_income, account_number=EXCLUDED.account_number, account_source=EXCLUDED.account_source, posted_date=EXCLUDED.posted_date, imported_merchant=EXCLUDED.imported_merchant, status=EXCLUDED.status, check_number=EXCLUDED.check_number, deleted=EXCLUDED.deleted, entity_id=EXCLUDED.entity_id, updated_at=now();";
        await using var cmd = new NpgsqlCommand(sql, conn);
        BindTransactionParameters(cmd.Parameters, budgetId, transaction);
        await cmd.ExecuteNonQueryAsync();
        // Ensure at least one split row exists. If none provided, create a default
        // split using either 'Income' or 'Uncategorized' for the full amount.
        var cats = transaction.Categories;
        if (cats == null || cats.Count == 0)
        {
            cats = new List<TransactionCategory>
            {
                new TransactionCategory
                {
                    Category = transaction.IsIncome ? "Income" : "Uncategorized",
                    Amount = transaction.Amount
                }
            };
        }
        await SaveCategories(conn, transaction.Id, cats);
        await LogEdit(conn, budgetId, userId, userEmail, "save-transaction");
    }

    public async Task DeleteTransaction(string budgetId, string transactionId, string userId, string userEmail)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "DELETE FROM transaction_categories WHERE transaction_id=@id; DELETE FROM transactions WHERE id=@id AND budget_id=@bid";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", transactionId);
        cmd.Parameters.AddWithValue("bid", budgetId);
        await cmd.ExecuteNonQueryAsync();
        await LogEdit(conn, budgetId, userId, userEmail, "delete-transaction");
    }

    public async Task BatchSaveTransactions(string budgetId, List<Transaction> transactions, string userId, string userEmail)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        await using var dbTx = await conn.BeginTransactionAsync();

        const string sql = @"INSERT INTO transactions (id, budget_id, date, budget_month, merchant, amount, notes, recurring, recurring_interval, user_id, is_income, account_number, account_source, posted_date, imported_merchant, status, check_number, deleted, entity_id, created_at, updated_at)
VALUES (@id,@budget_id,@date,@budget_month,@merchant,@amount,@notes,@recurring,@recurring_interval,@user_id,@is_income,@account_number,@account_source,@posted_date,@imported_merchant,@status,@check_number,@deleted,@entity_id, now(), now())
ON CONFLICT (id) DO UPDATE SET budget_id=EXCLUDED.budget_id, date=EXCLUDED.date, budget_month=EXCLUDED.budget_month, merchant=EXCLUDED.merchant, amount=EXCLUDED.amount, notes=EXCLUDED.notes, recurring=EXCLUDED.recurring, recurring_interval=EXCLUDED.recurring_interval, user_id=EXCLUDED.user_id, is_income=EXCLUDED.is_income, account_number=EXCLUDED.account_number, account_source=EXCLUDED.account_source, posted_date=EXCLUDED.posted_date, imported_merchant=EXCLUDED.imported_merchant, status=EXCLUDED.status, check_number=EXCLUDED.check_number, deleted=EXCLUDED.deleted, entity_id=EXCLUDED.entity_id, updated_at=now();";

        var batch = new NpgsqlBatch(conn) { Transaction = dbTx };

        foreach (var tx in transactions)
        {
            if (string.IsNullOrEmpty(tx.Id))
                tx.Id = Guid.NewGuid().ToString();

            var txCmd = new NpgsqlBatchCommand(sql);
            BindTransactionParameters(txCmd.Parameters, budgetId, tx);
            batch.BatchCommands.Add(txCmd);

            // Ensure at least one split row exists. If none provided, create a default
            // split using either 'Income' or 'Uncategorized' for the full amount.
            var cats = tx.Categories;
            if (cats == null || cats.Count == 0)
            {
                cats = new List<TransactionCategory>
                {
                    new TransactionCategory
                    {
                        Category = tx.IsIncome ? "Income" : "Uncategorized",
                        Amount = tx.Amount
                    }
                };
            }

            AddCategoryCommands(batch, tx.Id, cats);
        }

        await batch.ExecuteNonQueryAsync();
        await dbTx.CommitAsync();
    }

    public async Task<string> SaveImportedTransactions(string userId, ImportedTransactionDoc doc)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        await using var dbTx = await conn.BeginTransactionAsync();

        var docId = Guid.TryParse(doc.Id, out var parsed) ? parsed : Guid.NewGuid();
        doc.Id = docId.ToString();
        var fid = Guid.Parse(doc.FamilyId);

        const string sqlDoc = "INSERT INTO imported_transaction_docs (id, family_id, user_id, created_at) VALUES (@id,@fid,@uid, now())";
        await using var docCmd = new NpgsqlCommand(sqlDoc, conn, dbTx);
        docCmd.Parameters.AddWithValue("id", docId);
        docCmd.Parameters.AddWithValue("fid", fid);
        docCmd.Parameters.AddWithValue("uid", userId);
        await docCmd.ExecuteNonQueryAsync();

        const string sqlTx = @"INSERT INTO imported_transactions (id, document_id, account_id, account_number, account_source, payee, posted_date, amount, status, debit_amount, credit_amount, check_number, deleted, matched, ignored)
                                 VALUES (@id,@doc,@account_id,@acct_num,@acct_src,@payee,@posted,@amount,@status,@debit,@credit,@check,@deleted,@matched,@ignored)";

        var batch = new NpgsqlBatch(conn) { Transaction = dbTx };

        foreach (var tx in doc.importedTransactions)
        {
            if (string.IsNullOrEmpty(tx.Id))
                tx.Id = Guid.NewGuid().ToString();

            var txCmd = new NpgsqlBatchCommand(sqlTx);
            txCmd.Parameters.AddWithValue("id", tx.Id);
            txCmd.Parameters.AddWithValue("doc", docId);
            txCmd.Parameters.AddWithValue("account_id", Guid.Parse(tx.AccountId));
            txCmd.Parameters.AddWithValue("acct_num", (object?)tx.AccountNumber ?? DBNull.Value);
            txCmd.Parameters.AddWithValue("acct_src", (object?)tx.AccountSource ?? DBNull.Value);
            txCmd.Parameters.AddWithValue("payee", (object?)tx.Payee ?? DBNull.Value);
            txCmd.Parameters.AddWithValue("posted", (object?)ParseDate(tx.PostedDate) ?? DBNull.Value);
            txCmd.Parameters.AddWithValue("amount", (object?)tx.Amount ?? DBNull.Value);
            txCmd.Parameters.AddWithValue("status", (object?)tx.Status ?? DBNull.Value);
            txCmd.Parameters.AddWithValue("debit", (object?)tx.DebitAmount ?? DBNull.Value);
            txCmd.Parameters.AddWithValue("credit", (object?)tx.CreditAmount ?? DBNull.Value);
            txCmd.Parameters.AddWithValue("check", (object?)tx.CheckNumber ?? DBNull.Value);
            txCmd.Parameters.AddWithValue("deleted", tx.Deleted.HasValue ? (object)tx.Deleted.Value : DBNull.Value);
            txCmd.Parameters.AddWithValue("matched", tx.Matched);
            txCmd.Parameters.AddWithValue("ignored", tx.Ignored);
            batch.BatchCommands.Add(txCmd);
        }

        if (batch.BatchCommands.Count > 0)
            await batch.ExecuteNonQueryAsync();

        await dbTx.CommitAsync();
        return doc.Id;
    }

    public async Task UpdateImportedTransaction(string docId, string transactionId, bool? matched, bool? ignored, bool? deleted)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        var updates = new List<string>();
        var cmd = new NpgsqlCommand();
        cmd.Connection = conn;
        if (matched.HasValue) { updates.Add("matched=@matched"); cmd.Parameters.AddWithValue("matched", matched.Value); }
        if (ignored.HasValue) { updates.Add("ignored=@ignored"); cmd.Parameters.AddWithValue("ignored", ignored.Value); }
        if (deleted.HasValue) { updates.Add("deleted=@deleted"); cmd.Parameters.AddWithValue("deleted", deleted.Value); }
        if (updates.Count == 0) return;
        cmd.CommandText = $"UPDATE imported_transactions SET {string.Join(",", updates)} WHERE document_id=@doc AND id=@id";
        cmd.Parameters.AddWithValue("doc", Guid.Parse(docId));
        cmd.Parameters.AddWithValue("id", transactionId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<ImportedTransactionDoc>> GetImportedTransactions(string userId)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = @"SELECT d.id, d.family_id, d.user_id, t.id, t.account_id, t.account_number, t.account_source, t.payee, t.posted_date, t.amount, t.status, t.debit_amount, t.credit_amount, t.check_number, t.deleted, t.matched, t.ignored
                              FROM imported_transaction_docs d
                              LEFT JOIN imported_transactions t ON d.id = t.document_id
                              WHERE d.user_id=@uid
                              ORDER BY t.posted_date DESC";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);
        await using var reader = await cmd.ExecuteReaderAsync();
        var map = new Dictionary<Guid, ImportedTransactionDoc>();
        while (await reader.ReadAsync())
        {
            var docId = reader.GetGuid(0);
            if (!map.TryGetValue(docId, out var doc))
            {
                doc = new ImportedTransactionDoc
                {
                    Id = docId.ToString(),
                    FamilyId = reader.GetGuid(1).ToString(),
                    UserId = reader.GetString(2),
                    importedTransactions = Array.Empty<ImportedTransaction>()
                };
                map[docId] = doc;
            }

            if (!reader.IsDBNull(3))
            {
                var list = doc.importedTransactions.ToList();
                list.Add(new ImportedTransaction
                {
                    Id = reader.GetString(3),
                    AccountId = reader.IsDBNull(4) ? string.Empty : reader.GetGuid(4).ToString(),
                    AccountNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AccountSource = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Payee = reader.IsDBNull(7) ? null : reader.GetString(7),
                    PostedDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8).ToString("yyyy-MM-dd"),
                    Amount = reader.IsDBNull(9) ? null : (double?)reader.GetDecimal(9),
                    Status = reader.IsDBNull(10) ? null : reader.GetString(10),
                    DebitAmount = reader.IsDBNull(11) ? null : (double?)reader.GetDecimal(11),
                    CreditAmount = reader.IsDBNull(12) ? null : (double?)reader.GetDecimal(12),
                    CheckNumber = reader.IsDBNull(13) ? null : reader.GetString(13),
                    Deleted = reader.IsDBNull(14) ? (bool?)null : reader.GetBoolean(14),
                    Matched = reader.IsDBNull(15) ? false : reader.GetBoolean(15),
                    Ignored = reader.IsDBNull(16) ? false : reader.GetBoolean(16)
                });
                doc.importedTransactions = list.ToArray();
            }
        }
        return map.Values.ToList();
    }

    public async Task DeleteImportedTransactionDoc(string id)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "DELETE FROM imported_transactions WHERE document_id=@id; DELETE FROM imported_transaction_docs WHERE id=@id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.Parse(id));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<ImportedTransaction>> GetImportedTransactionsByAccountId(string accountId, int offset = 0, int limit = 100)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "SELECT id, account_id, account_number, account_source, payee, posted_date, amount, status, debit_amount, credit_amount, check_number, deleted, matched, ignored FROM imported_transactions WHERE account_id=@aid ORDER BY posted_date DESC LIMIT @limit OFFSET @offset";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("aid", Guid.Parse(accountId));
        cmd.Parameters.AddWithValue("limit", limit);
        cmd.Parameters.AddWithValue("offset", offset);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<ImportedTransaction>();
        while (await reader.ReadAsync())
        {
            list.Add(new ImportedTransaction
            {
                Id = reader.GetString(0),
                AccountId = reader.GetGuid(1).ToString(),
                AccountNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                AccountSource = reader.IsDBNull(3) ? null : reader.GetString(3),
                Payee = reader.IsDBNull(4) ? null : reader.GetString(4),
                PostedDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-dd"),
                Amount = reader.IsDBNull(6) ? null : (double?)reader.GetDecimal(6),
                Status = reader.IsDBNull(7) ? null : reader.GetString(7),
                DebitAmount = reader.IsDBNull(8) ? null : (double?)reader.GetDecimal(8),
                CreditAmount = reader.IsDBNull(9) ? null : (double?)reader.GetDecimal(9),
                CheckNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                Deleted = reader.IsDBNull(11) ? (bool?)null : reader.GetBoolean(11),
                Matched = reader.IsDBNull(12) ? false : reader.GetBoolean(12),
                Ignored = reader.IsDBNull(13) ? false : reader.GetBoolean(13)
            });
        }
        return list;
    }

    public async Task BatchUpdateImportedTransactions(List<ImportedTransaction> transactions)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "UPDATE imported_transactions SET account_number=@acct_num, account_source=@acct_src WHERE id=@id";
        foreach (var tx in transactions)
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("acct_num", (object?)tx.AccountNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("acct_src", (object?)tx.AccountSource ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", tx.Id!);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<TransactionWithBudgetId>> GetBudgetTransactionsMatchedToImported(string accountId, string userId)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sqlAccount = "SELECT account_number FROM accounts WHERE id=@id";
        await using var accCmd = new NpgsqlCommand(sqlAccount, conn);
        accCmd.Parameters.AddWithValue("id", Guid.Parse(accountId));
        var acctNumObj = await accCmd.ExecuteScalarAsync();
        if (acctNumObj is not string acctNum)
            return new List<TransactionWithBudgetId>();

        const string sql = @"SELECT budget_id, id, date, budget_month, merchant, amount, notes, recurring, recurring_interval, user_id, is_income, account_number, account_source, posted_date, imported_merchant, status, check_number, deleted, entity_id
                             FROM transactions WHERE account_number=@acct";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("acct", acctNum);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<TransactionWithBudgetId>();
        while (await reader.ReadAsync())
        {
            var tx = new Transaction
            {
                Id = reader.GetString(1),
                BudgetId = reader.GetString(0),
                Date = reader.IsDBNull(2) ? null : reader.GetDateTime(2).ToString("yyyy-MM-dd"),
                BudgetMonth = reader.IsDBNull(3) ? null : reader.GetString(3),
                Merchant = reader.IsDBNull(4) ? null : reader.GetString(4),
                Amount = (double)reader.GetDecimal(5),
                Notes = reader.IsDBNull(6) ? null : reader.GetString(6),
                Recurring = reader.GetBoolean(7),
                RecurringInterval = reader.IsDBNull(8) ? null : reader.GetString(8),
                UserId = reader.IsDBNull(9) ? null : reader.GetString(9),
                IsIncome = reader.GetBoolean(10),
                AccountNumber = reader.IsDBNull(11) ? null : reader.GetString(11),
                AccountSource = reader.IsDBNull(12) ? null : reader.GetString(12),
                PostedDate = reader.IsDBNull(13) ? null : reader.GetDateTime(13).ToString("yyyy-MM-dd"),
                ImportedMerchant = reader.IsDBNull(14) ? null : reader.GetString(14),
                Status = reader.IsDBNull(15) ? null : reader.GetString(15),
                CheckNumber = reader.IsDBNull(16) ? null : reader.GetString(16),
                Deleted = reader.IsDBNull(17) ? (bool?)null : reader.GetBoolean(17),
                EntityId = reader.IsDBNull(18) ? null : reader.GetGuid(18).ToString()
            };
            list.Add(new TransactionWithBudgetId { BudgetId = reader.GetString(0), Transaction = tx });
        }
        return list;
    }

    public async Task BatchUpdateBudgetTransactions(List<TransactionWithBudgetId> transactions, string userId, string userEmail)
    {
        foreach (var item in transactions)
        {
            var tx = item.Transaction;
            if (string.IsNullOrEmpty(tx.Id)) tx.Id = Guid.NewGuid().ToString();
            await SaveTransaction(item.BudgetId, tx, userId, userEmail);
            if (!string.IsNullOrEmpty(item.OldId) && item.OldId != tx.Id)
            {
                await DeleteTransaction(item.BudgetId, item.OldId, userId, userEmail);
            }
        }
    }

    public async Task BatchReconcileTransactions(string budgetId, List<ReconcileRequest> reconciliations, string userId, string userEmail)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        foreach (var rec in reconciliations)
        {
            const string sql = "UPDATE imported_transactions SET matched=@match, ignored=@ignore WHERE id=@id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("match", rec.Match);
            cmd.Parameters.AddWithValue("ignore", rec.Ignore);
            cmd.Parameters.AddWithValue("id", rec.ImportedTransactionId);
            await cmd.ExecuteNonQueryAsync();
        }
        await LogEdit(conn, budgetId, userId, userEmail, "batch-reconcile");
    }
}
