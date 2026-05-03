using System.Security.Cryptography;
using System.Text;

namespace ArchLucid.Host.Core.Services.Delivery;

/// <summary>Computes HMAC-SHA256 signatures for webhook JSON bodies.</summary>
public static class WebhookSignature
{
    public const string HeaderName = "X-ArchLucid-Webhook-Signature";
    public const string Prefix = "sha256=";

    public static string ComputeSha256Hex(string sharedSecret, byte[] utf8Body)
    {
        if (string.IsNullOrEmpty(sharedSecret))
            throw new ArgumentException("Shared secret is required.", nameof(sharedSecret));

        ArgumentNullException.ThrowIfNull(utf8Body);

        byte[] key = Encoding.UTF8.GetBytes(sharedSecret);

        using HMACSHA256 hmac = new(key);
        byte[] hash = hmac.ComputeHash(utf8Body);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
