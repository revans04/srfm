using Microsoft.Extensions.Logging;
using FamilyBudgetApi.Services;

namespace FamilyBudgetApi.Logging;

/// <summary>
/// Logger provider that creates <see cref="SupabaseLogger"/> instances.
/// </summary>
public class SupabaseLoggerProvider : ILoggerProvider
{
    private readonly SupabaseDbService _dbService;

    public SupabaseLoggerProvider(SupabaseDbService dbService)
    {
        _dbService = dbService;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SupabaseLogger(categoryName, _dbService);
    }

    public void Dispose() { }
}
