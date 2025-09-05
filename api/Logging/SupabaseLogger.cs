using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using FamilyBudgetApi.Services;

namespace FamilyBudgetApi.Logging;

/// <summary>
/// Logger that writes entries to a Supabase/PostgreSQL table named "logs".
/// </summary>
public class SupabaseLogger : ILogger
{
    private readonly string _categoryName;
    private readonly SupabaseDbService _dbService;
    private bool _loggingDisabled;
    private static bool _globalLoggingDisabled;

    public SupabaseLogger(string categoryName, SupabaseDbService dbService)
    {
        _categoryName = categoryName;
        _dbService = dbService;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        _ = WriteLogAsync(logLevel, message, exception);
    }

    private async Task WriteLogAsync(LogLevel level, string message, Exception? exception)
    {
        if (_loggingDisabled || _globalLoggingDisabled)
        {
            WriteToConsole(level, message, exception);
            return;
        }

        try
        {
            await using var conn = await _dbService.GetOpenConnectionAsync();
            const string sql = "INSERT INTO logs (timestamp, level, category, message, exception) VALUES (@timestamp, @level, @category, @message, @exception);";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@level", level.ToString());
            cmd.Parameters.AddWithValue("@category", _categoryName);
            cmd.Parameters.AddWithValue("@message", message);
            cmd.Parameters.Add("@exception", NpgsqlDbType.Text).Value = (object?)exception?.ToString() ?? DBNull.Value;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // If the logs table is missing, write the entry to console instead
            WriteToConsole(level, message, exception);
        }
        catch (Exception ex)
        {
            _loggingDisabled = true;
            _globalLoggingDisabled = true;
            Console.WriteLine($"Error writing log to Supabase: {ex.Message}");
            WriteToConsole(level, message, exception);
        }
    }

    private void WriteToConsole(LogLevel level, string message, Exception? exception)
    {
        var ts = DateTime.UtcNow.ToString("o");
        var line = $"[{ts}] {level} {_categoryName}: {message}";
        if (exception != null)
            line += $"\n{exception}";
        Console.WriteLine(line);
    }
}
