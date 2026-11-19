using InfiniteSky.Application.Authentication;
using InfiniteSky.Application.Characters;
using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;
using InfiniteSky.World.Players;
using InfiniteSky.World.Zones;

namespace InfiniteSky.Application.Players;

/// <summary>
/// Enforces authenticated account ownership before making a character active in a running zone.
/// </summary>
public sealed class PlayerActivationService(
    IAuthenticationSessionStore sessions,
    ICharacterRepository characters,
    IPlayerManager players,
    IZoneManager zones,
    PlayerMetrics metrics,
    string serverName)
{
    public async ValueTask<IReadOnlyList<Character>> GetCharactersAsync(
        Guid loginConnectionId,
        CancellationToken cancellationToken)
    {
        AuthenticatedSession session = RequireSession(loginConnectionId);
        IReadOnlyList<Character> result = await characters.GetForAccountAsync(
            session.AccountId,
            serverName,
            cancellationToken).ConfigureAwait(false);

        EnsureSessionIsCurrent(loginConnectionId, session.SessionId);
        return result;
    }

    public async ValueTask<PlayerRuntime> ActivateAsync(
        Guid loginConnectionId,
        Guid worldConnectionId,
        CharacterId characterId,
        CancellationToken cancellationToken)
    {
        if (worldConnectionId == Guid.Empty)
        {
            throw new ArgumentException("A world connection ID is required.", nameof(worldConnectionId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        AuthenticatedSession session = RequireSession(loginConnectionId);
        Character? character = await characters.GetOwnedAsync(
            characterId,
            session.AccountId,
            serverName,
            cancellationToken).ConfigureAwait(false);
        if (character is null)
        {
            metrics.RecordActivationFailure();
            throw new PlayerActivationException(PlayerActivationFailure.CharacterUnavailable);
        }

        if (character.AccountId != session.AccountId
            || !string.Equals(character.ServerName, serverName, StringComparison.Ordinal))
        {
            metrics.RecordActivationFailure();
            throw new PlayerActivationException(PlayerActivationFailure.CharacterUnavailable);
        }

        EnsureSessionIsCurrent(loginConnectionId, session.SessionId);
        if (character.SavedLocation.ZoneId is not ZoneId zoneId
            || character.SavedLocation.Position is not Position3 position)
        {
            metrics.RecordActivationFailure();
            throw new PlayerActivationException(PlayerActivationFailure.SavedLocationUnavailable);
        }

        ZoneRuntime? zone = zones.Find(zoneId);
        if (zone is null || zone.State != ZoneLifecycleState.Running)
        {
            metrics.RecordActivationFailure();
            throw new PlayerActivationException(PlayerActivationFailure.ZoneUnavailable);
        }

        cancellationToken.ThrowIfCancellationRequested();
        PlayerRuntime player = players.Activate(
            character,
            session.SessionId,
            worldConnectionId,
            zone);
        if (cancellationToken.IsCancellationRequested)
        {
            _ = players.RemoveByWorldConnection(worldConnectionId);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (!sessions.TryGetByConnection(loginConnectionId, out AuthenticatedSession? currentSession)
            || currentSession is null
            || currentSession.SessionId != session.SessionId)
        {
            _ = players.RemoveByWorldConnection(worldConnectionId);
            metrics.RecordActivationFailure();
            throw new PlayerActivationException(PlayerActivationFailure.AuthenticationExpired);
        }

        return player;
    }

    public async ValueTask<PlayerRuntimeSnapshot?> DisconnectAsync(
        Guid worldConnectionId,
        CancellationToken cancellationToken)
    {
        PlayerRuntimeSnapshot? player = players.RemoveByWorldConnection(worldConnectionId);
        if (player is null)
        {
            return null;
        }

        try
        {
            await characters.SaveWorldLocationAsync(
                player.CharacterId,
                player.AccountId,
                serverName,
                player.ZoneId,
                player.Position,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            metrics.RecordSaveFailure();
            throw;
        }

        return player;
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PlayerRuntimeSnapshot> playersToSave = players.RemoveAll();
        List<Exception>? failures = null;
        foreach (PlayerRuntimeSnapshot player in playersToSave)
        {
            try
            {
                await characters.SaveWorldLocationAsync(
                    player.CharacterId,
                    player.AccountId,
                    serverName,
                    player.ZoneId,
                    player.Position,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                metrics.RecordSaveFailure();
                (failures ??= []).Add(exception);
            }
        }

        if (failures is not null)
        {
            throw new AggregateException("One or more player locations could not be saved during shutdown.", failures);
        }
    }

    private AuthenticatedSession RequireSession(Guid loginConnectionId)
    {
        if (loginConnectionId == Guid.Empty
            || !sessions.TryGetByConnection(loginConnectionId, out AuthenticatedSession? session)
            || session is null)
        {
            metrics.RecordActivationFailure();
            throw new PlayerActivationException(PlayerActivationFailure.AuthenticationRequired);
        }

        return session;
    }

    private void EnsureSessionIsCurrent(Guid loginConnectionId, Guid expectedSessionId)
    {
        if (!sessions.TryGetByConnection(loginConnectionId, out AuthenticatedSession? current)
            || current is null
            || current.SessionId != expectedSessionId)
        {
            metrics.RecordActivationFailure();
            throw new PlayerActivationException(PlayerActivationFailure.AuthenticationExpired);
        }
    }
}

public enum PlayerActivationFailure
{
    AuthenticationRequired,
    AuthenticationExpired,
    CharacterUnavailable,
    SavedLocationUnavailable,
    ZoneUnavailable,
}

public sealed class PlayerActivationException(PlayerActivationFailure failure)
    : Exception($"Player activation failed: {failure}.")
{
    public PlayerActivationFailure Failure { get; } = failure;
}
