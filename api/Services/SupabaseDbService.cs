using Npgsql;

namespace FamilyBudgetApi.Services;

/// <summary>
/// Provides access to the Supabase/PostgreSQL database.
/// </summary>
public class SupabaseDbService
{
    private readonly string _connectionString;

    public SupabaseDbService(IConfiguration configuration)
    {
        _connectionString =
            Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION") ??
            configuration.GetConnectionString("Supabase") ??
            throw new InvalidOperationException("Supabase connection string not configured.");
    }

    /// <summary>
    /// Create and open a new NpgsqlConnection to the Supabase database.
    /// </summary>
    public async Task<NpgsqlConnection> GetOpenConnectionAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            CommandTimeout = 0
        };
        var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();
        return conn;
    }
}

