using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FamilyBudgetApi.Services;

/// <summary>
/// User operations backed by Supabase/PostgreSQL.
/// </summary>
public class UserService
{
    private readonly SupabaseDbService _db;
    private readonly ILogger<UserService> _logger;
    private bool? _hasEmailVerificationTokensTable;
    private readonly SemaphoreSlim _verificationTableCheckLock = new(1, 1);

    public UserService(SupabaseDbService db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UserData?> GetUser(string userId)
    {
        _logger.LogInformation("Fetching user {UserId}", userId);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "SELECT uid, email FROM users WHERE uid=@uid";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return null;
        }
        return new UserData { Uid = reader.GetString(0), Email = reader.GetString(1) };
    }

    public async Task<UserData?> GetUserByEmail(string email)
    {
        _logger.LogInformation("Fetching user by email {Email}", email);
        await using var conn = await _db.GetOpenConnectionAsync();
        const string sql = "SELECT uid, email FROM users WHERE email=@e";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("e", email);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            _logger.LogWarning("User with email {Email} not found", email);
            return null;
        }
        return new UserData { Uid = reader.GetString(0), Email = reader.GetString(1) };
    }

    public async Task SaveUser(string userId, UserData userData)
    {
        _logger.LogInformation("Saving user {UserId}", userId);
        await using var conn = await _db.GetOpenConnectionAsync();
        var email = userData.Email ?? string.Empty;
        const string sql = @"INSERT INTO users (uid, email, created_at, updated_at)
            VALUES (@uid,@email, now(), now())
            ON CONFLICT (uid) DO UPDATE SET
            email=CASE WHEN EXCLUDED.email = '' THEN users.email ELSE EXCLUDED.email END,
            updated_at=now()";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("email", email);
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("User {UserId} saved", userId);
    }

    public Task SaveUser(string userId, string? email) =>
        SaveUser(userId, new UserData
        {
            Uid = userId,
            Email = email ?? string.Empty
        });

    public async Task<string?> GetVerificationToken(string userId)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!await EnsureEmailVerificationTokensTable(conn))
        {
            _logger.LogWarning("Verification token table unavailable when reading token for user {UserId}", userId);
            return null;
        }

        const string sql = "SELECT token FROM email_verification_tokens WHERE user_id=@uid LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);
        var token = await cmd.ExecuteScalarAsync();
        return token?.ToString();
    }

    public async Task<string> EnsureVerificationToken(string userId)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!await EnsureEmailVerificationTokensTable(conn))
        {
            throw new InvalidOperationException("Email verification token storage is not available.");
        }

        const string selectSql = "SELECT token FROM email_verification_tokens WHERE user_id=@uid LIMIT 1";
        await using (var selectCmd = new NpgsqlCommand(selectSql, conn))
        {
            selectCmd.Parameters.AddWithValue("uid", userId);
            var existing = await selectCmd.ExecuteScalarAsync();
            if (existing != null && existing != DBNull.Value)
            {
                return existing.ToString()!;
            }
        }

        var token = Guid.NewGuid().ToString();
        const string upsertSql = @"
INSERT INTO email_verification_tokens (user_id, token, created_at, updated_at)
VALUES (@uid, @token, NOW(), NOW())
ON CONFLICT (user_id) DO UPDATE SET token=EXCLUDED.token, updated_at=NOW()";
        await using (var upsertCmd = new NpgsqlCommand(upsertSql, conn))
        {
            upsertCmd.Parameters.AddWithValue("uid", userId);
            upsertCmd.Parameters.AddWithValue("token", token);
            await upsertCmd.ExecuteNonQueryAsync();
        }

        return token;
    }

    public async Task<string?> GetUserIdByVerificationToken(string token)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!await EnsureEmailVerificationTokensTable(conn))
        {
            _logger.LogWarning("Verification token table unavailable when looking up token");
            return null;
        }

        const string sql = "SELECT user_id FROM email_verification_tokens WHERE token=@token LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("token", token);
        var userId = await cmd.ExecuteScalarAsync();
        return userId?.ToString();
    }

    public async Task ClearVerificationToken(string userId)
    {
        await using var conn = await _db.GetOpenConnectionAsync();
        if (!await EnsureEmailVerificationTokensTable(conn))
        {
            _logger.LogWarning("Verification token table unavailable when clearing token for user {UserId}", userId);
            return;
        }

        const string sql = "DELETE FROM email_verification_tokens WHERE user_id=@uid";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<bool> EnsureEmailVerificationTokensTable(NpgsqlConnection conn)
    {
        if (_hasEmailVerificationTokensTable.HasValue)
        {
            return _hasEmailVerificationTokensTable.Value;
        }

        await _verificationTableCheckLock.WaitAsync();
        try
        {
            if (_hasEmailVerificationTokensTable.HasValue)
            {
                return _hasEmailVerificationTokensTable.Value;
            }

            const string createSql = @"
CREATE TABLE IF NOT EXISTS email_verification_tokens (
  user_id TEXT PRIMARY KEY,
  token TEXT NOT NULL UNIQUE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_email_verification_tokens_token
  ON email_verification_tokens(token);";

            await using var cmd = new NpgsqlCommand(createSql, conn);
            await cmd.ExecuteNonQueryAsync();
            _hasEmailVerificationTokensTable = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize email_verification_tokens table");
            _hasEmailVerificationTokensTable = false;
            return false;
        }
        finally
        {
            _verificationTableCheckLock.Release();
        }
    }
}
