using System.Security.Cryptography;
using System.Text;

namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Matches the original plaintext account field using an exact, constant-time byte comparison.
/// </summary>
public sealed class LegacyPlaintextCredentialVerifier : ICredentialVerifier
{
    public bool Verify(
        string suppliedPassword,
        ReadOnlySpan<byte> storedLegacyPasswordUtf8)
    {
        ArgumentNullException.ThrowIfNull(suppliedPassword);

        byte[] suppliedBytes = Encoding.UTF8.GetBytes(suppliedPassword);
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                suppliedBytes,
                storedLegacyPasswordUtf8);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(suppliedBytes);
        }
    }
}
