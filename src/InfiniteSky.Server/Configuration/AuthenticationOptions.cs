namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Authentication request safeguards.
/// </summary>
public sealed class AuthenticationOptions
{
    /// <summary>
    /// Maximum time allowed for account lookup and session establishment.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;
}
