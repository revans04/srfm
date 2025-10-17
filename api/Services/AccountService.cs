using FamilyBudgetApi.Models;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;

namespace FamilyBudgetApi.Services;

/// <summary>
/// Account service backed by Supabase/PostgreSQL.
/// Implements core CRUD operations for accounts and snapshots.
/// </summary>
public class AccountService
{
    private readonly SupabaseDbService _db;
    private readonly ILogger<AccountService> _logger;

    public AccountService(SupabaseDbService db, ILogger<AccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Account>> GetAccounts(string familyId)
    {
        _logger.LogInformation("Fetching accounts for family {FamilyId}", familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!Guid.TryParse(familyId, out var fid))
        {
            _logger.LogWarning("Invalid familyId {FamilyId} when fetching accounts", familyId);
            return new List<Account>();
        }

        if (!await FamilyExists(conn, fid))
        {
            return new List<Account>();
        }

        const string sql =
            "SELECT id, user_id, name, type, category, account_number, institution, balance, interest_rate, appraised_value, maturity_date, address, created_at, updated_at FROM accounts WHERE family_id::uuid=@fid";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fid", fid);
        await using var reader = await cmd.ExecuteReaderAsync();

        var results = new List<Account>();
        while (await reader.ReadAsync())
        {
            results.Add(ReadAccount(reader));
        }
        _logger.LogInformation("Retrieved {Count} accounts for family {FamilyId}", results.Count, familyId);
        return results;
    }

    public async Task<Account?> GetAccount(string familyId, string accountId)
    {
        _logger.LogInformation("Fetching account {AccountId} for family {FamilyId}", accountId, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!Guid.TryParse(familyId, out var fid) || !Guid.TryParse(accountId, out var aid))
        {
            _logger.LogWarning("Invalid familyId {FamilyId} or accountId {AccountId}", familyId, accountId);
            return null;
        }

        if (!await FamilyExists(conn, fid))
        {
            return null;
        }

        const string sql =
            "SELECT id, user_id, name, type, category, account_number, institution, balance, interest_rate, appraised_value, maturity_date, address, created_at, updated_at FROM accounts WHERE family_id::uuid=@fid AND id=@id";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fid", fid);
        cmd.Parameters.AddWithValue("id", aid);
        await using var reader = await cmd.ExecuteReaderAsync();
        var result = await reader.ReadAsync() ? ReadAccount(reader) : null;
        if (result == null)
            _logger.LogInformation("Account {AccountId} not found for family {FamilyId}", accountId, familyId);
        return result;
    }

    public async Task SaveAccount(string familyId, Account account)
    {
        _logger.LogInformation("Saving account {AccountName} for family {FamilyId}", account.Name, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!Guid.TryParse(familyId, out var fid))
        {
            _logger.LogWarning("Invalid familyId {FamilyId} when saving account", familyId);
            throw new ArgumentException("Invalid familyId");
        }

        if (!await FamilyExists(conn, fid))
        {
            return;
        }

        var accountId = Guid.TryParse(account.Id, out var parsed) ? parsed : Guid.NewGuid();
        account.Id = accountId.ToString();

        const string sql = @"INSERT INTO accounts
            (id, family_id, user_id, name, type, category, account_number, institution, balance, interest_rate, appraised_value, maturity_date, address, created_at, updated_at)
            VALUES (@id, @fid, @uid, @name, @type::account_type, @cat::account_category, @acctNum, @inst, @bal, @ir, @appVal, @mat, @addr, now(), now())
            ON CONFLICT (id) DO UPDATE SET
            user_id=EXCLUDED.user_id,
            name=EXCLUDED.name,
            type=EXCLUDED.type,
            category=EXCLUDED.category,
            account_number=EXCLUDED.account_number,
            institution=EXCLUDED.institution,
            balance=EXCLUDED.balance,
            interest_rate=EXCLUDED.interest_rate,
            appraised_value=EXCLUDED.appraised_value,
            maturity_date=EXCLUDED.maturity_date,
            address=EXCLUDED.address,
            updated_at=now();";

        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", accountId);
        cmd.Parameters.AddWithValue("fid", fid);
        cmd.Parameters.AddWithValue("uid", (object?)account.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("name", account.Name);
        cmd.Parameters.AddWithValue("type", account.Type);
        cmd.Parameters.AddWithValue("cat", account.Category);
        cmd.Parameters.AddWithValue("acctNum", (object?)account.AccountNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("inst", account.Institution ?? string.Empty);
        cmd.Parameters.AddWithValue("bal", (object?)account.Balance ?? DBNull.Value);
        cmd.Parameters.AddWithValue("ir", (object?)account.Details?.InterestRate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("appVal", (object?)account.Details?.AppraisedValue ?? DBNull.Value);
        if (DateTime.TryParse(account.Details?.MaturityDate, out var mat))
            cmd.Parameters.AddWithValue("mat", mat);
        else
            cmd.Parameters.AddWithValue("mat", DBNull.Value);
        cmd.Parameters.AddWithValue("addr", (object?)account.Details?.Address ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Account {AccountId} saved for family {FamilyId}", account.Id, familyId);
    }

    public async Task DeleteAccount(string familyId, string accountId)
    {
        _logger.LogInformation("Deleting account {AccountId} for family {FamilyId}", accountId, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!Guid.TryParse(familyId, out var fid) || !Guid.TryParse(accountId, out var aid))
        {
            _logger.LogWarning("Invalid familyId {FamilyId} or accountId {AccountId} when deleting", familyId, accountId);
            return;
        }

        if (!await FamilyExists(conn, fid))
        {
            return;
        }

        const string sql = "DELETE FROM accounts WHERE family_id::uuid=@fid AND id=@id";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fid", fid);
        cmd.Parameters.AddWithValue("id", aid);
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Deleted account {AccountId} for family {FamilyId}", accountId, familyId);
    }

    public Task ImportAccountsAndSnapshots(string familyId, List<ImportAccountEntry> entries) => throw new NotImplementedException();

    public async Task<List<Snapshot>> GetSnapshots(string familyId)
    {
        _logger.LogInformation("Fetching snapshots for family {FamilyId}", familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!Guid.TryParse(familyId, out var fid))
        {
            _logger.LogWarning("Invalid familyId {FamilyId} when fetching snapshots", familyId);
            return new List<Snapshot>();
        }

        if (!await FamilyExists(conn, fid))
        {
            return new List<Snapshot>();
        }

        const string sql = @"SELECT s.id, s.snapshot_date, s.net_worth, s.created_at,
                                    sa.account_id, sa.account_name, sa.value, sa.account_type
                             FROM snapshots s
                             LEFT JOIN snapshot_accounts sa ON s.id = sa.snapshot_id
                             WHERE s.family_id::uuid=@fid
                             ORDER BY s.snapshot_date";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fid", fid);
        await using var reader = await cmd.ExecuteReaderAsync();

        var map = new Dictionary<Guid, Snapshot>();
        while (await reader.ReadAsync())
        {
            var sid = reader.GetGuid(0);
            if (!map.TryGetValue(sid, out var snap))
            {
                snap = new Snapshot
                {
                    Id = sid.ToString(),
                    Date = reader.IsDBNull(1) ? DateTime.Now.ToString("yyyy-MM-dd") : reader.GetDateTime(1).ToUniversalTime().ToString("yyyy-MM-dd"),
                    NetWorth = reader.IsDBNull(2) ? 0 : (double)reader.GetDecimal(2),
                    CreatedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3).ToString("yyyy-MM-dd"),
                    Accounts = new List<SnapshotAccount>()
                };
                map[sid] = snap;
            }

            if (!reader.IsDBNull(4))
            {
                snap.Accounts.Add(new SnapshotAccount
                {
                    AccountId = reader.GetGuid(4).ToString(),
                    AccountName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Value = reader.IsDBNull(6) ? 0 : (double)reader.GetDecimal(6),
                    Type = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                });
            }
        }

        return map.Values.ToList();
    }

    public async Task SaveSnapshot(string familyId, Snapshot snapshot)
    {
        _logger.LogInformation("Saving snapshot {SnapshotId} for family {FamilyId}", snapshot.Id, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!Guid.TryParse(familyId, out var fid))
        {
            _logger.LogWarning("Invalid familyId {FamilyId} when saving snapshot", familyId);
            throw new ArgumentException("Invalid familyId");
        }

        if (!await FamilyExists(conn, fid))
        {
            return;
        }

        var sid = Guid.TryParse(snapshot.Id, out var s) ? s : Guid.NewGuid();
        snapshot.Id = sid.ToString();

        const string sql = @"INSERT INTO snapshots (id, family_id, snapshot_date, net_worth, created_at)
                             VALUES (@id,@fid,@date,@net_worth,now())
                             ON CONFLICT (id) DO UPDATE SET snapshot_date=EXCLUDED.snapshot_date, net_worth=EXCLUDED.net_worth";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", sid);
        cmd.Parameters.AddWithValue("fid", fid);
        cmd.Parameters.AddWithValue("date", snapshot.Date != null ? DateTime.Parse(snapshot.Date) : DateTime.UtcNow);
        cmd.Parameters.AddWithValue("net_worth", (decimal)snapshot.NetWorth);
        await cmd.ExecuteNonQueryAsync();

        const string delSql = "DELETE FROM snapshot_accounts WHERE snapshot_id=@sid";
        await using (var delCmd = new NpgsqlCommand(delSql, conn))
        {
            delCmd.Parameters.AddWithValue("sid", sid);
            await delCmd.ExecuteNonQueryAsync();
        }

        if (snapshot.Accounts != null)
        {
            const string insSql = @"INSERT INTO snapshot_accounts (snapshot_id, account_id, account_name, value, account_type)
                                     VALUES (@sid,@aid,@name,@value,@type::account_type)";
            foreach (var acc in snapshot.Accounts)
            {
                if (!Guid.TryParse(acc.AccountId, out var accountGuid))
                {
                    _logger.LogWarning("Skipping snapshot account with invalid id {AccountId}", acc.AccountId);
                    continue;
                }

                var accountType = acc.Type;
                if (string.IsNullOrWhiteSpace(accountType))
                {
                    _logger.LogWarning("Skipping snapshot account {AccountId} due to missing account type", acc.AccountId);
                    continue;
                }

                await using var insCmd = new NpgsqlCommand(insSql, conn);
                insCmd.Parameters.AddWithValue("sid", sid);
                insCmd.Parameters.AddWithValue("aid", accountGuid);
                insCmd.Parameters.AddWithValue("name", acc.AccountName ?? string.Empty);
                insCmd.Parameters.AddWithValue("value", (decimal)acc.Value);
                insCmd.Parameters.AddWithValue("type", accountType);
                await insCmd.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task DeleteSnapshot(string familyId, string snapshotId)
    {
        _logger.LogInformation("Deleting snapshot {SnapshotId} for family {FamilyId}", snapshotId, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!Guid.TryParse(familyId, out var fid) || !Guid.TryParse(snapshotId, out var sid))
        {
            _logger.LogWarning("Invalid ids when deleting snapshot: family {FamilyId} snapshot {SnapshotId}", familyId, snapshotId);
            return;
        }

        if (!await FamilyExists(conn, fid))
        {
            return;
        }

        const string sqlAccounts = "DELETE FROM snapshot_accounts WHERE snapshot_id=@sid";
        await using (var cmdAcc = new NpgsqlCommand(sqlAccounts, conn))
        {
            cmdAcc.Parameters.AddWithValue("sid", sid);
            await cmdAcc.ExecuteNonQueryAsync();
        }

        const string sqlSnap = "DELETE FROM snapshots WHERE family_id::uuid=@fid AND id=@sid";
        await using var cmd = new NpgsqlCommand(sqlSnap, conn);
        cmd.Parameters.AddWithValue("fid", fid);
        cmd.Parameters.AddWithValue("sid", sid);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task BatchDeleteSnapshots(string familyId, List<string> snapshotIds)
    {
        _logger.LogInformation("Batch deleting {Count} snapshots for family {FamilyId}", snapshotIds.Count, familyId);
        foreach (var sid in snapshotIds)
        {
            await DeleteSnapshot(familyId, sid);
        }
    }

    public async Task<bool> IsFamilyMember(string familyId, string userId)
    {
        _logger.LogInformation("Checking membership of user {UserId} in family {FamilyId}", userId, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!Guid.TryParse(familyId, out var fid))
        {
            _logger.LogWarning("Invalid familyId {FamilyId} when checking membership", familyId);
            return false;
        }

        const string sql = "SELECT 1 FROM family_members WHERE family_id::uuid=@fid AND user_id=@uid LIMIT 1";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fid", fid);
        cmd.Parameters.AddWithValue("uid", userId);
        var result = await cmd.ExecuteScalarAsync();
        var isMember = result != null;
        _logger.LogInformation("User {UserId} membership in family {FamilyId}: {IsMember}", userId, familyId, isMember);
        return isMember;
    }

    private async Task<bool> FamilyExists(NpgsqlConnection conn, Guid familyId)
    {
        const string sql = "SELECT 1 FROM families WHERE id=@fid LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fid", familyId);
        var exists = await cmd.ExecuteScalarAsync() != null;
        if (!exists)
        {
            _logger.LogWarning("Family not found: {FamilyId}", familyId);
        }
        return exists;
    }

    private static Account ReadAccount(Npgsql.NpgsqlDataReader reader)
    {
        var account = new Account
        {
            Id = reader.GetGuid(0).ToString(),
            UserId = reader.IsDBNull(1) ? null : reader.GetString(1),
            Name = reader.GetString(2),
            Type = reader.GetString(3),
            Category = reader.GetString(4),
            AccountNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
            Institution = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Balance = reader.IsDBNull(7) ? null : (double?)reader.GetDecimal(7),
            Details = new AccountDetails
            {
                InterestRate = reader.IsDBNull(8) ? null : (double?)reader.GetDecimal(8),
                AppraisedValue = reader.IsDBNull(9) ? null : (double?)reader.GetDecimal(9),
                MaturityDate = reader.IsDBNull(10) ? null : reader.GetDateTime(10).ToString("yyyy-MM-dd"),
                Address = reader.IsDBNull(11) ? null : reader.GetString(11)
            },
            CreatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(
                reader.IsDBNull(12) ? DateTime.UtcNow : reader.GetDateTime(12).ToUniversalTime()),
            UpdatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(
                reader.IsDBNull(13) ? DateTime.UtcNow : reader.GetDateTime(13).ToUniversalTime())
        };
        return account;
    }
}
