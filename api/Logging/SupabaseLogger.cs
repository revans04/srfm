using System;
using Microsoft.Extensions.Logging;

namespace FamilyBudgetApi.Logging;

/// <summary>
/// Logger that writes entries to a Supabase/PostgreSQL table named "logs".
/// </summary>
public class SupabaseLogger : ILogger
{
    private readonly string _categoryName;
    private readonly SupabaseLogQueue _queue;

    public SupabaseLogger(string categoryName, SupabaseLogQueue queue)
    {
        _categoryName = categoryName;
        _queue = queue;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var entry = new SupabaseLogEntry(DateTime.UtcNow, logLevel.ToString(), _categoryName, message, exception?.ToString());

        if (!_queue.TryWrite(entry))
        {
            WriteToConsole(logLevel, message, exception);
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
