using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Persistence boundary for fields required by legacy account authentication.
/// Returned credential buffers are transferred to the caller for zeroization.
/// </summary>
public interface IAccountAuthenticationRepository
{
    ValueTask<AccountAuthenticationRecord?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken);

    ValueTask SetActiveAsync(
        AccountId accountId,
        bool active,
        CancellationToken cancellationToken);

    ValueTask ResetAllActiveAsync(CancellationToken cancellationToken);
}
