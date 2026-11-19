using InfiniteSky.Application.Authentication;
using InfiniteSky.Domain.Identifiers;

using Microsoft.Extensions.Logging;

namespace InfiniteSky.Server.Authentication;

/// <summary>
/// Explicit placeholder until the MongoDB account repository is delivered in Prompt 12.
/// It never authenticates an account.
/// </summary>
public sealed partial class UnavailableAccountAuthenticationRepository
    : IAccountAuthenticationRepository
{
    private readonly ILogger<UnavailableAccountAuthenticationRepository> _logger;

    public UnavailableAccountAuthenticationRepository(
        ILogger<UnavailableAccountAuthenticationRepository> logger)
    {
        _logger = logger;
        LogUnavailable(_logger);
    }

    public ValueTask<AccountAuthenticationRecord?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new AccountRepositoryException(
            "Production account lookup is unavailable until the persistence stage is implemented.");
    }

    public ValueTask SetActiveAsync(
        AccountId accountId,
        bool active,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new AccountRepositoryException(
            "Production account updates are unavailable until the persistence stage is implemented.");
    }

    [LoggerMessage(
        EventId = 4010,
        Level = LogLevel.Warning,
        Message = "Authentication is non-operational: no production account repository is configured.")]
    private static partial void LogUnavailable(ILogger logger);
}
