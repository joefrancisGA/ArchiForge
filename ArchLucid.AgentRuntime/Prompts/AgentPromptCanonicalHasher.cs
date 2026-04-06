using System.Security.Cryptography;
using System.Text;

namespace ArchLucid.AgentRuntime.Prompts;

/// <summary>
/// Canonical hashing for prompt templates: normalize newlines so Git/Windows vs Linux does not change the fingerprint, then SHA-256 over UTF-8.
/// </summary>
public static class AgentPromptCanonicalHasher
{
    /// <summary>Lowercase hex SHA-256 of <paramref name="text"/> after newline canonicalization.</summary>
    public static string Sha256HexUtf8Normalized(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        byte[] utf8 = Encoding.UTF8.GetBytes(normalized);
        byte[] hash = SHA256.HashData(utf8);

        return Convert.ToHexStringLower(hash);
    }
}
