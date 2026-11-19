using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Minimal account data for the legacy login use case. When present, password bytes are
/// UTF-8 bytes of the legacy plaintext field and are owned by the caller.
/// </summary>
public sealed class AccountAuthenticationRecord
{
    public AccountAuthenticationRecord(
        AccountId accountId,
        string username,
        byte[]? legacyPasswordUtf8)
    {
        ArgumentNullException.ThrowIfNull(username);

        AccountId = accountId;
        Username = username;
        LegacyPasswordUtf8 = legacyPasswordUtf8;
    }

    public AccountId AccountId { get; }
    public string Username { get; }

    /// <summary>
    /// Gets the transient legacy plaintext credential in UTF-8 form, or null when the
    /// legacy field was missing or null. The caller must zero a returned array.
    /// </summary>
    public byte[]? LegacyPasswordUtf8 { get; }
}
