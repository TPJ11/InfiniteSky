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
        double accountLevel,
        byte[]? legacyPasswordUtf8)
    {
        ArgumentNullException.ThrowIfNull(username);
        if (!double.IsFinite(accountLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(accountLevel));
        }

        AccountId = accountId;
        Username = username;
        AccountLevel = accountLevel;
        LegacyPasswordUtf8 = legacyPasswordUtf8;
    }

    public AccountId AccountId { get; }
    public string Username { get; }

    /// <summary>
    /// Gets the server-loaded legacy account privilege value. This is never client supplied.
    /// </summary>
    public double AccountLevel { get; }

    /// <summary>
    /// Gets the transient legacy plaintext credential in UTF-8 form, or null when the
    /// legacy field was missing or null. The caller must zero a returned array.
    /// </summary>
    public byte[]? LegacyPasswordUtf8 { get; }
}
