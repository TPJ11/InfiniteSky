using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Minimal account data for the legacy login use case. The password bytes are
/// UTF-8 bytes of the legacy plaintext field and are owned by the caller.
/// </summary>
public sealed class AccountAuthenticationRecord
{
    public AccountAuthenticationRecord(
        AccountId accountId,
        string username,
        byte[] legacyPasswordUtf8)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(legacyPasswordUtf8);

        AccountId = accountId;
        Username = username;
        LegacyPasswordUtf8 = legacyPasswordUtf8;
    }

    public AccountId AccountId { get; }
    public string Username { get; }

    /// <summary>
    /// Gets the transient legacy plaintext credential in UTF-8 form.
    /// The caller must zero this owned array after verification.
    /// </summary>
    public byte[] LegacyPasswordUtf8 { get; }
}
