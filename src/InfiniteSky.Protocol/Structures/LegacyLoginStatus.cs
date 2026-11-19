namespace InfiniteSky.Protocol.Structures;

/// <summary>
/// Status values declared by the original login packet handler.
/// </summary>
public enum LegacyLoginStatus : byte
{
    Success = 0,
    CannotAuthenticate = 1,
    AccountNotFound = 2,
    IncorrectPassword = 3,
    Suspended = 4,
    AlreadyLoggedIn = 5,
    NeedToPay = 6,
    WrongVersion = 7,
    Maintenance = 8,
    ServerFull = 9,
    UnknownError = 10,
}
