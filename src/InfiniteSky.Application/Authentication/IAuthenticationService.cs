namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Application-level authentication and connection lifecycle boundary.
/// </summary>
public interface IAuthenticationService
{
    ValueTask<AuthenticationResult> AuthenticateAsync(
        Guid connectionId,
        string username,
        string password,
        CancellationToken cancellationToken);

    void ConnectionClosed(Guid connectionId);

    AuthenticationConnectionState? GetConnectionState(Guid connectionId);
}
