using System.Security.Cryptography;
using System.Text;

namespace ArchiForge.ArtifactSynthesis.Services;

public static class ArtifactHashing
{
    public static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
