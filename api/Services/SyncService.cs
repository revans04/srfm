// FamilyBudgetApi/Services/SyncService.cs
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using FamilyBudgetApi.Models;
using NpgsqlTypes;

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
        /// Perform a full synchronization of budgets (including their transactions)
        /// and imported transactions from Firestore into the PostgreSQL database.
        /// </summary>
        public async Task FullSyncFirestoreToSupabaseAsync()
        {
            _logger.LogInformation("Starting full Firestore → Supabase sync.");
            await SyncUsersAsync(null);
            await SyncFamiliesAsync(null);
            await SyncAccountsAsync(null);
            await SyncSnapshotsAsync(null);
            await SyncBudgetsAsync(null);
            await SyncImportedTransactionsAsync(null);
            _logger.LogInformation("Completed full Firestore → Supabase sync.");
        }

        /// <summary>
        /// Perform an incremental synchronization from Firestore into the PostgreSQL database.
        /// Only records updated since the provided timestamp will be synchronized.
        /// </summary>
        /// <param name="since">The timestamp after which updated records should be synced.</param>
        public async Task IncrementalSyncFirestoreToSupabaseAsync(DateTime since)
        {
            var sinceUtc = since.Kind == DateTimeKind.Utc ? since : since.ToUniversalTime();
            _logger.LogInformation("Starting incremental Firestore → Supabase sync since {Since}.", sinceUtc);
            await SyncUsersAsync(sinceUtc);
            await SyncFamiliesAsync(sinceUtc);
            await SyncAccountsAsync(sinceUtc);
            await SyncSnapshotsAsync(sinceUtc);
            await SyncBudgetsAsync(sinceUtc);
            await SyncImportedTransactionsAsync(sinceUtc);
            _logger.LogInformation("Completed incremental Firestore → Supabase sync.");
        }

        public async Task SyncUsersAsync(DateTime? updatedSince)
        {
            Query query = _firestoreDb.Collection("users");
            if (updatedSince.HasValue)
            {
                var ts = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(updatedSince.Value.ToUniversalTime());
                query = query.WhereGreaterThanOrEqualTo("updatedAt", ts);
            }

            var snapshot = await query.GetSnapshotAsync();
            var supabaseUsers = new List<PgUser>();

            foreach (var doc in snapshot.Documents)
            {
                var user = doc.ConvertTo<UserData>();
                var createdAt = doc.CreateTime?.ToDateTime() ?? DateTime.UtcNow;
                var updatedAt = doc.UpdateTime?.ToDateTime() ?? createdAt;
                supabaseUsers.Add(new PgUser
                {
                    Uid = user.Uid ?? doc.Id,
                    Email = user.Email,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt
                });
            }

            if (supabaseUsers.Count == 0)
            {
                _logger.LogInformation("No users to upsert into Supabase.");
                return;
            }

            await using var conn = CreateNpgsqlConnection();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO users (uid, email, created_at, updated_at)
                VALUES (@uid, @email, @created_at, @updated_at)
                ON CONFLICT (uid) DO UPDATE SET
                    email = COALESCE(EXCLUDED.email, users.email),
                    created_at = COALESCE(users.created_at, EXCLUDED.created_at),
                    updated_at = COALESCE(EXCLUDED.updated_at, users.updated_at);";

            await using var transaction = await conn.BeginTransactionAsync();
            foreach (var u in supabaseUsers)
            {
                await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("uid", u.Uid);
                cmd.Parameters.AddWithValue("email", (object?)u.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("created_at", u.CreatedAt);
                cmd.Parameters.AddWithValue("updated_at", u.UpdatedAt);
                await cmd.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }

        public async Task SyncFamiliesAsync(DateTime? updatedSince)
        {
            Query query = _firestoreDb.Collection("families");
            if (updatedSince.HasValue)
            {
                var ts = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(updatedSince.Value.ToUniversalTime());
                query = query.WhereGreaterThanOrEqualTo("updatedAt", ts);
            }

            var snapshot = await query.GetSnapshotAsync();
            var supabaseFamilies = new List<PgFamily>();
            var supabaseMembers = new List<PgFamilyMember>();

            foreach (var doc in snapshot.Documents)
            {
                var family = doc.ConvertTo<Family>();
                var familyId = TryParseGuid(family.Id) ?? Guid.NewGuid();
                var createdAt = family.CreatedAt.ToDateTime();
                var updatedAt = family.UpdatedAt.ToDateTime();

                supabaseFamilies.Add(new PgFamily
                {
                    Id = familyId,
                    Name = family.Name,
                    OwnerUid = family.OwnerUid,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt
                });

                if (family.Members != null)
                {
                    foreach (var m in family.Members)
                    {
                        if (m.Uid != null)
                        {
                            supabaseMembers.Add(new PgFamilyMember
                            {
                                FamilyId = familyId,
                                UserId = m.Uid,
                                Role = m.Role
                            });
                        }
                    }
                }
            }

            if (supabaseFamilies.Count == 0)
            {
                _logger.LogInformation("No families to upsert into Supabase.");
                return;
            }

            await using var conn = CreateNpgsqlConnection();
            await conn.OpenAsync();

            const string famSql = @"INSERT INTO families (id, name, owner_uid, created_at, updated_at)
                VALUES (@id, @name, @owner_uid, @created_at, @updated_at)
                ON CONFLICT (id) DO UPDATE SET
                    name = COALESCE(EXCLUDED.name, families.name),
                    owner_uid = COALESCE(EXCLUDED.owner_uid, families.owner_uid),
                    created_at = COALESCE(families.created_at, EXCLUDED.created_at),
                    updated_at = COALESCE(EXCLUDED.updated_at, families.updated_at);";

            await using var transaction = await conn.BeginTransactionAsync();
            foreach (var f in supabaseFamilies)
            {
                await using var cmd = new NpgsqlCommand(famSql, conn, transaction);
                cmd.Parameters.AddWithValue("id", f.Id);
                cmd.Parameters.AddWithValue("name", f.Name);
                cmd.Parameters.AddWithValue("owner_uid", (object?)f.OwnerUid ?? DBNull.Value);
                cmd.Parameters.AddWithValue("created_at", f.CreatedAt);
                cmd.Parameters.AddWithValue("updated_at", f.UpdatedAt);
                await cmd.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();

            if (supabaseMembers.Count == 0)
            {
                _logger.LogInformation("No family members to upsert into Supabase.");
                return;
            }

            await using var memberTx = await conn.BeginTransactionAsync();
            foreach (var group in supabaseMembers.GroupBy(m => m.FamilyId))
            {
                await using var delCmd = new NpgsqlCommand("DELETE FROM family_members WHERE family_id=@fid", conn, memberTx);
                delCmd.Parameters.AddWithValue("fid", group.Key);
                await delCmd.ExecuteNonQueryAsync();

                foreach (var m in group)
                {
                    await using var cmd = new NpgsqlCommand(@"INSERT INTO family_members (family_id, user_id, role)
                        VALUES (@fid, @uid, @role)", conn, memberTx);
                    cmd.Parameters.AddWithValue("fid", m.FamilyId);
                    cmd.Parameters.AddWithValue("uid", m.UserId);
                    cmd.Parameters.AddWithValue("role", (object?)m.Role ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            await memberTx.CommitAsync();
        }

        public async Task SyncAccountsAsync(DateTime? updatedSince)
        {
            // Accounts are stored as an array on each family document, not a subcollection.
            // Iterate families and upsert their embedded accounts.
            var supabaseAccounts = new List<PgAccount>();
            var familiesSnap = await _firestoreDb.Collection("families").GetSnapshotAsync();

            foreach (var familyDoc in familiesSnap.Documents)
            {
                var family = familyDoc.ConvertTo<Family>();
                var familyGuid = TryParseGuid(familyDoc.Id);
                var accounts = family.Accounts ?? new List<Account>();

                if (updatedSince.HasValue)
                {
                    accounts = accounts
                        .Where(a => a.UpdatedAt.ToDateTime() >= updatedSince.Value.ToUniversalTime())
                        .ToList();
                }

                foreach (var account in accounts)
                {
                    var createdAt = account.CreatedAt.ToDateTime();
                    var updatedAt = account.UpdatedAt.ToDateTime();
                    if (createdAt == DateTime.MinValue)
                    {
                        createdAt = familyDoc.CreateTime?.ToDateTime() ?? DateTime.UtcNow;
                    }
                    if (updatedAt == DateTime.MinValue)
                    {
                        updatedAt = familyDoc.UpdateTime?.ToDateTime() ?? createdAt;
                    }

                    var accountId = TryParseGuid(account.Id) ?? Guid.NewGuid();
                    var userGuid = TryParseGuid(account.UserId);
                    supabaseAccounts.Add(new PgAccount
                    {
                        Id = accountId,
                        FamilyId = familyGuid,
                        UserId = userGuid,
                        Name = account.Name,
                        Type = account.Type,
                        Category = account.Category,
                        AccountNumber = account.AccountNumber,
                        Institution = account.Institution,
                        Balance = (decimal?)account.Balance ?? 0m,
                        InterestRate = (decimal?)account.Details?.InterestRate,
                        AppraisedValue = (decimal?)account.Details?.AppraisedValue,
                        MaturityDate = TryParseDate(account.Details?.MaturityDate),
                        Address = account.Details?.Address,
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt
                    });
                }
            }

            if (supabaseAccounts.Count == 0)
            {
                _logger.LogInformation("No accounts to upsert into Supabase.");
                return;
            }

            await using var conn = CreateNpgsqlConnection();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO accounts
                (id, family_id, user_id, name, type, category, account_number, institution, balance, interest_rate,
                 appraised_value, maturity_date, address, created_at, updated_at)
                VALUES (@id, @family_id, @user_id, @name, @type, @category, @account_number, @institution, @balance, @interest_rate,
                        @appraised_value, @maturity_date, @address, @created_at, @updated_at)
                ON CONFLICT (id) DO UPDATE SET
                    family_id = EXCLUDED.family_id,
                    user_id = COALESCE(EXCLUDED.user_id, accounts.user_id),
                    name = COALESCE(EXCLUDED.name, accounts.name),
                    type = COALESCE(EXCLUDED.type, accounts.type),
                    category = COALESCE(EXCLUDED.category, accounts.category),
                    account_number = COALESCE(EXCLUDED.account_number, accounts.account_number),
                    institution = COALESCE(EXCLUDED.institution, accounts.institution),
                    balance = COALESCE(EXCLUDED.balance, accounts.balance),
                    interest_rate = COALESCE(EXCLUDED.interest_rate, accounts.interest_rate),
                    appraised_value = COALESCE(EXCLUDED.appraised_value, accounts.appraised_value),
                    maturity_date = COALESCE(EXCLUDED.maturity_date, accounts.maturity_date),
                    address = COALESCE(EXCLUDED.address, accounts.address),
                    created_at = COALESCE(EXCLUDED.created_at, accounts.created_at),
                    updated_at = COALESCE(EXCLUDED.updated_at, accounts.updated_at);";

            await using var transaction = await conn.BeginTransactionAsync();
            foreach (var a in supabaseAccounts)
            {
                await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("id", a.Id);
                cmd.Parameters.AddWithValue("family_id", (object?)a.FamilyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("user_id", (object?)a.UserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("name", a.Name);
                cmd.Parameters.AddWithValue("type", a.Type);
                cmd.Parameters.AddWithValue("category", a.Category);
                cmd.Parameters.AddWithValue("account_number", (object?)a.AccountNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("institution", a.Institution);
                cmd.Parameters.AddWithValue("balance", a.Balance);
                cmd.Parameters.AddWithValue("interest_rate", (object?)a.InterestRate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("appraised_value", (object?)a.AppraisedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("maturity_date", (object?)a.MaturityDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("address", (object?)a.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("created_at", a.CreatedAt);
                cmd.Parameters.AddWithValue("updated_at", a.UpdatedAt);
                await cmd.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }

        public async Task SyncSnapshotsAsync(DateTime? updatedSince)
        {
            var supabaseSnapshots = new List<PgSnapshot>();
            var supabaseSnapshotAccounts = new List<PgSnapshotAccount>();

            // Firestore stores snapshots as a subcollection under each family document.
            // Enumerate families explicitly so we don't miss any snapshots if collection
            // group queries fail due to missing indexes or security rules.

            var familyDocs = await _firestoreDb.Collection("families").GetSnapshotAsync();
            var families = familyDocs.Documents.Select(doc => doc.ConvertTo<Family>());
            foreach (var familyDoc in families)
            {
                // Console.WriteLine("Family JSON: " + System.Text.Json.JsonSerializer.Serialize(familyDoc));
                var snapshots = familyDoc.Snapshots ?? new List<Snapshot>();
                var familyId = TryParseGuid(familyDoc.Id);
                if (updatedSince.HasValue)
                {
                    snapshots = snapshots.Where(s => DateTime.TryParse(s.CreatedAt, out var dt) && dt >= updatedSince.Value).ToList();
                }

                // log snapshot count for this family
                // Console.WriteLine($"Found {snapshots.Count} snapshots for family {familyDoc.Id}");
                foreach (var snap in snapshots)
                {
                    // Console.WriteLine("snap JSON: " + System.Text.Json.JsonSerializer.Serialize(snap));
                    var snapId = TryParseGuid(snap.Id) ?? Guid.NewGuid();
                    var snapshotDate = snap.Date != null ? TryParseDate(snap.Date) : null;
                    var createdAt = DateTime.TryParse(snap.CreatedAt, out var ca) ? ca : DateTime.UtcNow;

                    supabaseSnapshots.Add(new PgSnapshot
                    {
                        Id = snapId,
                        FamilyId = familyId,
                        SnapshotDate = snapshotDate,
                        NetWorth = (decimal)snap.NetWorth,
                        CreatedAt = createdAt
                    });
                    // write out json for debugging
                    // Console.WriteLine("PgSnapshot JSON: " + System.Text.Json.JsonSerializer.Serialize(supabaseSnapshots[supabaseSnapshots.Count - 1]));

                    if (snap.Accounts != null)
                    {
                        foreach (var acct in snap.Accounts)
                        {
                            var acctId = TryParseGuid(acct.AccountId) ?? Guid.NewGuid();
                            supabaseSnapshotAccounts.Add(new PgSnapshotAccount
                            {
                                SnapshotId = snapId,
                                AccountId = acctId,
                                AccountName = acct.AccountName,
                                Value = (decimal)acct.Value,
                                AccountType = acct.Type
                            });
                        }
                    }
                }
            }

            if (supabaseSnapshots.Count == 0)
            {
                _logger.LogInformation("No snapshots to upsert into Supabase.");
                return;
            }

            _logger.LogInformation("Upserting {SnapshotCount} snapshots and {AccountCount} snapshot accounts", supabaseSnapshots.Count, supabaseSnapshotAccounts.Count);

            await using var conn = CreateNpgsqlConnection();
            await conn.OpenAsync();

            const string snapSql = @"INSERT INTO snapshots
                (id, family_id, snapshot_date, net_worth, created_at)
                VALUES (@id, @family_id, @snapshot_date, @net_worth, @created_at)
                ON CONFLICT (id) DO UPDATE SET
                    family_id = COALESCE(EXCLUDED.family_id, snapshots.family_id),
                    snapshot_date = COALESCE(EXCLUDED.snapshot_date, snapshots.snapshot_date),
                    net_worth = COALESCE(EXCLUDED.net_worth, snapshots.net_worth),
                    created_at = COALESCE(EXCLUDED.created_at, snapshots.created_at);";

            await using var transaction = await conn.BeginTransactionAsync();
            foreach (var s in supabaseSnapshots)
            {
                await using var cmd = new NpgsqlCommand(snapSql, conn, transaction);
                cmd.Parameters.AddWithValue("id", s.Id);
                cmd.Parameters.AddWithValue("family_id", (object?)s.FamilyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("snapshot_date", (object?)s.SnapshotDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("net_worth", s.NetWorth);
                cmd.Parameters.AddWithValue("created_at", s.CreatedAt);
                await cmd.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();

            if (supabaseSnapshotAccounts.Count == 0)
            {
                _logger.LogInformation("No snapshot accounts to upsert into Supabase.");
                return;
            }

            await using var accountTx = await conn.BeginTransactionAsync();
            foreach (var group in supabaseSnapshotAccounts.GroupBy(a => a.SnapshotId))
            {
                await using var delCmd = new NpgsqlCommand("DELETE FROM snapshot_accounts WHERE snapshot_id=@sid", conn, accountTx);
                delCmd.Parameters.AddWithValue("sid", group.Key);
                await delCmd.ExecuteNonQueryAsync();

                foreach (var a in group)
                {
                    await using var cmd = new NpgsqlCommand(@"INSERT INTO snapshot_accounts
                        (snapshot_id, account_id, account_name, value, account_type)
                        VALUES (@snapshot_id, @account_id, @account_name, @value, @account_type::account_type)", conn, accountTx);
                    cmd.Parameters.AddWithValue("snapshot_id", a.SnapshotId);
                    cmd.Parameters.AddWithValue("account_id", a.AccountId);
                    cmd.Parameters.AddWithValue("account_name", a.AccountName);
                    cmd.Parameters.AddWithValue("value", a.Value);
                    var acctTypeParam = cmd.Parameters.Add("account_type", NpgsqlDbType.Unknown);
                    acctTypeParam.Value = a.AccountType;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            await accountTx.CommitAsync();
        }
        private async Task SyncBudgetsAsync(DateTime? updatedSince)
        {
            Query query = _firestoreDb.Collection("budgets");
            if (updatedSince.HasValue)
            {
                var ts = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(updatedSince.Value.ToUniversalTime());
                // Firestore stores timestamps as 'updatedAt'
                query = query.WhereGreaterThanOrEqualTo("updatedAt", ts);
            }

            var snapshot = await query.GetSnapshotAsync();
            var supabaseBudgets = new List<PgBudget>();
            var supabaseTransactions = new List<PgTransaction>();
            var supabaseTxCategories = new List<PgTransactionCategory>();
            var supabaseCategories = new List<PgBudgetCategory>();
            var supabaseMerchants = new List<PgMerchant>();

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
                    CreatedAt = doc.CreateTime?.ToDateTime() ?? DateTime.UtcNow,
                    UpdatedAt = doc.UpdateTime?.ToDateTime() ?? DateTime.UtcNow
                };
                supabaseBudgets.Add(pgBudget);

                foreach (var tx in budget.Transactions)
                {
                    var pgTransaction = new PgTransaction
                    {
                        Id = tx.Id ?? Guid.NewGuid().ToString(),
                        BudgetId = budget.BudgetId,
                        Date = TryParseDate(tx.Date),
                        BudgetMonth = tx.BudgetMonth,
                        Merchant = tx.Merchant,
                        Amount = (decimal)tx.Amount,
                        Notes = tx.Notes,
                        Recurring = tx.Recurring,
                        RecurringInterval = tx.RecurringInterval,
                        UserId = tx.UserId,
                        IsIncome = tx.IsIncome,
                        AccountNumber = tx.AccountNumber,
                        AccountSource = tx.AccountSource,
                        PostedDate = TryParseDate(tx.PostedDate),
                        ImportedMerchant = tx.ImportedMerchant,
                        Status = tx.Status,
                        CheckNumber = tx.CheckNumber,
                        Deleted = tx.Deleted,
                        EntityId = TryParseGuid(tx.EntityId),
                        CreatedAt = doc.CreateTime?.ToDateTime() ?? DateTime.UtcNow,
                        UpdatedAt = doc.UpdateTime?.ToDateTime() ?? DateTime.UtcNow
                    };
                    supabaseTransactions.Add(pgTransaction);

                    // Map split categories for this transaction, ensuring at least one row
                    if (tx.Categories != null && tx.Categories.Count > 0)
                    {
                        foreach (var c in tx.Categories)
                        {
                            supabaseTxCategories.Add(new PgTransactionCategory
                            {
                                TransactionId = pgTransaction.Id,
                                CategoryName = c.Category ?? string.Empty,
                                Amount = (decimal)c.Amount
                            });
                        }
                    }
                    else
                    {
                        supabaseTxCategories.Add(new PgTransactionCategory
                        {
                            TransactionId = pgTransaction.Id,
                            CategoryName = tx.IsIncome ? "Income" : "Uncategorized",
                            Amount = (decimal)tx.Amount
                        });
                    }
                }

                foreach (var cat in budget.Categories)
                {
                    supabaseCategories.Add(new PgBudgetCategory
                    {
                        BudgetId = budget.BudgetId,
                        Name = cat.Name,
                        Group = cat.Group,
                        Carryover = (decimal?)cat.Carryover,
                        Target = (decimal)cat.Target,
                        IsFund = cat.IsFund
                    });
                }

                foreach (var m in budget.Merchants)
                {
                    supabaseMerchants.Add(new PgMerchant
                    {
                        BudgetId = budget.BudgetId,
                        Name = m.Name,
                        UsageCount = m.UsageCount
                    });
                }
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
                    family_id = COALESCE(EXCLUDED.family_id, budgets.family_id),
                    entity_id = COALESCE(EXCLUDED.entity_id, budgets.entity_id),
                    month = COALESCE(EXCLUDED.month, budgets.month),
                    label = COALESCE(EXCLUDED.label, budgets.label),
                    income_target = COALESCE(EXCLUDED.income_target, budgets.income_target),
                    original_budget_id = COALESCE(EXCLUDED.original_budget_id, budgets.original_budget_id),
                    created_at = COALESCE(EXCLUDED.created_at, budgets.created_at),
                    updated_at = COALESCE(EXCLUDED.updated_at, budgets.updated_at);";

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
                cmd.Parameters.AddWithValue("created_at", b.CreatedAt ?? DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updated_at", b.UpdatedAt ?? DateTime.UtcNow);
                await cmd.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();

            if (supabaseTransactions.Count > 0)
            {
                const string txSql = @"INSERT INTO transactions
                (id, budget_id, date, budget_month, merchant, amount, notes, recurring, recurring_interval, user_id,
                 is_income, account_number, account_source, posted_date, imported_merchant, status, check_number,
                 deleted, entity_id, created_at, updated_at)
                VALUES (@id, @budget_id, @date, @budget_month, @merchant, @amount, @notes, @recurring, @recurring_interval, @user_id,
                        @is_income, @account_number, @account_source, @posted_date, @imported_merchant, @status, @check_number,
                        @deleted, @entity_id, @created_at, @updated_at)
                ON CONFLICT (id) DO UPDATE SET
                    budget_id = COALESCE(EXCLUDED.budget_id, transactions.budget_id),
                    date = COALESCE(EXCLUDED.date, transactions.date),
                    budget_month = COALESCE(EXCLUDED.budget_month, transactions.budget_month),
                    merchant = COALESCE(EXCLUDED.merchant, transactions.merchant),
                    amount = COALESCE(EXCLUDED.amount, transactions.amount),
                    notes = COALESCE(EXCLUDED.notes, transactions.notes),
                    recurring = COALESCE(EXCLUDED.recurring, transactions.recurring),
                    recurring_interval = COALESCE(EXCLUDED.recurring_interval, transactions.recurring_interval),
                    user_id = COALESCE(EXCLUDED.user_id, transactions.user_id),
                    is_income = COALESCE(EXCLUDED.is_income, transactions.is_income),
                    account_number = COALESCE(EXCLUDED.account_number, transactions.account_number),
                    account_source = COALESCE(EXCLUDED.account_source, transactions.account_source),
                    posted_date = COALESCE(EXCLUDED.posted_date, transactions.posted_date),
                    imported_merchant = COALESCE(EXCLUDED.imported_merchant, transactions.imported_merchant),
                    status = COALESCE(EXCLUDED.status, transactions.status),
                    check_number = COALESCE(EXCLUDED.check_number, transactions.check_number),
                    deleted = COALESCE(EXCLUDED.deleted, transactions.deleted),
                    entity_id = COALESCE(EXCLUDED.entity_id, transactions.entity_id),
                    created_at = COALESCE(EXCLUDED.created_at, transactions.created_at),
                    updated_at = COALESCE(EXCLUDED.updated_at, transactions.updated_at);";
                await using var txTransaction = await conn.BeginTransactionAsync();
                foreach (var t in supabaseTransactions)
                {
                    await using var cmd = new NpgsqlCommand(txSql, conn, txTransaction);
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
                    cmd.Parameters.AddWithValue("created_at", t.CreatedAt ?? DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("updated_at", t.UpdatedAt ?? DateTime.UtcNow);
                    await cmd.ExecuteNonQueryAsync();
                }
                await txTransaction.CommitAsync();
            }

            if (supabaseCategories.Count > 0)
            {
                await using var catTx = await conn.BeginTransactionAsync();
                foreach (var group in supabaseCategories.GroupBy(c => c.BudgetId))
                {
                    await using var delCmd = new NpgsqlCommand("DELETE FROM budget_categories WHERE budget_id=@bid", conn, catTx);
                    delCmd.Parameters.AddWithValue("bid", group.Key);
                    await delCmd.ExecuteNonQueryAsync();

                    foreach (var c in group)
                    {
                        await using var cmd = new NpgsqlCommand(@"INSERT INTO budget_categories
                            (budget_id, name, ""group"", carryover, target, is_fund)
                            VALUES (@budget_id, @name, @group, @carryover, @target, @is_fund)", conn, catTx);
                        cmd.Parameters.AddWithValue("budget_id", c.BudgetId);
                        cmd.Parameters.AddWithValue("name", (object?)c.Name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("group", (object?)c.Group ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("carryover", (object?)c.Carryover ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("target", c.Target);
                        cmd.Parameters.AddWithValue("is_fund", c.IsFund);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await catTx.CommitAsync();
            }

            // Upsert transaction split categories
            if (supabaseTxCategories.Count > 0)
            {
                await using var splitTx = await conn.BeginTransactionAsync();
                // Group by budget using the transactions we just prepared
                foreach (var grp in supabaseTransactions.GroupBy(t => t.BudgetId))
                {
                    var ids = grp.Select(t => t.Id).ToArray();
                    if (ids.Length == 0) continue;

                    // Delete existing splits for these transactions to avoid duplicates
                    await using (var delSplits = new NpgsqlCommand("DELETE FROM transaction_categories WHERE transaction_id = ANY(@ids)", conn, splitTx))
                    {
                        delSplits.Parameters.AddWithValue("ids", ids);
                        await delSplits.ExecuteNonQueryAsync();
                    }

                    // Insert splits for only these transactions
                    var rows = supabaseTxCategories.Where(s => ids.Contains(s.TransactionId));
                    const string insSql = "INSERT INTO transaction_categories (transaction_id, category_name, amount) VALUES (@tid, @name, @amount)";
                    foreach (var row in rows)
                    {
                        await using var ins = new NpgsqlCommand(insSql, conn, splitTx);
                        ins.Parameters.AddWithValue("tid", row.TransactionId);
                        ins.Parameters.AddWithValue("name", row.CategoryName);
                        ins.Parameters.AddWithValue("amount", row.Amount);
                        await ins.ExecuteNonQueryAsync();
                    }
                }
                await splitTx.CommitAsync();
            }

            if (supabaseMerchants.Count > 0)
            {
                await using var merchTx = await conn.BeginTransactionAsync();
                foreach (var group in supabaseMerchants.GroupBy(m => m.BudgetId))
                {
                    await using var delCmd = new NpgsqlCommand("DELETE FROM merchants WHERE budget_id=@bid", conn, merchTx);
                    delCmd.Parameters.AddWithValue("bid", group.Key);
                    await delCmd.ExecuteNonQueryAsync();

                    foreach (var m in group)
                    {
                        await using var cmd = new NpgsqlCommand(@"INSERT INTO merchants
                            (budget_id, name, usage_count)
                            VALUES (@budget_id, @name, @usage_count)", conn, merchTx);
                        cmd.Parameters.AddWithValue("budget_id", m.BudgetId);
                        cmd.Parameters.AddWithValue("name", (object?)m.Name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("usage_count", m.UsageCount);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await merchTx.CommitAsync();
            }
        }

        private async Task SyncImportedTransactionsAsync(DateTime? updatedSince)
        {
            Query query = _firestoreDb.Collection("importedtransactions");
            if (updatedSince.HasValue)
            {
                var ts = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(updatedSince.Value.ToUniversalTime());
                query = query.WhereGreaterThanOrEqualTo("updatedAt", ts);
            }

            var snapshot = await query.GetSnapshotAsync();
            var supabaseImported = new List<PgImportedTransaction>();

            foreach (var doc in snapshot.Documents)
            {
                var importedDoc = doc.ConvertTo<ImportedTransactionDoc>();
                foreach (var tx in importedDoc.importedTransactions)
                {
                    var pgImported = new PgImportedTransaction
                    {
                        Id = tx.Id ?? Guid.NewGuid().ToString(),
                        DocumentId = importedDoc.Id,
                        AccountId = tx.AccountId,
                        AccountNumber = tx.AccountNumber,
                        AccountSource = tx.AccountSource,
                        Payee = tx.Payee,
                        PostedDate = TryParseDate(tx.PostedDate),
                        Amount = (decimal?)tx.Amount ?? 0m,
                        Status = tx.Status,
                        Matched = tx.Matched,
                        Ignored = tx.Ignored,
                        DebitAmount = (decimal?)tx.DebitAmount,
                        CreditAmount = (decimal?)tx.CreditAmount,
                        CheckNumber = tx.CheckNumber,
                        Deleted = tx.Deleted,
                        FamilyId = TryParseGuid(importedDoc.FamilyId),
                        UserId = importedDoc.UserId,
                        CreatedAt = doc.CreateTime?.ToDateTime() ?? DateTime.UtcNow,
                        UpdatedAt = doc.UpdateTime?.ToDateTime() ?? DateTime.UtcNow
                    };
                    supabaseImported.Add(pgImported);
                }
            }

            if (supabaseImported.Count == 0)
            {
                _logger.LogInformation("No imported transactions to upsert into Supabase.");
                return;
            }

            await using var conn = CreateNpgsqlConnection();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO imported_transactions
                (id, document_id, account_id, account_number, account_source, payee, posted_date, amount, status, matched,
                 ignored, debit_amount, credit_amount, check_number, deleted, family_id, user_id, created_at, updated_at)
                VALUES (@id, @document_id, @account_id, @account_number, @account_source, @payee, @posted_date, @amount, @status, @matched,
                        @ignored, @debit_amount, @credit_amount, @check_number, @deleted, @family_id, @user_id, @created_at, @updated_at)
                ON CONFLICT (id) DO UPDATE SET
                    document_id = COALESCE(EXCLUDED.document_id, imported_transactions.document_id),
                    account_id = COALESCE(EXCLUDED.account_id, imported_transactions.account_id),
                    account_number = COALESCE(EXCLUDED.account_number, imported_transactions.account_number),
                    account_source = COALESCE(EXCLUDED.account_source, imported_transactions.account_source),
                    payee = COALESCE(EXCLUDED.payee, imported_transactions.payee),
                    posted_date = COALESCE(EXCLUDED.posted_date, imported_transactions.posted_date),
                    amount = COALESCE(EXCLUDED.amount, imported_transactions.amount),
                    status = COALESCE(EXCLUDED.status, imported_transactions.status),
                    matched = COALESCE(EXCLUDED.matched, imported_transactions.matched),
                    ignored = COALESCE(EXCLUDED.ignored, imported_transactions.ignored),
                    debit_amount = COALESCE(EXCLUDED.debit_amount, imported_transactions.debit_amount),
                    credit_amount = COALESCE(EXCLUDED.credit_amount, imported_transactions.credit_amount),
                    check_number = COALESCE(EXCLUDED.check_number, imported_transactions.check_number),
                    deleted = COALESCE(EXCLUDED.deleted, imported_transactions.deleted),
                    family_id = COALESCE(EXCLUDED.family_id, imported_transactions.family_id),
                    user_id = COALESCE(EXCLUDED.user_id, imported_transactions.user_id),
                    created_at = COALESCE(EXCLUDED.created_at, imported_transactions.created_at),
                    updated_at = COALESCE(EXCLUDED.updated_at, imported_transactions.updated_at);";

            await using var dbTransaction = await conn.BeginTransactionAsync();
            foreach (var t in supabaseImported)
            {
                await using var cmd = new NpgsqlCommand(sql, conn, dbTransaction);
                cmd.Parameters.AddWithValue("id", t.Id);
                cmd.Parameters.AddWithValue("document_id", t.DocumentId);
                cmd.Parameters.AddWithValue("account_id", t.AccountId);
                cmd.Parameters.AddWithValue("account_number", (object?)t.AccountNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("account_source", (object?)t.AccountSource ?? DBNull.Value);
                cmd.Parameters.AddWithValue("payee", (object?)t.Payee ?? DBNull.Value);
                cmd.Parameters.AddWithValue("posted_date", (object?)t.PostedDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("amount", t.Amount);
                cmd.Parameters.AddWithValue("status", (object?)t.Status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("matched", t.Matched);
                cmd.Parameters.AddWithValue("ignored", t.Ignored);
                cmd.Parameters.AddWithValue("debit_amount", (object?)t.DebitAmount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("credit_amount", (object?)t.CreditAmount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("check_number", (object?)t.CheckNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("deleted", (object?)t.Deleted ?? DBNull.Value);
                cmd.Parameters.AddWithValue("family_id", (object?)t.FamilyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("user_id", (object?)t.UserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("created_at", t.CreatedAt ?? DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updated_at", t.UpdatedAt ?? DateTime.UtcNow);
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

            // Disable the default 30-second command timeout so large sync batches don't fail
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                CommandTimeout = 0
            };
            return new NpgsqlConnection(builder.ConnectionString);
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

    /// <summary>
    /// Represents the 'users' table.
    /// </summary>
    public class PgUser
    {
        public string Uid { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents the 'families' table.
    /// </summary>
    public class PgFamily
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OwnerUid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents rows in the 'family_members' table.
    /// </summary>
    public class PgFamilyMember
    {
        public Guid FamilyId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    /// <summary>
    /// Represents the 'imported_transactions' table.
    /// </summary>
    public class PgImportedTransaction
    {
        public string Id { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string? AccountNumber { get; set; }
        public string? AccountSource { get; set; }
        public string? Payee { get; set; }
        public DateTime? PostedDate { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public bool Matched { get; set; }
        public bool Ignored { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public string? CheckNumber { get; set; }
        public bool? Deleted { get; set; }
        public Guid? FamilyId { get; set; }
        public string? UserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents the 'accounts' table.
    /// </summary>
    public class PgAccount
    {
        public Guid Id { get; set; }
        public Guid? FamilyId { get; set; }
        public Guid? UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? AccountNumber { get; set; }
        public string Institution { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? AppraisedValue { get; set; }
        public DateTime? MaturityDate { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents the 'snapshots' table.
    /// </summary>
    public class PgSnapshot
    {
        public Guid Id { get; set; }
        public Guid? FamilyId { get; set; }
        public DateTime? SnapshotDate { get; set; }
        public decimal NetWorth { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents the 'snapshot_accounts' table.
    /// </summary>
    public class PgSnapshotAccount
    {
        public Guid SnapshotId { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string AccountType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the 'budget_categories' table.
    /// </summary>
    public class PgBudgetCategory
    {
        public string BudgetId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Group { get; set; }
        public decimal? Carryover { get; set; }
        public decimal Target { get; set; }
        public bool IsFund { get; set; }
    }

    /// <summary>
    /// Represents the 'merchants' table.
    /// </summary>
    public class PgMerchant
    {
        public string BudgetId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// Represents rows in the 'transaction_categories' table (split amounts per transaction).
    /// </summary>
    public class PgTransactionCategory
    {
        public string TransactionId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
