using FamilyBudgetApi.Models;
using Microsoft.Extensions.Logging;

namespace FamilyBudgetApi.Services;

/// <summary>
/// User operations backed by Supabase/PostgreSQL.
/// </summary>
public class UserService
{
    private readonly SupabaseDbService _db;
    private readonly ILogger<UserService> _logger;

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
        const string sql = @"INSERT INTO users (uid, email)
            VALUES (@uid,@email)
            ON CONFLICT (uid) DO UPDATE SET email=EXCLUDED.email";
        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("email", userData.Email ?? string.Empty);
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("User {UserId} saved", userId);
    }

}

