using System.Net;
using System.Net.Sockets;

namespace ArchLucid.Core.Security;

/// <summary>
/// SSRF guard for optional external document URLs — HTTPS only, no loopback/link-local/private (RFC 1918) targets.
/// </summary>
public static class AllowedDocumentUrlPolicy
{
    /// <summary>Returns a problem detail when <paramref name="rawUrl" /> is non-empty and not permitted.</summary>
    public static string? TryGetRejectionReason(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return null;

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out Uri? uri))
            return "SourceDocumentUrl must be an absolute HTTPS URL.";

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return "SourceDocumentUrl must use the https scheme.";

        return HostIsForbidden(uri)
            ? "SourceDocumentUrl must not target loopback, link-local, or private network addresses."
            : null;
    }

    private static bool HostIsForbidden(Uri uri)
    {
        string host = uri.IdnHost;

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IPAddress.TryParse(host, out IPAddress? ip))
        {
            // Hostname — resolve is async and risky in sync validator; block obvious literals only here.
            // Production fetch path must resolve and re-check before connect.
            return false;
        }

        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.AddressFamily is AddressFamily.InterNetwork)
        {
            byte[] b = ip.GetAddressBytes();

            if (b[0] is 10)
                return true;

            if (b[0] is 172 && b[1] is >= 16 and <= 31)
                return true;

            if (b[0] is 192 && b[1] is 168)
                return true;

            // RFC 3927 link-local 169.254.0.0/16
            if (b[0] is 169 && b[1] is 254)
                return true;
        }

        if (ip.AddressFamily is AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6Multicast)
                return true;
        }

        return false;
    }
}
