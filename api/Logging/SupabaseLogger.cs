using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using FamilyBudgetApi.Services;

namespace FamilyBudgetApi.Logging;

/// <summary>
/// Logger that writes entries to a Supabase/PostgreSQL table named "logs".
/// </summary>
public class SupabaseLogger : ILogger
{
    private readonly string _categoryName;
    private readonly SupabaseDbService _dbService;

    public SupabaseLogger(string categoryName, SupabaseDbService dbService)
    {
        _categoryName = categoryName;
        _dbService = dbService;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        _ = WriteLogAsync(logLevel, message, exception);
    }

    private async Task WriteLogAsync(LogLevel level, string message, Exception exception)
    {
        try
        {
            await using var conn = await _dbService.GetOpenConnectionAsync();
            const string sql = "INSERT INTO logs (timestamp, level, category, message, exception) VALUES (@timestamp, @level, @category, @message, @exception);";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@level", level.ToString());
            cmd.Parameters.AddWithValue("@category", _categoryName);
            cmd.Parameters.AddWithValue("@message", message);
            cmd.Parameters.AddWithValue("@exception", (object?)exception?.ToString() ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Suppress errors when the logs table is missing
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing log to Supabase: {ex.Message}");
        }
    }
}
