using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FamilyBudgetApi.Services;

public class StatementService
{
    private readonly SupabaseDbService _db;
    private readonly ILogger<StatementService> _logger;

    public StatementService(SupabaseDbService db, ILogger<StatementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Statement>> GetStatements(string familyId, string accountNumber)
    {
        await using var conn = await _db.GetOpenConnectionAsync();

        var account = await GetAccountOrThrow(conn, familyId, accountNumber);

        const string sql = @"SELECT id::text,
                                    account_id::text,
                                    start_date,
                                    end_date,
                                    starting_balance,
                                    ending_balance,
                                    reconciled
                             FROM account_statements
                             WHERE account_id=@aid
                             ORDER BY start_date";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("aid", account.AccountId.ToString());
        await using var reader = await cmd.ExecuteReaderAsync();

        var results = new List<Statement>();
        while (await reader.ReadAsync())
        {
            results.Add(new Statement
            {
                Id = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                AccountNumber = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                StartDate = reader.IsDBNull(2) ? string.Empty : reader.GetDateTime(2).ToString("yyyy-MM-dd"),
                EndDate = reader.IsDBNull(3) ? string.Empty : reader.GetDateTime(3).ToString("yyyy-MM-dd"),
                StartingBalance = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                EndingBalance = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                Reconciled = !reader.IsDBNull(6) && reader.GetBoolean(6)
            });
        }

        return results;
    }

    public async Task<Statement> SaveStatement(
        string familyId,
        string accountNumber,
        Statement statement,
        List<(string budgetId, string transactionId)> _txRefs,
        string userId,
        string userEmail)
    {
        _logger.LogInformation("Saving statement {StatementId} for family {FamilyId} account {Account} by {UserId}/{UserEmail}", statement.Id, familyId, accountNumber, userId, userEmail);

        await using var conn = await _db.GetOpenConnectionAsync();
        var account = await GetAccountOrThrow(conn, familyId, accountNumber);
        var (startDate, endDate) = StatementRules.ParseDateWindow(statement.StartDate, statement.EndDate);

        if (statement.Reconciled)
            throw new InvalidOperationException("Use finalize to reconcile a statement.");

        await using var tx = await conn.BeginTransactionAsync();

        var existing = await GetStatementById(conn, tx, account.AccountId, statement.Id);
        if (existing is not null)
        {
            if (existing.Value.Reconciled)
                throw new InvalidOperationException("Reconciled statements are locked. Unreconcile before editing.");

            const string updateSql = @"UPDATE account_statements
                                       SET start_date=@startDate,
                                           end_date=@endDate,
                                           starting_balance=@startingBalance,
                                           ending_balance=@endingBalance,
                                           reconciled=false
                                       WHERE id::text=@sid AND account_id=@aid";
            await using var update = new NpgsqlCommand(updateSql, conn, tx);
            update.Parameters.AddWithValue("sid", statement.Id);
            update.Parameters.AddWithValue("aid", account.AccountId.ToString());
            update.Parameters.AddWithValue("startDate", startDate);
            update.Parameters.AddWithValue("endDate", endDate);
            update.Parameters.AddWithValue("startingBalance", statement.StartingBalance);
            update.Parameters.AddWithValue("endingBalance", statement.EndingBalance);
            await update.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return new Statement
            {
                Id = statement.Id,
                AccountNumber = account.AccountId.ToString(),
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                StartingBalance = statement.StartingBalance,
                EndingBalance = statement.EndingBalance,
                Reconciled = false
            };
        }

        var resolvedId = await InsertStatement(conn, tx, account.AccountId, statement, startDate, endDate);

        await tx.CommitAsync();

        return new Statement
        {
            Id = resolvedId,
            AccountNumber = account.AccountId.ToString(),
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            StartingBalance = statement.StartingBalance,
            EndingBalance = statement.EndingBalance,
            Reconciled = false
        };
    }

    public async Task FinalizeStatement(
        string familyId,
        string accountNumber,
        StatementFinalizeRequest request,
        string userId,
        string userEmail)
    {
        _logger.LogInformation("Finalizing statement for family {FamilyId} account {Account} by {UserId}/{UserEmail}", familyId, accountNumber, userId, userEmail);

        await using var conn = await _db.GetOpenConnectionAsync();
        var account = await GetAccountOrThrow(conn, familyId, accountNumber);
        var (startDate, endDate) = StatementRules.ParseDateWindow(request.StartDate, request.EndDate);

        var importedIds = request.ImportedTransactionIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList()
            ?? new List<string>();
        if (importedIds.Count == 0 && request.MatchedTransactionIds is not null)
        {
            importedIds = request.MatchedTransactionIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
        }

        var budgetRefs = request.BudgetTransactionRefs?.Where(r => !string.IsNullOrWhiteSpace(r.BudgetId) && !string.IsNullOrWhiteSpace(r.TransactionId))
            .Select(r => (budgetId: r.BudgetId.Trim(), transactionId: r.TransactionId.Trim()))
            .Distinct()
            .ToList() ?? new List<(string budgetId, string transactionId)>();

        if (importedIds.Count == 0 && budgetRefs.Count == 0)
            throw new ArgumentException("At least one imported or budget transaction must be selected.");

        await using var tx = await conn.BeginTransactionAsync();

        var statement = await GetOrCreateStatementForFinalize(conn, tx, account.AccountId, request, startDate, endDate);
        if (statement.Reconciled)
            throw new InvalidOperationException("Statement is already reconciled.");

        var selectedTotal = 0d;
        selectedTotal += await SumAndValidateImportedTransactions(conn, tx, account.AccountId, importedIds);
        selectedTotal += await SumAndValidateBudgetTransactions(conn, tx, account.FamilyId, account.AccountId, startDate, endDate, budgetRefs);

        var computedDelta = StatementRules.CalculateDelta(request.BeginningBalance, selectedTotal, request.EndingBalance);
        if (!StatementRules.IsZeroDelta(computedDelta))
            throw new ArgumentException($"Cannot finalize statement because delta is not zero. Delta: {computedDelta:F2}");

        if (importedIds.Count > 0)
        {
            const string updateImported = @"UPDATE imported_transactions
                                           SET status='R'
                                           WHERE id::text = ANY(@ids)";
            await using var importedCmd = new NpgsqlCommand(updateImported, conn, tx);
            importedCmd.Parameters.AddWithValue("ids", importedIds.ToArray());
            var updatedRows = await importedCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("FinalizeStatement: updated {UpdatedRows} of {RequestedRows} imported transactions to status=R", updatedRows, importedIds.Count);
            if (updatedRows != importedIds.Count)
                _logger.LogWarning("FinalizeStatement: row count mismatch — {UpdatedRows} updated, {RequestedRows} requested. IDs: {Ids}", updatedRows, importedIds.Count, string.Join(", ", importedIds));
        }

        if (budgetRefs.Count > 0)
        {
            const string updateBudget = @"UPDATE transactions t
                                         SET status='R'
                                         FROM UNNEST(@budgetIds, @transactionIds) AS p(budget_id, transaction_id)
                                         WHERE t.budget_id = p.budget_id AND t.id = p.transaction_id";
            await using var budgetCmd = new NpgsqlCommand(updateBudget, conn, tx);
            budgetCmd.Parameters.AddWithValue("budgetIds", budgetRefs.Select(r => r.budgetId).ToArray());
            budgetCmd.Parameters.AddWithValue("transactionIds", budgetRefs.Select(r => r.transactionId).ToArray());
            await budgetCmd.ExecuteNonQueryAsync();
        }

        const string reconcileStatementSql = @"UPDATE account_statements
                                               SET start_date=@startDate,
                                                   end_date=@endDate,
                                                   starting_balance=@startingBalance,
                                                   ending_balance=@endingBalance,
                                                   reconciled=true
                                               WHERE id::text=@sid AND account_id=@aid";
        await using (var reconcileCmd = new NpgsqlCommand(reconcileStatementSql, conn, tx))
        {
            reconcileCmd.Parameters.AddWithValue("sid", statement.Id);
            reconcileCmd.Parameters.AddWithValue("aid", account.AccountId.ToString());
            reconcileCmd.Parameters.AddWithValue("startDate", startDate);
            reconcileCmd.Parameters.AddWithValue("endDate", endDate);
            reconcileCmd.Parameters.AddWithValue("startingBalance", request.BeginningBalance);
            reconcileCmd.Parameters.AddWithValue("endingBalance", request.EndingBalance);
            await reconcileCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    public async Task DeleteStatement(
        string familyId,
        string accountNumber,
        string statementId,
        List<(string budgetId, string transactionId)> _txRefs,
        string userId,
        string userEmail)
    {
        _logger.LogInformation("DeleteStatement called for family {FamilyId} account {Account} statement {StatementId} by {UserId}/{UserEmail}", familyId, accountNumber, statementId, userId, userEmail);

        await using var conn = await _db.GetOpenConnectionAsync();
        var account = await GetAccountOrThrow(conn, familyId, accountNumber);

        await using var tx = await conn.BeginTransactionAsync();

        var statement = await GetStatementById(conn, tx, account.AccountId, statementId)
            ?? throw new ArgumentException("Statement not found.");

        if (statement.Reconciled)
            throw new InvalidOperationException("Reconciled statements are locked. Unreconcile before deleting.");

        const string sql = "DELETE FROM account_statements WHERE id::text=@sid AND account_id=@aid";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("sid", statementId);
        cmd.Parameters.AddWithValue("aid", account.AccountId.ToString());
        await cmd.ExecuteNonQueryAsync();

        await tx.CommitAsync();
    }

    public async Task UnreconcileStatement(
        string familyId,
        string accountNumber,
        string statementId,
        List<(string budgetId, string transactionId)> _txRefs,
        string userId,
        string userEmail)
    {
        _logger.LogInformation("UnreconcileStatement called for family {FamilyId} account {Account} statement {StatementId} by {UserId}/{UserEmail}", familyId, accountNumber, statementId, userId, userEmail);

        await using var conn = await _db.GetOpenConnectionAsync();
        var account = await GetAccountOrThrow(conn, familyId, accountNumber);

        await using var tx = await conn.BeginTransactionAsync();

        var statement = await GetStatementById(conn, tx, account.AccountId, statementId)
            ?? throw new ArgumentException("Statement not found.");

        if (!statement.Reconciled)
            throw new InvalidOperationException("Statement is not reconciled.");

        const string updateBudgetSql = @"UPDATE transactions
                                         SET status='C'
                                         WHERE account_number=@aidText
                                           AND COALESCE(transaction_date::date, posted_date::date, date::date) >= @startDate
                                           AND COALESCE(transaction_date::date, posted_date::date, date::date) <= @endDate
                                           AND status='R'
                                           AND (deleted IS NULL OR deleted=false)";
        await using (var updateBudget = new NpgsqlCommand(updateBudgetSql, conn, tx))
        {
            updateBudget.Parameters.AddWithValue("aidText", account.AccountId.ToString());
            updateBudget.Parameters.AddWithValue("startDate", statement.StartDate);
            updateBudget.Parameters.AddWithValue("endDate", statement.EndDate);
            await updateBudget.ExecuteNonQueryAsync();
        }

        const string updateImportedSql = @"UPDATE imported_transactions
                                           SET status='C'
                                           WHERE account_id=@aid
                                             AND COALESCE(transaction_date::date, posted_date::date) >= @startDate
                                             AND COALESCE(transaction_date::date, posted_date::date) <= @endDate
                                             AND status='R'
                                             AND (deleted IS NULL OR deleted=false)";
        await using (var updateImported = new NpgsqlCommand(updateImportedSql, conn, tx))
        {
            updateImported.Parameters.AddWithValue("aid", account.AccountId); // imported_transactions.account_id is uuid
            updateImported.Parameters.AddWithValue("startDate", statement.StartDate);
            updateImported.Parameters.AddWithValue("endDate", statement.EndDate);
            await updateImported.ExecuteNonQueryAsync();
        }

        const string updateStatementSql = @"UPDATE account_statements
                                            SET reconciled=false
                                            WHERE id::text=@sid AND account_id=@aid";
        await using (var updateStatement = new NpgsqlCommand(updateStatementSql, conn, tx))
        {
            updateStatement.Parameters.AddWithValue("sid", statementId);
            updateStatement.Parameters.AddWithValue("aid", account.AccountId.ToString());
            await updateStatement.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    private async Task<(Guid FamilyId, Guid AccountId)> GetAccountOrThrow(NpgsqlConnection conn, string familyId, string accountNumber)
    {
        if (!Guid.TryParse(familyId, out var fid))
            throw new ArgumentException("Invalid family id.");

        if (!Guid.TryParse(accountNumber, out var aid))
            throw new ArgumentException("Invalid account id.");

        const string accountCheck = "SELECT 1 FROM accounts WHERE family_id=@fid AND id::text=@aid";
        await using var check = new NpgsqlCommand(accountCheck, conn);
        check.Parameters.AddWithValue("fid", fid.ToString());
        check.Parameters.AddWithValue("aid", aid.ToString());
        var exists = await check.ExecuteScalarAsync();
        if (exists is null)
            throw new ArgumentException("Account not found in family.");

        return (fid, aid);
    }

    private async Task<StatementRow?> GetStatementById(NpgsqlConnection conn, NpgsqlTransaction tx, Guid accountId, string statementId)
    {
        const string sql = @"SELECT id::text, start_date, end_date, starting_balance, ending_balance, reconciled
                             FROM account_statements
                             WHERE account_id=@aid AND id::text=@sid
                             LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("aid", accountId.ToString());
        cmd.Parameters.AddWithValue("sid", statementId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new StatementRow
        {
            Id = reader.GetString(0),
            StartDate = reader.GetDateTime(1).Date,
            EndDate = reader.GetDateTime(2).Date,
            StartingBalance = reader.GetDouble(3),
            EndingBalance = reader.GetDouble(4),
            Reconciled = reader.GetBoolean(5)
        };
    }

    private async Task<string> InsertStatement(NpgsqlConnection conn, NpgsqlTransaction tx, Guid accountId, Statement statement, DateTime startDate, DateTime endDate)
    {
        if (Guid.TryParse(statement.Id, out var statementGuid))
        {
            const string insertWithId = @"INSERT INTO account_statements (id, account_id, start_date, end_date, starting_balance, ending_balance, reconciled)
                                          VALUES (@id, @aid, @startDate, @endDate, @startingBalance, @endingBalance, false)
                                          RETURNING id::text";
            await using var cmd = new NpgsqlCommand(insertWithId, conn, tx);
            cmd.Parameters.AddWithValue("id", statementGuid);
            cmd.Parameters.AddWithValue("aid", accountId.ToString());
            cmd.Parameters.AddWithValue("startDate", startDate);
            cmd.Parameters.AddWithValue("endDate", endDate);
            cmd.Parameters.AddWithValue("startingBalance", statement.StartingBalance);
            cmd.Parameters.AddWithValue("endingBalance", statement.EndingBalance);

            try
            {
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? statement.Id;
            }
            catch (PostgresException ex) when (ex.SqlState == "42804")
            {
                _logger.LogWarning(ex, "Statement id type mismatch for explicit id insert. Falling back to server-generated statement id.");
            }
        }

        const string insertAutoId = @"INSERT INTO account_statements (account_id, start_date, end_date, starting_balance, ending_balance, reconciled)
                                      VALUES (@aid, @startDate, @endDate, @startingBalance, @endingBalance, false)
                                      RETURNING id::text";
        await using var fallback = new NpgsqlCommand(insertAutoId, conn, tx);
        fallback.Parameters.AddWithValue("aid", accountId.ToString());
        fallback.Parameters.AddWithValue("startDate", startDate);
        fallback.Parameters.AddWithValue("endDate", endDate);
        fallback.Parameters.AddWithValue("startingBalance", statement.StartingBalance);
        fallback.Parameters.AddWithValue("endingBalance", statement.EndingBalance);
        var fallbackResult = await fallback.ExecuteScalarAsync();
        return fallbackResult?.ToString() ?? string.Empty;
    }

    private async Task<StatementRow> GetOrCreateStatementForFinalize(NpgsqlConnection conn, NpgsqlTransaction tx, Guid accountId, StatementFinalizeRequest request, DateTime startDate, DateTime endDate)
    {
        StatementRow? statement = null;

        if (!string.IsNullOrWhiteSpace(request.StatementId))
        {
            statement = await GetStatementById(conn, tx, accountId, request.StatementId);
            if (statement is null)
                throw new ArgumentException("Statement not found.");
        }
        else
        {
            const string byWindowSql = @"SELECT id::text, start_date, end_date, starting_balance, ending_balance, reconciled
                                         FROM account_statements
                                         WHERE account_id=@aid AND start_date=@startDate AND end_date=@endDate
                                         ORDER BY id DESC
                                         LIMIT 1";
            await using var byWindow = new NpgsqlCommand(byWindowSql, conn, tx);
            byWindow.Parameters.AddWithValue("aid", accountId.ToString());
            byWindow.Parameters.AddWithValue("startDate", startDate);
            byWindow.Parameters.AddWithValue("endDate", endDate);
            await using var reader = await byWindow.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                statement = new StatementRow
                {
                    Id = reader.GetString(0),
                    StartDate = reader.GetDateTime(1).Date,
                    EndDate = reader.GetDateTime(2).Date,
                    StartingBalance = reader.GetDouble(3),
                    EndingBalance = reader.GetDouble(4),
                    Reconciled = reader.GetBoolean(5)
                };
            }
        }

        if (statement is not null)
            return statement.Value;

        const string insertAutoId = @"INSERT INTO account_statements (account_id, start_date, end_date, starting_balance, ending_balance, reconciled)
                                      VALUES (@aid, @startDate, @endDate, @startingBalance, @endingBalance, false)
                                      RETURNING id::text";
        await using var insert = new NpgsqlCommand(insertAutoId, conn, tx);
        insert.Parameters.AddWithValue("aid", accountId.ToString());
        insert.Parameters.AddWithValue("startDate", startDate);
        insert.Parameters.AddWithValue("endDate", endDate);
        insert.Parameters.AddWithValue("startingBalance", request.BeginningBalance);
        insert.Parameters.AddWithValue("endingBalance", request.EndingBalance);
        var createdId = (await insert.ExecuteScalarAsync())?.ToString() ?? throw new InvalidOperationException("Unable to create statement.");

        return new StatementRow
        {
            Id = createdId,
            StartDate = startDate,
            EndDate = endDate,
            StartingBalance = request.BeginningBalance,
            EndingBalance = request.EndingBalance,
            Reconciled = false
        };
    }

    private static async Task<double> SumAndValidateImportedTransactions(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid accountId,
        List<string> importedIds)
    {
        if (importedIds.Count == 0) return 0;

        // Date window is intentionally not enforced here — the user explicitly selected these
        // transactions and verified the delta is zero. Dates are derived from the selection,
        // not used to gate which transactions are valid.
        const string sql = @"SELECT id::text,
                                    (CASE WHEN COALESCE(debit_amount, 0) > 0 THEN -COALESCE(debit_amount,0)
                                          ELSE COALESCE(credit_amount,0) END) AS signed_amount
                             FROM imported_transactions
                             WHERE id::text = ANY(@ids)
                               AND account_id=@aid
                               AND (status IN ('C','M','R') OR ignored=true)
                               AND (deleted IS NULL OR deleted=false)";
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("ids", importedIds.ToArray());
        cmd.Parameters.AddWithValue("aid", accountId); // imported_transactions.account_id is uuid

        var found = new HashSet<string>();
        var total = 0d;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            found.Add(reader.GetString(0));
            total += reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
        }

        if (found.Count != importedIds.Count)
            throw new ArgumentException("One or more selected transactions were not found or are not in a reconcilable status (must be cleared or matched).");

        return total;
    }

    private static async Task<double> SumAndValidateBudgetTransactions(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid familyId,
        Guid accountId,
        DateTime startDate,
        DateTime endDate,
        List<(string budgetId, string transactionId)> budgetRefs)
    {
        if (budgetRefs.Count == 0) return 0;

        const string sql = @"SELECT t.id,
                                    t.budget_id,
                                    (CASE WHEN COALESCE(t.is_income, false) THEN COALESCE(t.amount,0)
                                          ELSE -COALESCE(t.amount,0) END) AS signed_amount
                             FROM UNNEST(@budgetIds, @transactionIds) AS p(budget_id, transaction_id)
                             JOIN transactions t ON t.id = p.transaction_id AND t.budget_id = p.budget_id
                             JOIN budgets b ON b.id = t.budget_id
                             WHERE b.family_id=@fid
                               AND t.account_number=@aidText
                               AND COALESCE(t.transaction_date::date, t.posted_date::date, t.date::date) >= @startDate
                               AND COALESCE(t.transaction_date::date, t.posted_date::date, t.date::date) <= @endDate
                               AND t.status IN ('C','R')
                               AND (t.deleted IS NULL OR t.deleted=false)";

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("budgetIds", budgetRefs.Select(r => r.budgetId).ToArray());
        cmd.Parameters.AddWithValue("transactionIds", budgetRefs.Select(r => r.transactionId).ToArray());
        cmd.Parameters.AddWithValue("fid", familyId);
        cmd.Parameters.AddWithValue("aidText", accountId.ToString());
        cmd.Parameters.AddWithValue("startDate", startDate);
        cmd.Parameters.AddWithValue("endDate", endDate);

        var found = new HashSet<string>();
        var total = 0d;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var key = $"{reader.GetString(1)}::{reader.GetString(0)}";
            found.Add(key);
            total += reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
        }

        if (found.Count != budgetRefs.Count)
            throw new ArgumentException("One or more budget transactions are invalid for this statement (must be in account/date range and cleared or reconciled).");

        return total;
    }

    private readonly record struct StatementRow
    {
        public required string Id { get; init; }
        public required DateTime StartDate { get; init; }
        public required DateTime EndDate { get; init; }
        public required double StartingBalance { get; init; }
        public required double EndingBalance { get; init; }
        public required bool Reconciled { get; init; }
    }
}

public class StatementFinalizeRequest
{
    public string? StatementId { get; set; }
    public required string StartDate { get; set; }
    public required string EndDate { get; set; }
    public double BeginningBalance { get; set; }
    public double EndingBalance { get; set; }
    public List<string>? ImportedTransactionIds { get; set; }
    // Backward-compatible alias for imported transaction IDs from older clients.
    public List<string>? MatchedTransactionIds { get; set; }
    public List<StatementBudgetTransactionRef>? BudgetTransactionRefs { get; set; }
}

public class StatementBudgetTransactionRef
{
    public required string BudgetId { get; set; }
    public required string TransactionId { get; set; }
}
