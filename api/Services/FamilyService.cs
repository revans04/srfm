using FamilyBudgetApi.Models;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace FamilyBudgetApi.Services;

/// <summary>
/// Family operations backed by Supabase/PostgreSQL.
/// Only the queries required by other services are implemented for now.
/// </summary>
public class FamilyService
{
    private readonly SupabaseDbService _db;
    private readonly ILogger<FamilyService> _logger;

    public FamilyService(SupabaseDbService db, ILogger<FamilyService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve the family for which the given user is a member.
    /// </summary>
    public async Task<Family?> GetUserFamily(string uid)
    {
        _logger.LogInformation("Retrieving family for user {UserId}", uid);
        await using var conn = await _db.GetOpenConnectionAsync();

        const string sqlFamilyId =
            "SELECT family_id FROM family_members WHERE user_id=@uid LIMIT 1";
        await using var idCmd = new Npgsql.NpgsqlCommand(sqlFamilyId, conn);
        idCmd.Parameters.AddWithValue("uid", uid);
        var familyIdObj = await idCmd.ExecuteScalarAsync();
        if (familyIdObj is not Guid familyId)
        {
            _logger.LogWarning("No family found for user {UserId}", uid);
            return null;
        }

        return await GetFamilyById(familyId.ToString());
    }

    /// <summary>
    /// Fetch a family by its identifier including members, accounts, snapshots, and entities.
    /// </summary>
    public async Task<Family?> GetFamilyById(string familyId)
    {
        _logger.LogInformation("Fetching family {FamilyId}", familyId);
        await using var conn = await _db.GetOpenConnectionAsync();

        if (!Guid.TryParse(familyId, out var fid))
        {
            _logger.LogWarning("Invalid familyId {FamilyId}", familyId);
            return null;
        }

        Family family;

        const string sqlFamily =
            "SELECT id, name, owner_uid, created_at, updated_at FROM families WHERE id=@id";
        await using (var familyCmd = new Npgsql.NpgsqlCommand(sqlFamily, conn))
        {
            familyCmd.Parameters.AddWithValue("id", fid);
            await using var reader = await familyCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                _logger.LogWarning("Family {FamilyId} not found", familyId);
                return null;
            }

            family = new Family
            {
                Id = reader.GetGuid(0).ToString(),
                Name = reader.GetString(1),
                OwnerUid = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Members = new List<UserRef>(),
                MemberUids = new List<string>(),
                Accounts = new List<Account>(),
                Snapshots = new List<Snapshot>(),
                Entities = new List<Entity>(),
                CreatedAt = Timestamp.FromDateTime(reader.IsDBNull(3) ? DateTime.UtcNow : reader.GetDateTime(3).ToUniversalTime()),
                UpdatedAt = Timestamp.FromDateTime(reader.IsDBNull(4) ? DateTime.UtcNow : reader.GetDateTime(4).ToUniversalTime())
            };
        }

        const string sqlMembers =
            "SELECT user_id, role FROM family_members WHERE family_id=@id";
        await using (var memCmd = new Npgsql.NpgsqlCommand(sqlMembers, conn))
        {
            memCmd.Parameters.AddWithValue("id", fid);
            await using var memReader = await memCmd.ExecuteReaderAsync();
            while (await memReader.ReadAsync())
            {
                var member = new UserRef
                {
                    Uid = memReader.GetString(0),
                    Role = memReader.IsDBNull(1) ? null : memReader.GetString(1)
                };
                family.Members.Add(member);
                if (member.Uid != null) family.MemberUids.Add(member.Uid);
            }
        }

        // Load accounts
        const string sqlAccounts =
            @"SELECT id, user_id, name, type, category, account_number, institution,
                     balance, interest_rate, appraised_value, maturity_date, address,
                     created_at, updated_at
              FROM accounts WHERE family_id=@fid";
        await using (var accCmd = new Npgsql.NpgsqlCommand(sqlAccounts, conn))
        {
            accCmd.Parameters.AddWithValue("fid", fid);
            await using var accReader = await accCmd.ExecuteReaderAsync();
            while (await accReader.ReadAsync())
            {
                var account = new Account
                {
                    Id = accReader.GetGuid(0).ToString(),
                    UserId = accReader.IsDBNull(1) ? null : accReader.GetString(1),
                    Name = accReader.GetString(2),
                    Type = accReader.GetString(3),
                    Category = accReader.GetString(4),
                    AccountNumber = accReader.IsDBNull(5) ? null : accReader.GetString(5),
                    Institution = accReader.IsDBNull(6) ? string.Empty : accReader.GetString(6),
                    Balance = accReader.IsDBNull(7) ? null : (double?)accReader.GetDecimal(7),
                    Details = new AccountDetails
                    {
                        InterestRate = accReader.IsDBNull(8) ? null : (double?)accReader.GetDecimal(8),
                        AppraisedValue = accReader.IsDBNull(9) ? null : (double?)accReader.GetDecimal(9),
                        MaturityDate = accReader.IsDBNull(10) ? null : accReader.GetDateTime(10).ToString("yyyy-MM-dd"),
                        Address = accReader.IsDBNull(11) ? null : accReader.GetString(11)
                    },
                    CreatedAt = Timestamp.FromDateTime(accReader.IsDBNull(12) ? DateTime.UtcNow : accReader.GetDateTime(12).ToUniversalTime()),
                    UpdatedAt = Timestamp.FromDateTime(accReader.IsDBNull(13) ? DateTime.UtcNow : accReader.GetDateTime(13).ToUniversalTime())
                };
                family.Accounts.Add(account);
            }
        }

        // Load entities
        const string sqlEntities =
            "SELECT id, name, type, created_at, updated_at FROM entities WHERE family_id=@fid";
        await using (var entCmd = new Npgsql.NpgsqlCommand(sqlEntities, conn))
        {
            entCmd.Parameters.AddWithValue("fid", fid);
            await using var entReader = await entCmd.ExecuteReaderAsync();
            while (await entReader.ReadAsync())
            {
                family.Entities.Add(new Entity
                {
                    Id = entReader.GetGuid(0).ToString(),
                    Name = entReader.GetString(1),
                    Type = entReader.GetString(2),
                    CreatedAt = Timestamp.FromDateTime(entReader.IsDBNull(3) ? DateTime.UtcNow : entReader.GetDateTime(3).ToUniversalTime()),
                    UpdatedAt = Timestamp.FromDateTime(entReader.IsDBNull(4) ? DateTime.UtcNow : entReader.GetDateTime(4).ToUniversalTime()),
                    Members = new List<UserRef>()
                });
            }
        }

        // Load snapshots and their accounts in a single query to avoid nested readers
        const string sqlSnapshots = @"
            SELECT s.id, s.snapshot_date, s.net_worth, s.created_at,
                   sa.account_id, sa.account_name, sa.value, sa.account_type
            FROM snapshots s
            LEFT JOIN snapshot_accounts sa ON s.id = sa.snapshot_id
            WHERE s.family_id=@fid
            ORDER BY s.snapshot_date";
        await using (var snapCmd = new Npgsql.NpgsqlCommand(sqlSnapshots, conn))
        {
            snapCmd.Parameters.AddWithValue("fid", fid);
            await using var snapReader = await snapCmd.ExecuteReaderAsync();
            var snapMap = new Dictionary<Guid, Snapshot>();
            while (await snapReader.ReadAsync())
            {
                var sid = snapReader.GetGuid(0);
                if (!snapMap.TryGetValue(sid, out var snap))
                {
                    snap = new Snapshot
                    {
                        Id = sid.ToString(),
                        Date = snapReader.IsDBNull(1) ? DateTime.Now.ToString("yyyy-MM-dd") : snapReader.GetDateTime(1).ToUniversalTime().ToString("yyyy-MM-dd"),
                        NetWorth = snapReader.IsDBNull(2) ? 0 : (double)snapReader.GetDecimal(2),
                        CreatedAt = snapReader.IsDBNull(3) ? null : snapReader.GetDateTime(3).ToString("yyyy-MM-dd"),
                        Accounts = new List<SnapshotAccount>()
                    };
                    snapMap[sid] = snap;
                    family.Snapshots.Add(snap);
                }
                if (!snapReader.IsDBNull(4))
                {
                    snap.Accounts.Add(new SnapshotAccount
                    {
                        AccountId = snapReader.GetGuid(4).ToString(),
                        AccountName = snapReader.IsDBNull(5) ? string.Empty : snapReader.GetString(5),
                        Value = snapReader.IsDBNull(6) ? 0 : (double)snapReader.GetDecimal(6),
                        Type = snapReader.IsDBNull(7) ? string.Empty : snapReader.GetString(7)
                    });
                }
            }
        }

        _logger.LogInformation(
            "Loaded family {FamilyId} with {MemberCount} members, {AccountCount} accounts, {SnapshotCount} snapshots, {EntityCount} entities",
            familyId, family.Members.Count, family.Accounts.Count, family.Snapshots.Count, family.Entities.Count);
        return family;
    }

    public async Task CreateFamily(string familyId, Family family)
    {
        _logger.LogInformation("Creating family {FamilyId}", familyId);
        await using var conn = await _db.GetOpenConnectionAsync();

        const string sql = "INSERT INTO families (id, name, owner_uid, created_at, updated_at) VALUES (@id, @name, @owner, now(), now())";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.Parse(familyId));
        cmd.Parameters.AddWithValue("name", family.Name);
        cmd.Parameters.AddWithValue("owner", (object?)family.OwnerUid ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();

        if (family.Members != null)
        {
            foreach (var member in family.Members)
            {
                const string sqlMember = "INSERT INTO family_members (family_id, user_id, role) VALUES (@fid, @uid, @role)";
                await using var memCmd = new Npgsql.NpgsqlCommand(sqlMember, conn);
                memCmd.Parameters.AddWithValue("fid", Guid.Parse(familyId));
                memCmd.Parameters.AddWithValue("uid", member.Uid);
                memCmd.Parameters.AddWithValue("role", (object?)member.Role ?? DBNull.Value);
                await memCmd.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task AddFamilyMember(string familyId, UserRef member)
    {
        _logger.LogInformation("Adding member {Uid} to family {FamilyId}", member.Uid, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "INSERT INTO family_members (family_id, user_id, role) VALUES (@fid, @uid, @role)";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fid", Guid.Parse(familyId));
        cmd.Parameters.AddWithValue("uid", member.Uid);
        cmd.Parameters.AddWithValue("role", (object?)member.Role ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveFamilyMember(string familyId, string memberUid)
    {
        _logger.LogInformation("Removing member {Uid} from family {FamilyId}", memberUid, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "DELETE FROM family_members WHERE family_id=@fid AND user_id=@uid";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fid", Guid.Parse(familyId));
        cmd.Parameters.AddWithValue("uid", memberUid);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RenameFamily(string familyId, string newName)
    {
        _logger.LogInformation("Renaming family {FamilyId} to {Name}", familyId, newName);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "UPDATE families SET name=@name, updated_at=NOW() WHERE id=@id";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.Parse(familyId));
        cmd.Parameters.AddWithValue("name", newName);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CreateEntity(string familyId, Entity entity)
    {
        _logger.LogInformation("Creating entity {EntityId} for family {FamilyId}", entity.Id, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "INSERT INTO entities (id, family_id, name, type, created_at, updated_at) VALUES (@id, @fid, @name, @type, now(), now())";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.Parse(entity.Id));
        cmd.Parameters.AddWithValue("fid", Guid.Parse(familyId));
        cmd.Parameters.AddWithValue("name", entity.Name);
        cmd.Parameters.AddWithValue("type", entity.Type);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateEntity(string familyId, Entity entity)
    {
        _logger.LogInformation("Updating entity {EntityId} for family {FamilyId}", entity.Id, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "UPDATE entities SET name=@name, type=@type, updated_at=NOW() WHERE id=@id AND family_id=@fid";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.Parse(entity.Id));
        cmd.Parameters.AddWithValue("fid", Guid.Parse(familyId));
        cmd.Parameters.AddWithValue("name", entity.Name);
        cmd.Parameters.AddWithValue("type", entity.Type);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteEntity(string familyId, string entityId)
    {
        _logger.LogInformation("Deleting entity {EntityId} for family {FamilyId}", entityId, familyId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "DELETE FROM entities WHERE id=@id AND family_id=@fid";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.Parse(entityId));
        cmd.Parameters.AddWithValue("fid", Guid.Parse(familyId));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddEntityMember(string familyId, string entityId, UserRef member)
    {
        _logger.LogInformation("Adding member {Uid} to entity {EntityId}", member.Uid, entityId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "INSERT INTO entity_members (entity_id, user_id) VALUES (@eid, @uid)";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("eid", Guid.Parse(entityId));
        cmd.Parameters.AddWithValue("uid", member.Uid);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveEntityMember(string familyId, string entityId, string memberUid)
    {
        _logger.LogInformation("Removing member {Uid} from entity {EntityId}", memberUid, entityId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "DELETE FROM entity_members WHERE entity_id=@eid AND user_id=@uid";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("eid", Guid.Parse(entityId));
        cmd.Parameters.AddWithValue("uid", memberUid);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CreatePendingInvite(PendingInvite invite)
    {
        _logger.LogInformation("Creating pending invite for {Invitee}", invite.InviteeEmail);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = @"INSERT INTO pending_invites (token, inviter_uid, inviter_email, invitee_email, created_at, expires_at)
                             VALUES (@token, @inviter_uid, @inviter_email, @invitee_email, @created_at, @expires_at)";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("token", invite.Token);
        cmd.Parameters.AddWithValue("inviter_uid", invite.InviterUid);
        cmd.Parameters.AddWithValue("inviter_email", invite.InviterEmail);
        cmd.Parameters.AddWithValue("invitee_email", invite.InviteeEmail);
        cmd.Parameters.AddWithValue("created_at", invite.CreatedAt.ToDateTime());
        cmd.Parameters.AddWithValue("expires_at", invite.ExpiresAt.ToDateTime());
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<PendingInvite?> GetPendingInviteByToken(string token)
    {
        _logger.LogInformation("Fetching pending invite {Token}", token);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = @"SELECT inviter_uid, inviter_email, invitee_email, token, created_at, expires_at
                             FROM pending_invites WHERE token=@token";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("token", token);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }
        return new PendingInvite
        {
            InviterUid = reader.GetString(0),
            InviterEmail = reader.GetString(1),
            InviteeEmail = reader.GetString(2),
            Token = reader.GetString(3),
            CreatedAt = Timestamp.FromDateTime(reader.IsDBNull(4) ? DateTime.UtcNow : reader.GetDateTime(4).ToUniversalTime()),
            ExpiresAt = Timestamp.FromDateTime(reader.IsDBNull(5) ? DateTime.UtcNow : reader.GetDateTime(5).ToUniversalTime())
        };
    }

    public async Task DeletePendingInvite(string token)
    {
        _logger.LogInformation("Deleting pending invite {Token}", token);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "DELETE FROM pending_invites WHERE token=@token";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("token", token);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<PendingInvite>> GetPendingInvitesByInviter(string inviterUid)
    {
        _logger.LogInformation("Fetching pending invites for inviter {Inviter}", inviterUid);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = @"SELECT inviter_uid, inviter_email, invitee_email, token, created_at, expires_at
                             FROM pending_invites WHERE inviter_uid=@uid";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", inviterUid);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<PendingInvite>();
        while (await reader.ReadAsync())
        {
            list.Add(new PendingInvite
            {
                InviterUid = reader.GetString(0),
                InviterEmail = reader.GetString(1),
                InviteeEmail = reader.GetString(2),
                Token = reader.GetString(3),
                CreatedAt = Timestamp.FromDateTime(reader.IsDBNull(4) ? DateTime.UtcNow : reader.GetDateTime(4).ToUniversalTime()),
                ExpiresAt = Timestamp.FromDateTime(reader.IsDBNull(5) ? DateTime.UtcNow : reader.GetDateTime(5).ToUniversalTime())
            });
        }
        return list;
    }

    public async Task UpdateLastAccessed(string uid)
    {
        _logger.LogInformation("Updating last accessed for user {Uid}", uid);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "UPDATE users SET last_accessed = NOW() WHERE uid=@uid";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", uid);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Timestamp?> GetLastAccessed(string uid)
    {
        _logger.LogInformation("Getting last accessed for user {Uid}", uid);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "SELECT last_accessed FROM users WHERE uid=@uid";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", uid);
        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value) return null;
        return Timestamp.FromDateTime(((DateTime)result).ToUniversalTime());
    }
}
