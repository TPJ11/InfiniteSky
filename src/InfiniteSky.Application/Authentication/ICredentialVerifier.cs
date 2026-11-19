namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Verifies a supplied legacy password against transient stored credential bytes.
/// </summary>
public interface ICredentialVerifier
{
    bool Verify(
        string suppliedPassword,
        ReadOnlySpan<byte> storedLegacyPasswordUtf8);
}
