using FamilyBudgetApi.Models;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FamilyBudgetApi.Services;

/// <summary>
/// Atomic first-run setup for a brand-new user. Creates family +
/// family_members + entity + an income budget_group + (optionally) a starter
/// budget with categories + (optionally) a few accounts in a single Postgres
/// transaction. The pre-existing flow stitched these together via 5+
/// sequential POSTs from the wizard, with no rollback if any one failed —
/// see the SetupWizard rewrite plan, "BLOCKER" #2.
/// </summary>
public class OnboardingService
{
    private readonly SupabaseDbService _db;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(SupabaseDbService db, ILogger<OnboardingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Seed a brand-new user. If the user already has a family, returns
    /// <c>Created=false</c> with their existing FamilyId and the first
    /// entity/budget IDs we can find — caller (controller) translates that
    /// to a 409 so the UI can short-circuit straight to the budget page.
    /// </summary>
    public async Task<OnboardingSeedResponse> SeedAsync(string userUid, string userEmail, OnboardingSeedRequest request)
    {
        if (string.IsNullOrWhiteSpace(userUid))
            throw new ArgumentException("userUid is required", nameof(userUid));
        if (string.IsNullOrWhiteSpace(request.FamilyName))
            throw new ArgumentException("FamilyName is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.EntityName))
            throw new ArgumentException("EntityName is required", nameof(request));
        if (!Enum.TryParse<EntityType>(request.EntityType, ignoreCase: true, out _))
            throw new ArgumentException($"Invalid EntityType: {request.EntityType}", nameof(request));

        await using var conn = await _db.GetOpenConnectionAsync();

        // Idempotency: if this user already belongs to a family, surface that
        // back to the controller (409) so the UI doesn't accidentally clone.
        var existingFamilyId = await FindExistingFamilyForUserAsync(conn, userUid);
        if (existingFamilyId.HasValue)
        {
            _logger.LogInformation("Onboarding seed short-circuit: user {Uid} already belongs to family {FamilyId}", userUid, existingFamilyId);
            var (existingEntityId, existingBudgetId) = await PickFirstEntityAndBudgetAsync(conn, existingFamilyId.Value);
            return new OnboardingSeedResponse
            {
                FamilyId = existingFamilyId.Value.ToString(),
                EntityId = existingEntityId?.ToString() ?? string.Empty,
                BudgetId = existingBudgetId,
                Created = false
            };
        }

        var familyId = Guid.NewGuid();
        var entityGuid = Guid.NewGuid();
        var monthString = string.IsNullOrWhiteSpace(request.Month) ? null : request.Month!.Trim();
        // Budget id derivation matches the frontend convention used
        // elsewhere: `${ownerUid}_${entityId}_${YYYY-MM}` so subsequent
        // saves from the SPA naturally upsert this row.
        var budgetId = monthString == null ? null : $"{userUid}_{entityGuid}_{monthString}";

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            // 1. families
            const string sqlFamily = "INSERT INTO families (id, name, owner_uid, created_at, updated_at) VALUES (@id, @name, @owner, now(), now())";
            await using (var cmd = new NpgsqlCommand(sqlFamily, conn, tx))
            {
                cmd.Parameters.AddWithValue("id", familyId);
                cmd.Parameters.AddWithValue("name", request.FamilyName.Trim());
                cmd.Parameters.AddWithValue("owner", userUid);
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. family_members (the requesting user, owner role).
            const string sqlMember = "INSERT INTO family_members (family_id, user_id, role) VALUES (@fid, @uid, @role)";
            await using (var cmd = new NpgsqlCommand(sqlMember, conn, tx))
            {
                cmd.Parameters.AddWithValue("fid", familyId);
                cmd.Parameters.AddWithValue("uid", userUid);
                cmd.Parameters.AddWithValue("role", "owner");
                await cmd.ExecuteNonQueryAsync();
            }

            // 3. entity (with persisted template_budget JSONB + tax_form_ids).
            // Persist a minimal template snapshot so future "create budget for
            // next month" calls have a deterministic seed instead of relying
            // on the frontend's DEFAULT_BUDGET_TEMPLATES dictionary.
            TemplateBudget? template = null;
            if (request.UseTemplate && request.TemplateCategories?.Count > 0)
            {
                template = new TemplateBudget
                {
                    Categories = new List<BudgetCategory>(),
                };
                foreach (var seed in request.TemplateCategories)
                {
                    template.Categories.Add(new BudgetCategory
                    {
                        Name = seed.Name,
                        GroupName = seed.GroupName,
                        Target = seed.Target,
                        IsFund = seed.IsFund,
                    });
                }
            }

            const string sqlEntity = @"INSERT INTO entities
                (id, family_id, name, type, created_at, updated_at, template_budget, tax_form_ids)
                VALUES (@id, @fid, @name, @type, now(), now(), @template_budget, @tax_form_ids)";
            await using (var cmd = new NpgsqlCommand(sqlEntity, conn, tx))
            {
                cmd.Parameters.AddWithValue("id", entityGuid);
                cmd.Parameters.AddWithValue("fid", familyId);
                cmd.Parameters.AddWithValue("name", request.EntityName.Trim());
                cmd.Parameters.AddWithValue("type", request.EntityType);
                var tmplParam = cmd.Parameters.Add("template_budget", NpgsqlDbType.Jsonb);
                tmplParam.Value = template == null ? (object)DBNull.Value : JsonConvert.SerializeObject(template);
                var taxParam = cmd.Parameters.Add("tax_form_ids", NpgsqlDbType.Array | NpgsqlDbType.Text);
                taxParam.Value = request.TaxFormIds?.ToArray() ?? Array.Empty<string>();
                await cmd.ExecuteNonQueryAsync();
            }

            // 4. Pre-create the entity's Income group eagerly. Currently
            // EnsureGroupAsync would do this on first budget save; doing it
            // up-front means the entity is "ready" even if the user
            // intentionally skips creating a budget here.
            await BudgetService.EnsureGroupAsync(conn, tx, entityGuid, "Income", "income");

            // 5. budget + categories (only if Month was supplied).
            var accountIds = new List<string>();
            if (budgetId != null && monthString != null)
            {
                var seedBudget = new Budget
                {
                    BudgetId = budgetId,
                    FamilyId = familyId.ToString(),
                    EntityId = entityGuid.ToString(),
                    Month = monthString,
                    Label = monthString,
                    IncomeTarget = 0,
                    Categories = template?.Categories ?? new List<BudgetCategory>(),
                };

                await BudgetService.WriteBudgetAndCategoriesAsync(conn, tx, budgetId, seedBudget, _logger);
            }

            // 6. Optional starter accounts.
            if (request.Accounts?.Count > 0)
            {
                const string sqlAccount = @"INSERT INTO accounts
                    (id, family_id, user_id, name, type, category, account_number, institution, balance, interest_rate, appraised_value, maturity_date, address, created_at, updated_at)
                    VALUES (@id, @fid, @uid, @name, @type::account_type, @cat::account_category, @acctNum, @inst, @bal, NULL, NULL, NULL, NULL, now(), now())";
                foreach (var seed in request.Accounts)
                {
                    if (string.IsNullOrWhiteSpace(seed.Name)) continue;
                    var accountId = Guid.NewGuid();
                    var category = !string.IsNullOrWhiteSpace(seed.Category)
                        ? seed.Category!
                        : InferAccountCategory(seed.Type);

                    await using var cmd = new NpgsqlCommand(sqlAccount, conn, tx);
                    cmd.Parameters.AddWithValue("id", accountId);
                    cmd.Parameters.AddWithValue("fid", familyId);
                    cmd.Parameters.AddWithValue("uid", DBNull.Value);
                    cmd.Parameters.AddWithValue("name", seed.Name.Trim());
                    cmd.Parameters.AddWithValue("type", seed.Type);
                    cmd.Parameters.AddWithValue("cat", category);
                    cmd.Parameters.AddWithValue("acctNum", (object?)seed.AccountNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("inst", seed.Institution ?? string.Empty);
                    cmd.Parameters.AddWithValue("bal", seed.Balance.HasValue ? (object)seed.Balance.Value : DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();

                    accountIds.Add(accountId.ToString());
                }
            }

            await tx.CommitAsync();

            _logger.LogInformation(
                "Onboarding seed complete for {Uid}: family={FamilyId} entity={EntityId} budget={BudgetId} accounts={AccountCount}",
                userUid, familyId, entityGuid, budgetId, accountIds.Count);

            return new OnboardingSeedResponse
            {
                FamilyId = familyId.ToString(),
                EntityId = entityGuid.ToString(),
                BudgetId = budgetId,
                AccountIds = accountIds,
                Created = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onboarding seed failed for user {Uid}; rolling back", userUid);
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<Guid?> FindExistingFamilyForUserAsync(NpgsqlConnection conn, string uid)
    {
        const string sql = @"SELECT family_id FROM family_members WHERE user_id=@uid LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", uid);
        var result = await cmd.ExecuteScalarAsync();
        if (result is Guid g) return g;
        return null;
    }

    private static async Task<(Guid? entityId, string? budgetId)> PickFirstEntityAndBudgetAsync(NpgsqlConnection conn, Guid familyId)
    {
        Guid? entityId = null;
        const string entitySql = "SELECT id FROM entities WHERE family_id=@fid ORDER BY created_at LIMIT 1";
        await using (var cmd = new NpgsqlCommand(entitySql, conn))
        {
            cmd.Parameters.AddWithValue("fid", familyId);
            var result = await cmd.ExecuteScalarAsync();
            if (result is Guid g) entityId = g;
        }
        if (entityId == null) return (null, null);

        string? budgetId = null;
        const string budgetSql = "SELECT id FROM budgets WHERE entity_id=@eid ORDER BY month DESC LIMIT 1";
        await using (var cmd = new NpgsqlCommand(budgetSql, conn))
        {
            cmd.Parameters.AddWithValue("eid", entityId.Value);
            var result = await cmd.ExecuteScalarAsync();
            if (result is string s) budgetId = s;
        }
        return (entityId, budgetId);
    }

    /// <summary>
    /// Default account category if the seed doesn't specify one. Mirrors the
    /// frontend's `AccountForm` defaults: credit cards and loans are
    /// liabilities, everything else is an asset.
    /// </summary>
    private static string InferAccountCategory(string type)
    {
        return type switch
        {
            "CreditCard" => "Liability",
            "Loan" => "Liability",
            _ => "Asset",
        };
    }
}
