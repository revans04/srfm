using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
    private bool? _hasBudgetEditHistoryTable;
    private readonly SemaphoreSlim _editHistoryCheckLock = new(1, 1);

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

        var sql = "SELECT b.id, b.family_id, b.entity_id, b.month, b.label, b.income_target, b.original_budget_id, " +
                  "(SELECT COUNT(*) FROM transactions t WHERE t.budget_id = b.id) AS transaction_count " +
                  "FROM budgets b WHERE b.family_id=@fid";
        if (!string.IsNullOrEmpty(entityId)) sql += " AND b.entity_id=@eid";

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
                TransactionCount = reader.IsDBNull(7) ? 0 : (int)reader.GetInt64(7),
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
        // Determine if the goals_budget_categories table exists so we can
        // gracefully fall back when the migration hasn't run yet.
        var hasGoalTable = false;
        const string checkSql = "SELECT to_regclass('public.goals_budget_categories') IS NOT NULL";
        await using (var checkCmd = new NpgsqlCommand(checkSql, conn))
        {
            var result = await checkCmd.ExecuteScalarAsync();
            if (result is bool b)
                hasGoalTable = b;
        }

        if (includeTransactions)
        {
            // Hide transactions that ONLY touch goal-linked categories — those
            // belong to the Goal panel, not the budget view. A transaction with
            // at least one non-goal-linked split (e.g. a transfer from a goal
            // fund to a regular fund, or a goal-funded expense whose destination
            // is a regular expense category) must still appear here so the
            // non-goal side is visible in budget rollups and category panels.
            //
            // EXISTS-with-LEFT-JOIN-IS-NULL identifies "this transaction has
            // ≥1 split whose category is not in goals_budget_categories." Pure
            // goal contributions/withdrawals (every split is goal-linked) stay
            // hidden from the budget view.
            var sqlTx = hasGoalTable
                ? @"SELECT t.id, t.date, t.budget_month, t.merchant, t.amount, t.notes, t.recurring, t.recurring_interval, t.user_id, t.is_income, t.account_number, t.account_source, t.transaction_date, t.posted_date, t.imported_merchant, t.status, t.check_number, t.deleted, t.entity_id, t.transaction_type
FROM transactions t
WHERE t.budget_id=@id AND t.entity_id=@entity
AND EXISTS (
    SELECT 1 FROM transaction_categories tc
    LEFT JOIN budget_categories bc
      ON bc.budget_id = t.budget_id AND bc.name = tc.category_name
    LEFT JOIN goals_budget_categories gbc
      ON gbc.budget_cat_id = bc.id
    WHERE tc.transaction_id = t.id
      AND gbc.id IS NULL
);" :
                @"SELECT t.id, t.date, t.budget_month, t.merchant, t.amount, t.notes, t.recurring, t.recurring_interval, t.user_id, t.is_income, t.account_number, t.account_source, t.transaction_date, t.posted_date, t.imported_merchant, t.status, t.check_number, t.deleted, t.entity_id, t.transaction_type
FROM transactions t
WHERE t.budget_id=@id AND t.entity_id=@entity";

            await using (var txCmd = new NpgsqlCommand(sqlTx, conn))
            {
                txCmd.Parameters.AddWithValue("id", budget.BudgetId);
                SetOptionalGuid(txCmd.Parameters, "entity", budget.EntityId);
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
                        TransactionDate = txReader.IsDBNull(12) ? null : txReader.GetDateTime(12).ToString("yyyy-MM-dd"),
                        PostedDate = txReader.IsDBNull(13) ? null : txReader.GetDateTime(13).ToString("yyyy-MM-dd"),
                        ImportedMerchant = txReader.IsDBNull(14) ? null : txReader.GetString(14),
                        Status = txReader.IsDBNull(15) ? null : txReader.GetString(15),
                        CheckNumber = txReader.IsDBNull(16) ? null : txReader.GetString(16),
                        Deleted = txReader.IsDBNull(17) ? (bool?)null : txReader.GetBoolean(17),
                        EntityId = txReader.IsDBNull(18) ? null : txReader.GetGuid(18).ToString(),
                        TransactionType = txReader.IsDBNull(19) ? "standard" : txReader.GetString(19)
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

        var sqlCats = hasGoalTable
            ? @"SELECT bc.id, bc.name, bc.target, bc.is_fund, bc.group_id, bg.name AS group_name,
                       bc.sort_order, bc.carryover, bc.favorite, bc.funding_source_category
                  FROM budget_categories bc
                  LEFT JOIN budget_groups bg ON bg.id = bc.group_id
                 WHERE bc.budget_id=@id
                   AND NOT EXISTS (SELECT 1 FROM goals_budget_categories gbc WHERE gbc.budget_cat_id = bc.id)
                 ORDER BY COALESCE(bg.sort_order, 2147483647), bc.sort_order, bc.name"
            : @"SELECT bc.id, bc.name, bc.target, bc.is_fund, bc.group_id, bg.name AS group_name,
                       bc.sort_order, bc.carryover, bc.favorite, bc.funding_source_category
                  FROM budget_categories bc
                  LEFT JOIN budget_groups bg ON bg.id = bc.group_id
                 WHERE bc.budget_id=@id
                 ORDER BY COALESCE(bg.sort_order, 2147483647), bc.sort_order, bc.name";
        await using (var catCmd = new NpgsqlCommand(sqlCats, conn))
        {
            catCmd.Parameters.AddWithValue("id", budget.BudgetId);
            await using var catReader = await catCmd.ExecuteReaderAsync();
            while (await catReader.ReadAsync())
            {
                budget.Categories.Add(new BudgetCategory
                {
                    Id = catReader.IsDBNull(0) ? (long?)null : catReader.GetInt64(0),
                    Name = catReader.IsDBNull(1) ? null : catReader.GetString(1),
                    Target = catReader.IsDBNull(2) ? 0 : (double)catReader.GetDecimal(2),
                    IsFund = catReader.IsDBNull(3) ? false : catReader.GetBoolean(3),
                    GroupId = catReader.IsDBNull(4) ? null : catReader.GetGuid(4).ToString(),
                    GroupName = catReader.IsDBNull(5) ? null : catReader.GetString(5),
                    SortOrder = catReader.IsDBNull(6) ? 0 : catReader.GetInt32(6),
                    Carryover = catReader.IsDBNull(7) ? null : (double?)catReader.GetDecimal(7),
                    Favorite = catReader.IsDBNull(8) ? (bool?)null : catReader.GetBoolean(8),
                    FundingSourceCategory = catReader.IsDBNull(9) ? null : catReader.GetString(9)
                });
            }
        }

        // Hydrate the entity-level group taxonomy snapshot. The frontend
        // uses this to render the group taxonomy without a second round-trip.
        if (!string.IsNullOrWhiteSpace(budget.EntityId) && Guid.TryParse(budget.EntityId, out var entityIdGuid))
        {
            const string sqlGroups = @"SELECT id, name, sort_order, archived, kind, color, icon, collapsed_default
                                       FROM budget_groups
                                       WHERE entity_id=@eid
                                       ORDER BY sort_order, name";
            await using var grpCmd = new NpgsqlCommand(sqlGroups, conn);
            grpCmd.Parameters.AddWithValue("eid", entityIdGuid);
            await using var grpReader = await grpCmd.ExecuteReaderAsync();
            while (await grpReader.ReadAsync())
            {
                budget.Groups.Add(new BudgetGroup
                {
                    Id = grpReader.GetGuid(0).ToString(),
                    EntityId = budget.EntityId!,
                    Name = grpReader.IsDBNull(1) ? string.Empty : grpReader.GetString(1),
                    SortOrder = grpReader.IsDBNull(2) ? 0 : grpReader.GetInt32(2),
                    Archived = !grpReader.IsDBNull(3) && grpReader.GetBoolean(3),
                    Kind = grpReader.IsDBNull(4) ? "expense" : grpReader.GetString(4),
                    Color = grpReader.IsDBNull(5) ? null : grpReader.GetString(5),
                    Icon = grpReader.IsDBNull(6) ? null : grpReader.GetString(6),
                    CollapsedDefault = !grpReader.IsDBNull(7) && grpReader.GetBoolean(7),
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
    public async Task<List<Budget>> GetBudgets(string[] budgetIds)
    {
        if (budgetIds.Length == 0) return new List<Budget>();

        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "SELECT id, family_id, entity_id, month, label, income_target, original_budget_id FROM budgets WHERE id = ANY(@ids)";
        var budgets = new List<Budget>();

        await using (var cmd = new NpgsqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("ids", budgetIds);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                budgets.Add(new Budget
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
                });
            }
        }

        // Limit concurrency to avoid exhausting the DB connection pool
        using var semaphore = new SemaphoreSlim(5);
        await Task.WhenAll(budgets.Select(async budget =>
        {
            await semaphore.WaitAsync();
            try
            {
                await using var detailConn = await _db.GetOpenConnectionAsync();
                await LoadBudgetDetails(detailConn, budget);
            }
            finally
            {
                semaphore.Release();
            }
        }));

        return budgets;
    }

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
    public async Task SaveBudget(string budgetId, Budget budget, string userId, string userEmail, bool skipCarryoverRecalc = false)
    {
        _logger.LogInformation("Saving budget {BudgetId}", budgetId);
        await using var conn = await _db.GetOpenConnectionAsync();
        await using (var dbTx = await conn.BeginTransactionAsync())
        {
            await WriteBudgetAndCategoriesAsync(conn, dbTx, budgetId, budget, _logger);
            await dbTx.CommitAsync();
        }

        if (budget.Transactions != null && budget.Transactions.Count > 0)
        {
            await BatchSaveTransactions(budgetId, budget.Transactions, userId, userEmail, !skipCarryoverRecalc);
        }
        await LogEdit(conn, budgetId, userId, userEmail, "save-budget");
        _logger.LogInformation("Budget {BudgetId} saved with {CatCount} categories and {TxCount} transactions", budgetId, budget.Categories?.Count ?? 0, budget.Transactions?.Count ?? 0);
    }

    /// <summary>
    /// Write the `budgets` row and replace its `budget_categories` rows on an
    /// existing open connection + transaction. Does NOT manage the
    /// transaction lifecycle (caller commits/rolls back), does NOT batch
    /// transactions (they live on a different write path), and does NOT log
    /// edit history. Used by both <see cref="SaveBudget"/> and
    /// <c>OnboardingService.SeedAsync</c> so the seed flow can compose budget
    /// creation with family/entity inserts inside one Postgres transaction
    /// without re-implementing the group-resolution / sort-order logic.
    /// </summary>
    internal static async Task WriteBudgetAndCategoriesAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        string budgetId,
        Budget budget,
        ILogger logger)
    {
        const string sql = @"INSERT INTO budgets (id, family_id, entity_id, month, label, income_target, original_budget_id, created_at, updated_at)
VALUES (@id,@family_id,@entity_id,@month,@label,@income_target,@original_budget_id, now(), now())
ON CONFLICT (id) DO UPDATE SET family_id=EXCLUDED.family_id, entity_id=EXCLUDED.entity_id, month=EXCLUDED.month, label=EXCLUDED.label, income_target=EXCLUDED.income_target, original_budget_id=EXCLUDED.original_budget_id, updated_at=now();";
        await using (var cmd = new NpgsqlCommand(sql, conn, tx))
        {
            cmd.Parameters.AddWithValue("id", budgetId);
            cmd.Parameters.AddWithValue("family_id", Guid.Parse(budget.FamilyId));
            SetOptionalGuid(cmd.Parameters, "entity_id", budget.EntityId);
            cmd.Parameters.AddWithValue("month", budget.Month);
            cmd.Parameters.AddWithValue("label", (object?)budget.Label ?? DBNull.Value);
            cmd.Parameters.AddWithValue("income_target", (decimal)budget.IncomeTarget);
            cmd.Parameters.AddWithValue("original_budget_id", (object?)budget.OriginalBudgetId ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        // Resolve a group_id for every incoming category. Categories may carry
        // either an explicit GroupId (already known) or just a GroupName (newly
        // typed in the UI) — in the latter case we upsert a budget_groups row
        // for the entity and capture its id. Falls back to an "(Ungrouped)"
        // group when neither is provided.
        Guid? entityGuid = null;
        if (!string.IsNullOrWhiteSpace(budget.EntityId) && Guid.TryParse(budget.EntityId, out var parsedEntity))
            entityGuid = parsedEntity;

        var resolvedGroupIds = new Dictionary<int, Guid>();
        if (budget.Categories != null && budget.Categories.Count > 0)
        {
            for (var i = 0; i < budget.Categories.Count; i++)
            {
                var cat = budget.Categories[i];
                Guid? gid = null;
                if (!string.IsNullOrWhiteSpace(cat.GroupId) && Guid.TryParse(cat.GroupId, out var parsedGid))
                    gid = parsedGid;

                if (gid == null && entityGuid.HasValue && !string.IsNullOrWhiteSpace(cat.GroupName))
                {
                    gid = await EnsureGroupAsync(conn, tx, entityGuid.Value, cat.GroupName!.Trim(), InferGroupKind(cat.GroupName!));
                }

                if (gid == null && entityGuid.HasValue)
                {
                    gid = await EnsureGroupAsync(conn, tx, entityGuid.Value, "(Ungrouped)", "expense");
                }

                if (gid.HasValue)
                    resolvedGroupIds[i] = gid.Value;
            }
        }

        // Detect the goals_budget_categories table once so the DELETE below
        // and the EnsureGoalCategoriesForBudget call at the end can both
        // adapt to migration state.
        bool hasGoalTable;
        const string checkGoalTableSql = "SELECT to_regclass('public.goals_budget_categories') IS NOT NULL";
        await using (var checkCmd = new NpgsqlCommand(checkGoalTableSql, conn, tx))
        {
            hasGoalTable = (await checkCmd.ExecuteScalarAsync()) is bool b && b;
        }

        // Replace existing categories and insert current ones, but PRESERVE
        // any goal-linked rows. The FE doesn't include goal-linked categories
        // in its payload because LoadBudgetDetails hides them from the budget
        // view; without this guard, every save would orphan
        // goals_budget_categories and break the goal rollup
        // (`savedToDate` would silently reset to 0).
        var delCatSql = hasGoalTable
            ? @"DELETE FROM budget_categories
                WHERE budget_id=@bid
                  AND id NOT IN (SELECT budget_cat_id FROM goals_budget_categories)"
            : "DELETE FROM budget_categories WHERE budget_id=@bid";
        await using (var delCatCmd = new NpgsqlCommand(delCatSql, conn, tx))
        {
            delCatCmd.Parameters.AddWithValue("bid", budgetId);
            await delCatCmd.ExecuteNonQueryAsync();
        }

        if (budget.Categories != null && budget.Categories.Count > 0)
        {
            // Per-category sort_order is taken from the array index when the
            // payload doesn't explicitly carry one. Within a group, dense
            // 0..N-1 ordering is recomputed on save.
            var perGroupCounter = new Dictionary<Guid, int>();
            const string insCatSql = @"INSERT INTO budget_categories
                (budget_id, name, target, is_fund, group_id, sort_order, carryover, favorite, funding_source_category)
                VALUES (@budget_id, @name, @target, @is_fund, @group_id, @sort_order, @carryover, @favorite, @funding_source_category)";
            for (var i = 0; i < budget.Categories.Count; i++)
            {
                var cat = budget.Categories[i];
                if (!resolvedGroupIds.TryGetValue(i, out var gid))
                {
                    // Skip categories with no resolvable group (no entity_id + no upsert path).
                    logger.LogWarning("Skipping category '{Name}' on budget {BudgetId}: no group_id resolvable", cat.Name, budgetId);
                    continue;
                }

                if (!perGroupCounter.TryGetValue(gid, out var nextOrder))
                    nextOrder = 0;
                var sortOrder = cat.SortOrder > 0 ? cat.SortOrder : nextOrder;
                perGroupCounter[gid] = Math.Max(nextOrder, sortOrder) + 1;

                await using var catCmd = new NpgsqlCommand(insCatSql, conn, tx);
                catCmd.Parameters.AddWithValue("budget_id", budgetId);
                catCmd.Parameters.AddWithValue("name", (object?)cat.Name ?? DBNull.Value);
                catCmd.Parameters.AddWithValue("target", (decimal)cat.Target);
                catCmd.Parameters.AddWithValue("is_fund", cat.IsFund);
                catCmd.Parameters.AddWithValue("group_id", gid);
                catCmd.Parameters.AddWithValue("sort_order", sortOrder);
                catCmd.Parameters.AddWithValue("carryover", cat.Carryover.HasValue ? (object)(decimal)cat.Carryover.Value : DBNull.Value);
                catCmd.Parameters.AddWithValue("favorite", cat.Favorite ?? false);
                catCmd.Parameters.AddWithValue("funding_source_category", (object?)cat.FundingSourceCategory ?? DBNull.Value);
                await catCmd.ExecuteNonQueryAsync();
            }
        }

        // After the FE categories are in place, make sure every active goal
        // for this budget's entity has a budget_categories row + goal link
        // here. Handles two cases without per-call branching:
        //   • The budget pre-dates the goal (goal was created later) — link
        //     gets created on first save of this budget.
        //   • The budget was edited and the bulk DELETE wiped a stale link
        //     (legacy data only — the conditional DELETE above prevents this
        //     going forward; this catches anything left over from before).
        if (hasGoalTable && entityGuid.HasValue)
        {
            await EnsureGoalCategoriesForBudgetAsync(conn, tx, budgetId, entityGuid.Value);
        }
    }

    /// <summary>
    /// Ensure every active (non-archived) goal under <paramref name="entityId"/>
    /// has both a matching <c>budget_categories</c> row in
    /// <paramref name="budgetId"/> and a <c>goals_budget_categories</c> link
    /// pointing at it. Idempotent — existing categories/links are left alone,
    /// missing ones are created in place.
    ///
    /// Called from two places to keep goal↔budget links intact across the
    /// app's lifecycle:
    ///   • <see cref="WriteBudgetAndCategoriesAsync"/> after FE-supplied
    ///     categories have been written, so a brand-new budget (or a re-saved
    ///     budget whose payload omits goal-linked rows because
    ///     <c>LoadBudgetDetails</c> hides them) re-acquires its goal links.
    ///   • <c>GoalService.InsertGoal</c> per existing budget, so a freshly-
    ///     created goal seeds links into all of the entity's existing months.
    ///
    /// Silently no-ops when <c>goals_budget_categories</c> doesn't exist
    /// (migration not yet applied) — the caller's <c>hasGoalTable</c> check
    /// is the primary guard, but this is defensive in case a future caller
    /// forgets it.
    /// </summary>
    internal static async Task EnsureGoalCategoriesForBudgetAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction? tx,
        string budgetId,
        Guid entityId)
    {
        bool hasGoalTable;
        const string checkSql = "SELECT to_regclass('public.goals_budget_categories') IS NOT NULL";
        await using (var checkCmd = new NpgsqlCommand(checkSql, conn, tx))
        {
            hasGoalTable = (await checkCmd.ExecuteScalarAsync()) is bool b && b;
        }
        if (!hasGoalTable) return;

        // Pull non-archived goals for this entity.
        var goals = new List<(Guid GoalId, string Name, double MonthlyTarget)>();
        const string goalsSql = @"SELECT id, name, monthly_target
                                  FROM goals
                                  WHERE entity_id=@eid
                                    AND COALESCE(archived, FALSE) = FALSE";
        await using (var goalsCmd = new NpgsqlCommand(goalsSql, conn, tx))
        {
            goalsCmd.Parameters.AddWithValue("eid", entityId);
            await using var reader = await goalsCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var gId = reader.GetGuid(0);
                var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (string.IsNullOrWhiteSpace(name)) continue;
                var mt = reader.IsDBNull(2) ? 0d : (double)reader.GetDecimal(2);
                goals.Add((gId, name, mt));
            }
        }
        if (goals.Count == 0) return;

        // Resolve (or create) the entity's "Savings" group for any newly-
        // inserted budget_categories rows.
        var savingsGroupId = await EnsureGroupAsync(conn, tx, entityId, "Savings", "savings");

        const string findCatSql = "SELECT id FROM budget_categories WHERE budget_id=@bid AND name=@name";
        const string insertCatSql = @"INSERT INTO budget_categories
            (budget_id, name, target, is_fund, group_id, sort_order, carryover)
            VALUES (@bid, @name, @target, true, @group_id, 0, 0)
            RETURNING id";
        // Idempotent insert without relying on a unique constraint on
        // (goal_id, budget_cat_id) — schema may or may not have one.
        const string insertAssocSql = @"INSERT INTO goals_budget_categories (goal_id, budget_cat_id)
            SELECT @goal_id, @budget_cat_id
            WHERE NOT EXISTS (
                SELECT 1 FROM goals_budget_categories
                WHERE goal_id = @goal_id AND budget_cat_id = @budget_cat_id
            )";

        foreach (var (goalId, name, monthlyTarget) in goals)
        {
            long budgetCatId;
            await using (var findCmd = new NpgsqlCommand(findCatSql, conn, tx))
            {
                findCmd.Parameters.AddWithValue("bid", budgetId);
                findCmd.Parameters.AddWithValue("name", name);
                var idObj = await findCmd.ExecuteScalarAsync();
                if (idObj != null)
                {
                    budgetCatId = idObj is long l ? l : Convert.ToInt64(idObj);
                }
                else
                {
                    await using var insCmd = new NpgsqlCommand(insertCatSql, conn, tx);
                    insCmd.Parameters.AddWithValue("bid", budgetId);
                    insCmd.Parameters.AddWithValue("name", name);
                    insCmd.Parameters.AddWithValue("target", (decimal)monthlyTarget);
                    insCmd.Parameters.AddWithValue("group_id", savingsGroupId);
                    var newId = await insCmd.ExecuteScalarAsync();
                    budgetCatId = newId is long l2 ? l2 : Convert.ToInt64(newId);
                }
            }

            await using (var assocCmd = new NpgsqlCommand(insertAssocSql, conn, tx))
            {
                assocCmd.Parameters.AddWithValue("goal_id", goalId);
                assocCmd.Parameters.AddWithValue("budget_cat_id", budgetCatId);
                await assocCmd.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// Heuristic for inferring a budget_group's kind from its name when a
    /// category arrives with no explicit group_id (e.g. typed-in new group on
    /// the inline category editor, or a seed payload sourcing from
    /// `DEFAULT_BUDGET_TEMPLATES`). Lifted out of SaveBudget so the seed
    /// helper and EnsureGroup-by-kind-callers share the same convention.
    /// </summary>
    internal static string InferGroupKind(string name)
    {
        var n = (name ?? string.Empty).Trim().ToLowerInvariant();
        if (n == "income") return "income";
        if (n == "savings") return "savings";
        return "expense";
    }

    public async Task RecalculateCarryover(string budgetId, IEnumerable<string> categoryNames, string userId, string userEmail)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        await RecalculateCarryoverForAffectedCategories(conn, budgetId, categoryNames, userId, userEmail);
    }

    private DateTime? ParseDate(string? input) =>
        DateTime.TryParse(input, out var dt) ? dt : (DateTime?)null;

    private async Task<bool> EnsureBudgetEditHistoryTableAsync(NpgsqlConnection conn, NpgsqlTransaction? transaction)
    {
        if (_hasBudgetEditHistoryTable.HasValue)
            return _hasBudgetEditHistoryTable.Value;

        await _editHistoryCheckLock.WaitAsync();
        try
        {
            if (_hasBudgetEditHistoryTable.HasValue)
                return _hasBudgetEditHistoryTable.Value;

            const string checkSql = "SELECT to_regclass('public.budget_edit_history') IS NOT NULL";
            await using var checkCmd = new NpgsqlCommand(checkSql, conn);
            if (transaction != null)
                checkCmd.Transaction = transaction;
            var result = await checkCmd.ExecuteScalarAsync();
            _hasBudgetEditHistoryTable = result is bool exists && exists;
            return _hasBudgetEditHistoryTable.Value;
        }
        finally
        {
            _editHistoryCheckLock.Release();
        }
    }

    private async Task LogEdit(
        NpgsqlConnection conn,
        string budgetId,
        string userId,
        string userEmail,
        string action,
        NpgsqlTransaction? transaction = null)
    {
        if (!await EnsureBudgetEditHistoryTableAsync(conn, transaction))
            return;

        const string sql = "INSERT INTO budget_edit_history (budget_id, user_id, user_email, timestamp, action) VALUES (@bid,@uid,@email, now(), @action)";
        await using var cmd = new NpgsqlCommand(sql, conn, transaction);
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
        parameters.AddWithValue("transaction_date", (object?)ParseDate(tx.TransactionDate) ?? DBNull.Value);
        parameters.AddWithValue("posted_date", (object?)ParseDate(tx.PostedDate) ?? DBNull.Value);
        parameters.AddWithValue("imported_merchant", (object?)tx.ImportedMerchant ?? DBNull.Value);
        parameters.AddWithValue("status", (object?)tx.Status ?? DBNull.Value);
        parameters.AddWithValue("check_number", (object?)tx.CheckNumber ?? DBNull.Value);
        parameters.AddWithValue("deleted", tx.Deleted.HasValue ? (object)tx.Deleted.Value : DBNull.Value);
        SetOptionalGuid(parameters, "entity_id", tx.EntityId);
        parameters.AddWithValue("transaction_type", (object?)tx.TransactionType ?? "standard");
    }

    private static void SetOptionalGuid(NpgsqlParameterCollection parameters, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SetOptionalGuid(parameters, name, (Guid?)null);
            return;
        }

        SetOptionalGuid(parameters, name, Guid.Parse(value));
    }

    private static void SetOptionalGuid(NpgsqlParameterCollection parameters, string name, Guid? value)
    {
        var parameter = parameters.Add(name, NpgsqlDbType.Uuid);
        parameter.Value = value.HasValue ? value.Value : (object)DBNull.Value;
    }

    /// <summary>
    /// Look up (or create on miss) a budget_groups row for the given entity by
    /// case-sensitive name match. Returns the resolved group id. Used by
    /// SaveBudget/GoalService to upsert groups inline when categories are
    /// persisted with only a group name.
    /// </summary>
    internal static async Task<Guid> EnsureGroupAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction? tx,
        Guid entityId,
        string name,
        string kind = "expense")
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Group name cannot be empty", nameof(name));

        // Try existing first to avoid bumping updated_at on every save.
        const string lookupSql = "SELECT id FROM budget_groups WHERE entity_id=@eid AND name=@name LIMIT 1";
        await using (var lookup = new NpgsqlCommand(lookupSql, conn, tx))
        {
            lookup.Parameters.AddWithValue("eid", entityId);
            lookup.Parameters.AddWithValue("name", trimmed);
            var existing = await lookup.ExecuteScalarAsync();
            if (existing is Guid g) return g;
        }

        // Compute next sort_order at the end of the entity's group list.
        const string upsertSql = @"
            INSERT INTO budget_groups (entity_id, name, kind, sort_order)
            VALUES (@eid, @name, @kind,
                    COALESCE((SELECT max(sort_order) + 1 FROM budget_groups WHERE entity_id=@eid), 0))
            ON CONFLICT (entity_id, name) DO UPDATE SET updated_at=now()
            RETURNING id";
        await using var insert = new NpgsqlCommand(upsertSql, conn, tx);
        insert.Parameters.AddWithValue("eid", entityId);
        insert.Parameters.AddWithValue("name", trimmed);
        insert.Parameters.AddWithValue("kind", kind);
        var idObj = await insert.ExecuteScalarAsync();
        if (idObj is Guid id) return id;
        throw new InvalidOperationException($"Failed to upsert budget_group '{trimmed}' for entity {entityId}");
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

    public async Task<Budget> MergeBudgets(string targetBudgetId, string sourceBudgetId, string userId, string userEmail)
    {
        if (string.IsNullOrWhiteSpace(targetBudgetId) || string.IsNullOrWhiteSpace(sourceBudgetId))
            throw new ArgumentException("Both target and source budget ids are required.");

        if (string.Equals(targetBudgetId, sourceBudgetId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Target and source budget ids must be different.", nameof(sourceBudgetId));

        _logger.LogInformation("Merging budget {SourceBudgetId} into {TargetBudgetId}", sourceBudgetId, targetBudgetId);

        var targetBudget = await GetBudget(targetBudgetId)
            ?? throw new InvalidOperationException($"Target budget {targetBudgetId} not found");
        var sourceBudget = await GetBudget(sourceBudgetId)
            ?? throw new InvalidOperationException($"Source budget {sourceBudgetId} not found");

        if (!string.Equals(targetBudget.FamilyId, sourceBudget.FamilyId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Budgets must belong to the same family to merge.");

        var targetEntity = targetBudget.EntityId ?? string.Empty;
        var sourceEntity = sourceBudget.EntityId ?? string.Empty;
        if (!string.Equals(targetEntity, sourceEntity, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Budgets must belong to the same entity to merge.");

        if (!string.Equals(targetBudget.Month, sourceBudget.Month, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Budgets must represent the same month to merge.");

        targetBudget.IncomeTarget = Math.Max(targetBudget.IncomeTarget, sourceBudget.IncomeTarget);
        if (string.IsNullOrWhiteSpace(targetBudget.Label) && !string.IsNullOrWhiteSpace(sourceBudget.Label))
            targetBudget.Label = sourceBudget.Label;
        if (string.IsNullOrWhiteSpace(targetBudget.OriginalBudgetId) && !string.IsNullOrWhiteSpace(sourceBudget.OriginalBudgetId))
            targetBudget.OriginalBudgetId = sourceBudget.OriginalBudgetId;

        targetBudget.Categories ??= new List<BudgetCategory>();
        var comparer = StringComparer.OrdinalIgnoreCase;
        var categoryMap = targetBudget.Categories
            .Where(cat => !string.IsNullOrWhiteSpace(cat.Name))
            .ToDictionary(cat => cat.Name!, comparer);

        foreach (var cat in sourceBudget.Categories ?? new List<BudgetCategory>())
        {
            if (string.IsNullOrWhiteSpace(cat.Name))
            {
                targetBudget.Categories.Add(new BudgetCategory
                {
                    Name = cat.Name,
                    Target = cat.Target,
                    IsFund = cat.IsFund,
                    GroupId = cat.GroupId,
                    GroupName = cat.GroupName,
                    SortOrder = cat.SortOrder,
                    Carryover = cat.Carryover,
                    Favorite = cat.Favorite
                });
                continue;
            }

            if (categoryMap.TryGetValue(cat.Name, out var existing))
            {
                const double tolerance = 0.01;
                if (Math.Abs(existing.Target) < tolerance && Math.Abs(cat.Target) > tolerance)
                {
                    existing.Target = cat.Target;
                }
                else if (Math.Abs(existing.Target - cat.Target) > tolerance)
                {
                    existing.Target = Math.Max(existing.Target, cat.Target);
                }

                if (!existing.Carryover.HasValue && cat.Carryover.HasValue)
                    existing.Carryover = cat.Carryover;

                if (!existing.Favorite.HasValue && cat.Favorite.HasValue)
                    existing.Favorite = cat.Favorite;

                // Both budgets share an entity_id (enforced earlier in MergeBudgets),
                // so a missing GroupId on the target should fall back to the source's.
                if (string.IsNullOrWhiteSpace(existing.GroupId) && !string.IsNullOrWhiteSpace(cat.GroupId))
                    existing.GroupId = cat.GroupId;
                if (string.IsNullOrWhiteSpace(existing.GroupName) && !string.IsNullOrWhiteSpace(cat.GroupName))
                    existing.GroupName = cat.GroupName;

                if (!existing.IsFund && cat.IsFund)
                    existing.IsFund = true;
            }
            else
            {
                var clone = new BudgetCategory
                {
                    Name = cat.Name,
                    Target = cat.Target,
                    IsFund = cat.IsFund,
                    GroupId = cat.GroupId,
                    GroupName = cat.GroupName,
                    SortOrder = cat.SortOrder,
                    Carryover = cat.Carryover,
                    Favorite = cat.Favorite
                };
                targetBudget.Categories.Add(clone);
                categoryMap[cat.Name] = clone;
            }
        }

        targetBudget.Transactions ??= new List<Transaction>();
        var targetTransactionIds = new HashSet<string>(
            targetBudget.Transactions
                .Where(t => !string.IsNullOrWhiteSpace(t.Id))
                .Select(t => t.Id!),
            comparer);

        foreach (var tx in sourceBudget.Transactions ?? new List<Transaction>())
        {
            if (string.IsNullOrWhiteSpace(tx.Id) || targetTransactionIds.Contains(tx.Id))
                tx.Id = Guid.NewGuid().ToString();

            tx.BudgetId = targetBudget.BudgetId;
            tx.BudgetMonth = targetBudget.Month;
            tx.EntityId = targetBudget.EntityId;

            targetBudget.Transactions.Add(tx);

            if (!string.IsNullOrWhiteSpace(tx.Id))
                targetTransactionIds.Add(tx.Id);
        }

        targetBudget.Merchants ??= new List<Merchant>();
        var merchantMap = targetBudget.Merchants
            .Where(m => !string.IsNullOrWhiteSpace(m.Name))
            .ToDictionary(m => m.Name!, comparer);

        foreach (var merchant in sourceBudget.Merchants ?? new List<Merchant>())
        {
            if (string.IsNullOrWhiteSpace(merchant.Name))
                continue;

            if (merchantMap.TryGetValue(merchant.Name, out var existing))
            {
                existing.UsageCount += merchant.UsageCount;
            }
            else
            {
                var clone = new Merchant
                {
                    Name = merchant.Name,
                    UsageCount = merchant.UsageCount
                };
                targetBudget.Merchants.Add(clone);
                merchantMap[merchant.Name] = clone;
            }
        }

        try
        {
            await SaveBudget(targetBudgetId, targetBudget, userId, userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist merged budget {TargetBudgetId}", targetBudgetId);
            throw;
        }

        try
        {
            await DeleteBudget(sourceBudgetId, userId, userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merged budget but failed to delete source budget {SourceBudgetId}", sourceBudgetId);
            throw;
        }

        _logger.LogInformation("Successfully merged budget {SourceBudgetId} into {TargetBudgetId}", sourceBudgetId, targetBudgetId);

        return await GetBudget(targetBudgetId) ?? targetBudget;
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
        const string sql = @"INSERT INTO transactions (id, budget_id, date, budget_month, merchant, amount, notes, recurring, recurring_interval, user_id, is_income, account_number, account_source, transaction_date, posted_date, imported_merchant, status, check_number, deleted, entity_id, transaction_type, created_at, updated_at)
VALUES (@id,@budget_id,@date,@budget_month,@merchant,@amount,@notes,@recurring,@recurring_interval,@user_id,@is_income,@account_number,@account_source,@transaction_date,@posted_date,@imported_merchant,@status,@check_number,@deleted,@entity_id,@transaction_type, now(), now())
ON CONFLICT (id) DO UPDATE SET budget_id=EXCLUDED.budget_id, date=EXCLUDED.date, budget_month=EXCLUDED.budget_month, merchant=EXCLUDED.merchant, amount=EXCLUDED.amount, notes=EXCLUDED.notes, recurring=EXCLUDED.recurring, recurring_interval=EXCLUDED.recurring_interval, user_id=EXCLUDED.user_id, is_income=EXCLUDED.is_income, account_number=EXCLUDED.account_number, account_source=EXCLUDED.account_source, transaction_date=EXCLUDED.transaction_date, posted_date=EXCLUDED.posted_date, imported_merchant=EXCLUDED.imported_merchant, status=EXCLUDED.status, check_number=EXCLUDED.check_number, deleted=EXCLUDED.deleted, entity_id=EXCLUDED.entity_id, transaction_type=EXCLUDED.transaction_type, updated_at=now();";
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
        var impactedCategories = cats
            .Where(c => !string.IsNullOrWhiteSpace(c.Category))
            .Select(c => c.Category!);
        await RecalculateCarryoverForAffectedCategories(conn, budgetId, impactedCategories, userId, userEmail);
    }

    public async Task DeleteTransaction(string budgetId, string transactionId, string userId, string userEmail)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        var impactedCategories = new List<string>();
        const string sqlCats = "SELECT category_name FROM transaction_categories WHERE transaction_id=@id";
        await using (var catCmd = new NpgsqlCommand(sqlCats, conn))
        {
            catCmd.Parameters.AddWithValue("id", transactionId);
            await using var reader = await catCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                    impactedCategories.Add(reader.GetString(0));
            }
        }

        const string sql = "DELETE FROM transaction_categories WHERE transaction_id=@id; DELETE FROM transactions WHERE id=@id AND budget_id=@bid";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", transactionId);
        cmd.Parameters.AddWithValue("bid", budgetId);
        await cmd.ExecuteNonQueryAsync();
        await LogEdit(conn, budgetId, userId, userEmail, "delete-transaction");
        await RecalculateCarryoverForAffectedCategories(conn, budgetId, impactedCategories, userId, userEmail);
    }

    public async Task BatchSaveTransactions(string budgetId, List<Transaction> transactions, string userId, string userEmail, bool recalculateCarryover = true)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        await using var dbTx = await conn.BeginTransactionAsync();

        const string sql = @"INSERT INTO transactions (id, budget_id, date, budget_month, merchant, amount, notes, recurring, recurring_interval, user_id, is_income, account_number, account_source, transaction_date, posted_date, imported_merchant, status, check_number, deleted, entity_id, transaction_type, created_at, updated_at)
VALUES (@id,@budget_id,@date,@budget_month,@merchant,@amount,@notes,@recurring,@recurring_interval,@user_id,@is_income,@account_number,@account_source,@transaction_date,@posted_date,@imported_merchant,@status,@check_number,@deleted,@entity_id,@transaction_type, now(), now())
ON CONFLICT (id) DO UPDATE SET budget_id=EXCLUDED.budget_id, date=EXCLUDED.date, budget_month=EXCLUDED.budget_month, merchant=EXCLUDED.merchant, amount=EXCLUDED.amount, notes=EXCLUDED.notes, recurring=EXCLUDED.recurring, recurring_interval=EXCLUDED.recurring_interval, user_id=EXCLUDED.user_id, is_income=EXCLUDED.is_income, account_number=EXCLUDED.account_number, account_source=EXCLUDED.account_source, transaction_date=EXCLUDED.transaction_date, posted_date=EXCLUDED.posted_date, imported_merchant=EXCLUDED.imported_merchant, status=EXCLUDED.status, check_number=EXCLUDED.check_number, deleted=EXCLUDED.deleted, entity_id=EXCLUDED.entity_id, transaction_type=EXCLUDED.transaction_type, updated_at=now();";

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

        if (recalculateCarryover)
        {
            var impactedCategories = transactions
                .SelectMany(t => t.Categories ?? new List<TransactionCategory>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Category))
                .Select(c => c.Category!);
            await RecalculateCarryoverForAffectedCategories(conn, budgetId, impactedCategories, userId, userEmail);
        }
    }

    private async Task RecalculateCarryoverForAffectedCategories(
        NpgsqlConnection conn,
        string budgetId,
        IEnumerable<string> categoryNames,
        string userId,
        string userEmail)
    {
        var normalizedCategories = new HashSet<string>(
            categoryNames
                .Select(name => name?.Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>(),
            StringComparer.OrdinalIgnoreCase);

        if (normalizedCategories.Count == 0)
            return;

        Guid familyId;
        Guid? entityId;
        string startMonth;

        const string metaSql = "SELECT family_id, entity_id, month FROM budgets WHERE id=@bid";
        await using (var metaCmd = new NpgsqlCommand(metaSql, conn))
        {
            metaCmd.Parameters.AddWithValue("bid", budgetId);
            await using var reader = await metaCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return;

            familyId = reader.GetGuid(0);
            entityId = reader.IsDBNull(1) ? null : reader.GetGuid(1);
            startMonth = reader.GetString(2);
        }

        const string budgetsSql = @"SELECT id, family_id, entity_id, month, label, income_target, original_budget_id
                                FROM budgets
                                WHERE family_id=@fid
                                  AND entity_id IS NOT DISTINCT FROM @entity
                                  AND month >= @month
                                ORDER BY month";

        var budgets = new List<Budget>();
        await using (var budgetsCmd = new NpgsqlCommand(budgetsSql, conn))
        {
            budgetsCmd.Parameters.AddWithValue("fid", familyId);
            SetOptionalGuid(budgetsCmd.Parameters, "entity", entityId);
            budgetsCmd.Parameters.AddWithValue("month", startMonth);
            await using var reader = await budgetsCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                budgets.Add(new Budget
                {
                    BudgetId = reader.GetString(0),
                    FamilyId = reader.GetGuid(1).ToString(),
                    EntityId = reader.IsDBNull(2) ? null : reader.GetGuid(2).ToString(),
                    Month = reader.GetString(3),
                    Label = reader.IsDBNull(4) ? null : reader.GetString(4),
                    IncomeTarget = reader.IsDBNull(5) ? 0 : (double)reader.GetDecimal(5),
                    OriginalBudgetId = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
        }

        if (budgets.Count == 0)
            return;

        for (var i = 0; i < budgets.Count; i++)
        {
            await LoadBudgetDetails(conn, budgets[i]);
        }

        var startIndex = budgets.FindIndex(b => b.BudgetId == budgetId);
        if (startIndex < 0 || startIndex >= budgets.Count - 1)
            return;

        var startBudget = budgets[startIndex];
        var impactedFundCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = startIndex; i < budgets.Count; i++)
        {
            foreach (var category in budgets[i].Categories)
            {
                var name = category.Name?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!category.IsFund)
                    continue;
                if (normalizedCategories.Contains(name))
                    impactedFundCategories.Add(name);
            }
        }

        if (impactedFundCategories.Count == 0)
            return;

        var carryForward = CalculateCarryoverForCategories(startBudget, impactedFundCategories);
        var updates = new List<(Budget Budget, Dictionary<string, double> Categories)>();

        for (var i = startIndex + 1; i < budgets.Count; i++)
        {
            var budget = budgets[i];
            var categoryUpdates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var category in budget.Categories)
            {
                var name = category.Name?.Trim();
                if (string.IsNullOrEmpty(name) || !category.IsFund)
                    continue;
                if (!impactedFundCategories.Contains(name))
                    continue;

                var nextCarry = carryForward.TryGetValue(name, out var value) ? value : 0;
                if (!category.Carryover.HasValue || Math.Abs(category.Carryover.Value - nextCarry) > 0.009)
                {
                    categoryUpdates[name] = nextCarry;
                }
                category.Carryover = nextCarry;
            }

            if (categoryUpdates.Count > 0)
            {
                updates.Add((budget, categoryUpdates));
            }

            carryForward = CalculateCarryoverForCategories(budget, impactedFundCategories);
        }

        if (updates.Count == 0)
            return;

        await using var updateTx = await conn.BeginTransactionAsync();
        foreach (var (budget, categories) in updates)
        {
            foreach (var kvp in categories)
            {
                const string updateSql = "UPDATE budget_categories SET carryover=@carry WHERE budget_id=@bid AND name=@name";
                await using var updateCmd = new NpgsqlCommand(updateSql, conn, updateTx);
                updateCmd.Parameters.AddWithValue("carry", (decimal)kvp.Value);
                updateCmd.Parameters.AddWithValue("bid", budget.BudgetId);
                updateCmd.Parameters.AddWithValue("name", kvp.Key);
                await updateCmd.ExecuteNonQueryAsync();
            }

            await LogEdit(conn, budget.BudgetId, userId, userEmail, "carryover-update", updateTx);
        }

        await updateTx.CommitAsync();
        _logger.LogInformation(
            "Updated carryover values for {BudgetCount} future budgets after changes to {BudgetId}",
            updates.Count,
            budgetId);
    }

    private Dictionary<string, double> CalculateCarryoverForCategories(Budget budget, HashSet<string> relevantCategories)
    {
        var results = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (relevantCategories.Count == 0)
            return results;

        var spending = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var income = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var transaction in budget.Transactions)
        {
            if (transaction.Deleted.HasValue && transaction.Deleted.Value)
                continue;
            if (transaction.Categories == null)
                continue;

            var isTransfer = string.Equals(transaction.TransactionType, "transfer", StringComparison.OrdinalIgnoreCase);

            foreach (var split in transaction.Categories)
            {
                var name = split.Category?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!relevantCategories.Contains(name))
                    continue;

                if (isTransfer)
                {
                    // Per-split signed amount. Negative = money leaving this
                    // category (debit / spend); positive = money arriving
                    // (credit / income). Avoids double-counting a fund→fund
                    // transfer as spend on both sides.
                    if (split.Amount < 0)
                    {
                        var debit = Math.Abs(split.Amount);
                        spending[name] = spending.TryGetValue(name, out var existing) ? existing + debit : debit;
                    }
                    else if (split.Amount > 0)
                    {
                        var credit = split.Amount;
                        income[name] = income.TryGetValue(name, out var existing) ? existing + credit : credit;
                    }
                }
                else
                {
                    var amount = Math.Abs(split.Amount);
                    if (transaction.IsIncome)
                    {
                        income[name] = income.TryGetValue(name, out var existing) ? existing + amount : amount;
                    }
                    else
                    {
                        spending[name] = spending.TryGetValue(name, out var existing) ? existing + amount : amount;
                    }
                }
            }
        }

        foreach (var category in budget.Categories)
        {
            var name = category.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                continue;
            if (!category.IsFund || !relevantCategories.Contains(name))
                continue;

            spending.TryGetValue(name, out var spent);
            income.TryGetValue(name, out var received);
            var previousCarry = category.Carryover ?? 0;
            // Allow negative carryover. An overspent fund carries the deficit
            // forward so subsequent months reflect that the fund is in the
            // hole, instead of silently resetting to zero.
            results[name] = previousCarry + category.Target + received - spent;
        }

        return results;
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

        const string sqlTx = @"INSERT INTO imported_transactions (id, document_id, account_id, account_number, account_source, payee, transaction_date, posted_date, amount, status, debit_amount, credit_amount, check_number, deleted, matched, ignored)
                                 VALUES (@id,@doc,@account_id,@acct_num,@acct_src,@payee,@transaction_date,@posted,@amount,@status,@debit,@credit,@check,@deleted,@matched,@ignored)";

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
            txCmd.Parameters.AddWithValue("transaction_date", (object?)ParseDate(tx.TransactionDate) ?? DBNull.Value);
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
        var compositeId = $"{docId}-{transactionId}";
        cmd.CommandText = $"UPDATE imported_transactions SET {string.Join(",", updates)} WHERE id=@id";
        cmd.Parameters.AddWithValue("id", compositeId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<ImportedTransactionDoc>> GetImportedTransactions(string userId)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = @"SELECT d.id, d.family_id, d.user_id, t.id, t.account_id, t.account_number, t.account_source, t.payee, t.transaction_date, t.posted_date, t.amount, t.status, t.debit_amount, t.credit_amount, t.check_number, t.deleted, t.matched, t.ignored
                              FROM imported_transaction_docs d
                              LEFT JOIN imported_transactions t ON d.id = t.document_id
                              WHERE d.user_id=@uid
                              ORDER BY COALESCE(t.transaction_date, t.posted_date) DESC";
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
                    TransactionDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8).ToString("yyyy-MM-dd"),
                    PostedDate = reader.IsDBNull(9) ? null : reader.GetDateTime(9).ToString("yyyy-MM-dd"),
                    Amount = reader.IsDBNull(10) ? null : (double?)reader.GetDecimal(10),
                    Status = reader.IsDBNull(11) ? null : reader.GetString(11),
                    DebitAmount = reader.IsDBNull(12) ? null : (double?)reader.GetDecimal(12),
                    CreditAmount = reader.IsDBNull(13) ? null : (double?)reader.GetDecimal(13),
                    CheckNumber = reader.IsDBNull(14) ? null : reader.GetString(14),
                    Deleted = reader.IsDBNull(15) ? (bool?)null : reader.GetBoolean(15),
                    Matched = reader.IsDBNull(16) ? false : reader.GetBoolean(16),
                    Ignored = reader.IsDBNull(17) ? false : reader.GetBoolean(17)
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
        const string sql = "SELECT id, account_id, account_number, account_source, payee, transaction_date, posted_date, amount, status, debit_amount, credit_amount, check_number, deleted, matched, ignored FROM imported_transactions WHERE account_id=@aid ORDER BY COALESCE(transaction_date, posted_date) DESC LIMIT @limit OFFSET @offset";
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
                TransactionDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-dd"),
                PostedDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6).ToString("yyyy-MM-dd"),
                Amount = reader.IsDBNull(7) ? null : (double?)reader.GetDecimal(7),
                Status = reader.IsDBNull(8) ? null : reader.GetString(8),
                DebitAmount = reader.IsDBNull(9) ? null : (double?)reader.GetDecimal(9),
                CreditAmount = reader.IsDBNull(10) ? null : (double?)reader.GetDecimal(10),
                CheckNumber = reader.IsDBNull(11) ? null : reader.GetString(11),
                Deleted = reader.IsDBNull(12) ? (bool?)null : reader.GetBoolean(12),
                Matched = reader.IsDBNull(13) ? false : reader.GetBoolean(13),
                Ignored = reader.IsDBNull(14) ? false : reader.GetBoolean(14)
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

        const string sql = @"SELECT budget_id, id, date, budget_month, merchant, amount, notes, recurring, recurring_interval, user_id, is_income, account_number, account_source, transaction_date, posted_date, imported_merchant, status, check_number, deleted, entity_id
                             FROM transactions WHERE account_number=@acct";
        var list = new List<TransactionWithBudgetId>();
        await using (var cmd = new NpgsqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("acct", acctNum);
            await using var reader = await cmd.ExecuteReaderAsync();
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
                    TransactionDate = reader.IsDBNull(13) ? null : reader.GetDateTime(13).ToString("yyyy-MM-dd"),
                    PostedDate = reader.IsDBNull(14) ? null : reader.GetDateTime(14).ToString("yyyy-MM-dd"),
                    ImportedMerchant = reader.IsDBNull(15) ? null : reader.GetString(15),
                    Status = reader.IsDBNull(16) ? null : reader.GetString(16),
                    CheckNumber = reader.IsDBNull(17) ? null : reader.GetString(17),
                    Deleted = reader.IsDBNull(18) ? (bool?)null : reader.GetBoolean(18),
                    EntityId = reader.IsDBNull(19) ? null : reader.GetGuid(19).ToString()
                };
                list.Add(new TransactionWithBudgetId { BudgetId = reader.GetString(0), Transaction = tx });
            }
        } // reader is closed here

        // Load categories for all matched transactions
        if (list.Count > 0)
        {
            var txIds = list.Select(t => t.Transaction.Id!).ToArray();
            const string sqlCats = "SELECT transaction_id, category_name, amount FROM transaction_categories WHERE transaction_id = ANY(@ids)";
            await using var catCmd = new NpgsqlCommand(sqlCats, conn);
            catCmd.Parameters.AddWithValue("ids", txIds);
            await using var catReader = await catCmd.ExecuteReaderAsync();
            var txMap = list.ToDictionary(t => t.Transaction.Id!);
            while (await catReader.ReadAsync())
            {
                var txId = catReader.GetString(0);
                if (!txMap.TryGetValue(txId, out var item)) continue;
                item.Transaction.Categories ??= new List<TransactionCategory>();
                item.Transaction.Categories.Add(new TransactionCategory
                {
                    Category = catReader.IsDBNull(1) ? null : catReader.GetString(1),
                    Amount = catReader.IsDBNull(2) ? 0 : (double)catReader.GetDecimal(2)
                });
            }
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

        var ids = reconciliations.Select(r => r.ImportedTransactionId).ToArray();
        var matches = reconciliations.Select(r => r.Match).ToArray();
        var ignores = reconciliations.Select(r => r.Ignore).ToArray();

        const string sqlImported = @"UPDATE imported_transactions i
            SET matched = d.match,
                ignored = d.ignore,
                status = CASE WHEN d.match THEN 'C' ELSE i.status END
            FROM UNNEST(@ids, @matches, @ignores) AS d(id, match, ignore)
            WHERE i.id = d.id";

        await using (var cmd = new NpgsqlCommand(sqlImported, conn))
        {
            cmd.Parameters.AddWithValue("ids", ids);
            cmd.Parameters.AddWithValue("matches", matches);
            cmd.Parameters.AddWithValue("ignores", ignores);
            await cmd.ExecuteNonQueryAsync();
        }

        var matched = reconciliations
            .Where(r => r.Match && !string.IsNullOrEmpty(r.BudgetTransactionId))
            .ToList();

        if (matched.Count > 0)
        {
            var budgetIds = matched.Select(r => r.BudgetTransactionId!).ToArray();
            var importedIds = matched.Select(r => r.ImportedTransactionId).ToArray();

        const string sqlBudget = @"UPDATE transactions t
                SET account_number = it.account_number,
                    account_source = it.account_source,
                    transaction_date = it.transaction_date,
                    posted_date = it.posted_date,
                    imported_merchant = it.payee,
                    status = 'C',
                    check_number = it.check_number
                FROM UNNEST(@budgetIds, @importedIds) AS m(budget_id, imported_id)
                JOIN imported_transactions it ON it.id = m.imported_id
                WHERE t.id = m.budget_id";

            await using (var budCmd = new NpgsqlCommand(sqlBudget, conn))
            {
                budCmd.Parameters.AddWithValue("budgetIds", budgetIds);
                budCmd.Parameters.AddWithValue("importedIds", importedIds);
                await budCmd.ExecuteNonQueryAsync();
            }
        }

        await LogEdit(conn, budgetId, userId, userEmail, "batch-reconcile");
    }
}
