using Microsoft.Extensions.Logging;

namespace FamilyBudgetApi.Logging;

public class CustomGoogleLoggerProvider : ILoggerProvider
{
    private readonly string _projectId;

    public CustomGoogleLoggerProvider(string projectId)
    {
        _projectId = projectId;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CustomGoogleLogger(_projectId, categoryName);
    }

    public void Dispose() { }
}