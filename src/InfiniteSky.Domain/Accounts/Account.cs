using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Accounts;

/// <summary>
/// Account-side identity and non-credential state. Credentials remain a persistence/security concern.
/// </summary>
public sealed class Account
{
    public Account(AccountId id, string username)
    {
        ArgumentNullException.ThrowIfNull(username);
        Id = id;
        Username = username;
    }

    public AccountId Id { get; }
    public string Username { get; }
    public string? Email { get; init; }
    public int Active { get; set; }
    public int Level { get; set; }
    public string? LastIp { get; set; }
    public bool UsesPin { get; set; }
    public string Pin { get; set; } = "0000";
    public DateTimeOffset? LastLogin { get; set; }
    public long ActionPoints { get; set; }

    /// <summary>
    /// Gets per-server data by exact server name. Null preserves the source default.
    /// </summary>
    public IDictionary<string, AccountServerData>? CharacterIndividuals { get; set; }
}
