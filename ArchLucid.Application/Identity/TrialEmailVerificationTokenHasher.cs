using System.Security.Cryptography;
using System.Text;

namespace ArchLucid.Application.Identity;
public static class TrialEmailVerificationTokenHasher
{
    public static string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}