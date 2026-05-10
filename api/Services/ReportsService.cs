using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FamilyBudgetApi.Services;

public class ReportsService
{
    private readonly SupabaseDbService _db;
    private readonly ILogger<ReportsService> _logger;

    public ReportsService(SupabaseDbService db, ILogger<ReportsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Verify the caller is a member of the family that owns <paramref name="entityId"/>.
    /// Single round-trip via JOIN — closes the GoalController-style gap where
    /// entityId was trusted without a membership check.
    /// </summary>
    public async Task<bool> UserCanAccessEntity(NpgsqlConnection conn, Guid entityId, string userId)
    {
        const string sql = @"SELECT 1
                             FROM entities e
                             JOIN family_members fm ON fm.family_id = e.family_id
                             WHERE e.id = @eid AND fm.user_id = @uid
                             LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("eid", entityId);
        cmd.Parameters.AddWithValue("uid", userId);
        return await cmd.ExecuteScalarAsync() != null;
    }

    /// <summary>
    /// Aggregate transaction splits by payee (transactions.merchant) over a
    /// budget-month range, broken down by category.
    ///
    /// Sign convention mirrors ReportsPage's Monthly Overview: standard expense
    /// splits add to spend; income splits on an expense category subtract
    /// (refunds net against spend). Transfers and income-group splits are
    /// excluded; goal-only category splits are dropped so pure goal
    /// contributions/withdrawals don't appear here. Goal-funded standard
    /// expenses still count on their destination category — they're regular
    /// expenses with a funded_by_goal_id pointer (see CLAUDE.md invariants).
    /// </summary>
    public async Task<List<PayeeSpendingRow>> GetSpendingByPayee(
        string entityId,
        string userId,
        string fromMonth,
        string toMonth,
        IReadOnlyList<string>? excludeGroupIds,
        IReadOnlyList<string>? excludeCategoryNames,
        IReadOnlyList<string>? excludeMerchants)
    {
        if (!Guid.TryParse(entityId, out var entityGuid))
            throw new ArgumentException("Invalid entityId");

        await using var conn = await _db.GetOpenConnectionAsync();

        if (!await UserCanAccessEntity(conn, entityGuid, userId))
        {
            _logger.LogWarning("User {UserId} attempted by-payee report for entity {EntityId} without membership", userId, entityId);
            throw new UnauthorizedAccessException("Caller is not a member of the family that owns this entity");
        }

        var hasGoalTable = false;
        const string checkSql = "SELECT to_regclass('public.goals_budget_categories') IS NOT NULL";
        await using (var checkCmd = new NpgsqlCommand(checkSql, conn))
        {
            var result = await checkCmd.ExecuteScalarAsync();
            if (result is bool b) hasGoalTable = b;
        }

        var excludeGroupGuids = (excludeGroupIds ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => Guid.TryParse(s, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToArray();

        var excludeCategoryArray = (excludeCategoryNames ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        // Normalize user-supplied merchant exclusions so they match the
        // grouped payee key produced by the SELECT (which trims, collapses
        // whitespace, and folds curly/grave/etc. apostrophe variants to
        // ASCII). Without this, " McDonald's " (U+2019) in the URL would
        // never match the canonical "McDonald's" (U+0027) key.
        var excludeMerchantArray = (excludeMerchants ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(NormalizePayeeKey)
            .ToArray();

        // Group by (payee, category_name) at the SQL layer; rebuild per-payee
        // structure in C#. Keeps the query simple and lets us sort/totalize
        // without window functions.
        var goalFilter = hasGoalTable
            ? "AND NOT EXISTS (SELECT 1 FROM goals_budget_categories gbc WHERE gbc.budget_cat_id = bc.id)"
            : string.Empty;

        // Canonical payee key. Without this, "McDonald's" (U+2019 curly
        // apostrophe, common in bank feeds), "McDonald's" (U+0027 ASCII,
        // typed by hand), and "MCDONALD'S" all hash differently and end up
        // as separate rows. Steps, innermost-out:
        //   1. regexp_replace folds curly/grave/acute/modifier/prime
        //      apostrophe variants to ASCII '
        //   2. regexp_replace collapses runs of whitespace to a single space
        //   3. TRIM removes leading/trailing whitespace
        //   4. NULLIF + COALESCE bucket empty strings under "(No payee)"
        // The grouping/comparison key additionally LOWERs this so case-only
        // differences merge. For DISPLAY we use MODE() WITHIN GROUP to pick
        // the most-common original form — keeps the table reading naturally
        // ("McDonald's") instead of all-lowercase ("mcdonald's").
        const string payeeCanonical =
            "COALESCE(NULLIF(TRIM(regexp_replace(regexp_replace(t.merchant, " +
            "'[‘’`´ʼ′]', '''', 'g'), " +
            "'\\s+', ' ', 'g')), ''), '(No payee)')";

        var sql = $@"
            SELECT
                MODE() WITHIN GROUP (ORDER BY {payeeCanonical}) AS payee,
                tc.category_name AS category_name,
                SUM(CASE WHEN t.is_income THEN -tc.amount ELSE tc.amount END) AS amt
            FROM transactions t
            JOIN transaction_categories tc ON tc.transaction_id = t.id
            JOIN budget_categories bc      ON bc.budget_id = t.budget_id AND bc.name = tc.category_name
            LEFT JOIN budget_groups bg     ON bg.id = bc.group_id
            WHERE t.entity_id = @eid
              AND COALESCE(t.deleted, false) = false
              AND t.budget_month BETWEEN @from AND @to
              AND COALESCE(t.transaction_type, 'standard') <> 'transfer'
              AND COALESCE(bg.kind, 'expense') <> 'income'
              {goalFilter}
              AND NOT (bc.group_id = ANY(@excGroups))
              AND NOT (tc.category_name = ANY(@excCats))
              AND NOT (LOWER({payeeCanonical}) = ANY(@excMerchants))
            GROUP BY LOWER({payeeCanonical}), tc.category_name
            ORDER BY payee, tc.category_name";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("eid", entityGuid);
        cmd.Parameters.AddWithValue("from", fromMonth);
        cmd.Parameters.AddWithValue("to", toMonth);
        cmd.Parameters.Add(new NpgsqlParameter("excGroups", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = excludeGroupGuids });
        cmd.Parameters.Add(new NpgsqlParameter("excCats", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = excludeCategoryArray });
        cmd.Parameters.Add(new NpgsqlParameter("excMerchants", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = excludeMerchantArray });

        var byPayee = new Dictionary<string, PayeeSpendingRow>(StringComparer.Ordinal);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var payee = reader.IsDBNull(0) ? "(No payee)" : reader.GetString(0);
            var categoryName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var amount = reader.IsDBNull(2) ? 0d : (double)reader.GetDecimal(2);

            if (!byPayee.TryGetValue(payee, out var row))
            {
                row = new PayeeSpendingRow { Payee = payee };
                byPayee[payee] = row;
            }
            row.Total += amount;
            row.Categories.Add(new PayeeCategoryBreakdown { Name = categoryName, Amount = amount });
        }

        var ordered = byPayee.Values
            .Select(r =>
            {
                r.Categories = r.Categories
                    .OrderByDescending(c => c.Amount)
                    .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return r;
            })
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.Payee, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogInformation(
            "By-payee report for entity {EntityId} {From}..{To}: {PayeeCount} payees",
            entityId, fromMonth, toMonth, ordered.Count);

        return ordered;
    }

    private static readonly Regex WhitespaceRun = new("\\s+", RegexOptions.Compiled);

    /// <summary>
    /// Mirror of the SQL <c>LOWER(payeeCanonical)</c> normalization for
    /// client-supplied merchant-exclusion strings. Keep the two
    /// implementations in lockstep — diverging means exclusions silently
    /// miss rows.
    /// </summary>
    private static string NormalizePayeeKey(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var folded = value
            .Replace('‘', '\'') // left single quotation mark
            .Replace('’', '\'') // right single quotation mark (most common)
            .Replace('`', '\'') // grave accent
            .Replace('´', '\'') // acute accent
            .Replace('ʼ', '\'') // modifier letter apostrophe
            .Replace('′', '\''); // prime
        return WhitespaceRun.Replace(folded, " ").Trim().ToLowerInvariant();
    }
}
