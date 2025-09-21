using Microsoft.Extensions.Logging;

namespace FamilyBudgetApi.Logging;

/// <summary>
/// Logger provider that creates <see cref="SupabaseLogger"/> instances.
/// </summary>
public class SupabaseLoggerProvider : ILoggerProvider
{
    private readonly SupabaseLogQueue _queue;

    public SupabaseLoggerProvider(SupabaseLogQueue queue)
    {
        _queue = queue;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SupabaseLogger(categoryName, _queue);
    }

    public void Dispose() { }
}
