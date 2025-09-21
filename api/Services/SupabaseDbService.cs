using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace FamilyBudgetApi.Services;

/// <summary>
/// Provides pooled access to the Supabase/PostgreSQL database.
/// </summary>
public class SupabaseDbService : IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public SupabaseDbService(IConfiguration configuration)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION") ??
            configuration.GetConnectionString("Supabase") ??
            throw new InvalidOperationException("Supabase connection string not configured.");

        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.ConnectionStringBuilder.CommandTimeout = 30;

        _dataSource = builder.Build();
    }

    /// <summary>
    /// Create and open a pooled NpgsqlConnection to the Supabase database.
    /// </summary>
    public ValueTask<NpgsqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default) =>
        _dataSource.OpenConnectionAsync(cancellationToken);

    public NpgsqlCommand CreateCommand(string sql)
    {
        var command = _dataSource.CreateCommand(sql);
        if (command.CommandTimeout == 0)
        {
            command.CommandTimeout = 30;
        }
        return command;
    }

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
