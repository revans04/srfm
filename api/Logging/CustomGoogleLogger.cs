using Google.Api;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Microsoft.Extensions.Logging;
using System;

namespace FamilyBudgetApi.Logging;

public class CustomGoogleLogger : ILogger
{
    private readonly LoggingServiceV2Client _client;
    private readonly string _projectId;
    private readonly string _categoryName;

    public CustomGoogleLogger(string projectId, string categoryName)
    {
        _projectId = projectId;
        _categoryName = categoryName;
        _client = LoggingServiceV2Client.Create();
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var logEntry = new LogEntry
        {
            LogName = $"projects/{_projectId}/logs/{_categoryName.Replace(".", "_")}",
            Severity = logLevel switch
            {
                LogLevel.Information => LogSeverity.Info,
                LogLevel.Warning => LogSeverity.Warning,
                LogLevel.Error => LogSeverity.Error,
                LogLevel.Critical => LogSeverity.Critical,
                _ => LogSeverity.Default
            },
            TextPayload = $"{formatter(state, exception)}{(exception != null ? $"\nException: {exception}" : "")}",
            Resource = new MonitoredResource { Type = "global" }
        };

        try
        {
            var request = new WriteLogEntriesRequest
            {
                LogName = logEntry.LogName,
                Resource = logEntry.Resource,
                Entries = { logEntry }
            };
            _client.WriteLogEntries(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to Google Cloud Logging: {ex.Message}");
        }
    }
}