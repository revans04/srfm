using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;

namespace FamilyBudgetApi.Logging;

public class CustomGoogleLoggerProvider : ILoggerProvider
{
    private readonly string _projectId;
    private readonly GoogleCredential _credential;

    public CustomGoogleLoggerProvider(string projectId, GoogleCredential credential)
    {
        _projectId = projectId;
        _credential = credential;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CustomGoogleLogger(_projectId, categoryName, _credential);
    }

    public void Dispose() { }
}