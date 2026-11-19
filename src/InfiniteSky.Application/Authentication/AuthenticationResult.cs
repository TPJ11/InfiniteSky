namespace InfiniteSky.Application.Authentication;

public sealed record AuthenticationResult(
    AuthenticationOutcome Outcome,
    AuthenticatedSession? Session = null);
