using System.Security.Cryptography;
using System.Text;

namespace ArchiForge.ArtifactSynthesis.Services;

public static class ArtifactHashing
{
    public static string ComputeHash(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
