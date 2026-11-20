using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Ephemeral login authentication. It is not a character or world-player session.
/// </summary>
public sealed record AuthenticatedSession(
    Guid SessionId,
    AccountId AccountId,
    string Username,
    double AccountLevel,
    Guid ConnectionId,
    DateTimeOffset CreatedAt);
