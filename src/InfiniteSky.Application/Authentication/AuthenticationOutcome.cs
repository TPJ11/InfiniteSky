namespace InfiniteSky.Application.Authentication;

public enum AuthenticationOutcome
{
    Authenticated,
    AccountNotFound,
    IncorrectPassword,
    AlreadyLoggedIn,
    CannotAuthenticate,
    InvalidRequest,
    TimedOut,
    RequestInProgress,
    AlreadyAuthenticated,
    ConnectionClosed,
}
