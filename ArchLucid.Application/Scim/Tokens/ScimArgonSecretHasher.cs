using System.Security.Cryptography;

using Konscious.Security.Cryptography;

namespace ArchLucid.Application.Scim.Tokens;

public static class ScimArgonSecretHasher
{
    private const int Parallelism = 4;

    private const int MemoryKiB = 65536;

    private const int Iterations = 3;

    private const int HashBytes = 32;

    public static byte[] HashSecret(ReadOnlySpan<byte> secret, Guid tenantId)
    {
        byte[] salt = System.Text.Encoding.UTF8.GetBytes(tenantId.ToString("D", System.Globalization.CultureInfo.InvariantCulture));
        byte[] secretCopy = secret.ToArray();

        using Argon2id argon2 = new(secretCopy);
        argon2.Salt = salt;
        argon2.DegreeOfParallelism = Parallelism;
        argon2.MemorySize = MemoryKiB;
        argon2.Iterations = Iterations;

        return argon2.GetBytes(HashBytes);
    }

    public static bool VerifySecret(ReadOnlySpan<byte> secret, Guid tenantId, ReadOnlySpan<byte> expectedHash)
    {
        byte[] recomputed = HashSecret(secret, tenantId);

        try
        {
            return CryptographicOperations.FixedTimeEquals(recomputed, expectedHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(recomputed);
        }
    }
}
