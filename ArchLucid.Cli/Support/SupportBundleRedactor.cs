using System.Collections;

namespace ArchLucid.Cli.Support;

/// <summary>
/// Removes credentials and other sensitive material from strings included in support bundles.
/// </summary>
public static class SupportBundleRedactor
{
    private static readonly HashSet<string> SensitiveEnvironmentNameSubstrings =
    [
        "PASSWORD", "SECRET", "API_KEY", "APIKEY", "TOKEN", "CREDENTIAL", "PRIVATE_KEY", "CONN", "CONNECTIONSTRING"
    ];

    /// <summary>
    /// Returns a display-safe API base URL: strips userinfo (e.g. <c>https://user:pass@host</c> → <c>https://host</c>).
    /// </summary>
    public static string RedactHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))

            return string.Empty;


        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri? uri))

            return "(invalid url)";


        UriBuilder builder = new(uri)
        {
            UserName = string.Empty,
            Password = string.Empty
        };

        return builder.Uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    /// <summary>
    /// True when an environment variable name suggests a secret value (never emit the value in bundles).
    /// </summary>
    public static bool IsSensitiveEnvironmentVariableName(string name)
    {
        if (string.IsNullOrEmpty(name))

            return false;


        if (name.StartsWith("ARCHLUCID_", StringComparison.OrdinalIgnoreCase)
            && name.Contains("SQL", StringComparison.OrdinalIgnoreCase))

            return true;


        string upper = name.ToUpperInvariant();

        foreach (string fragment in SensitiveEnvironmentNameSubstrings)

            if (upper.Contains(fragment, StringComparison.Ordinal))

                return true;



        return false;
    }

    /// <summary>
    /// Builds a map of non-sensitive environment keys to safe values, and sensitive keys to the literal <c>"(set)"</c> or <c>"(not set)"</c> only.
    /// </summary>
    public static IReadOnlyDictionary<string, string> SnapshotEnvironmentForBundle()
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            string key = entry.Key.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(key))

                continue;


            if (!key.StartsWith("ARCHLUCID_", StringComparison.OrdinalIgnoreCase)
                && !key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase))

                continue;


            if (IsSensitiveEnvironmentVariableName(key))
            {
                string? val = entry.Value?.ToString();

                result[key] = string.IsNullOrEmpty(val) ? "(not set)" : "(set)";
            }
            else
            {
                string raw = entry.Value?.ToString() ?? string.Empty;

                if (string.Equals(key, "ARCHLUCID_API_URL", StringComparison.OrdinalIgnoreCase)
                    && raw.StartsWith("http", StringComparison.OrdinalIgnoreCase))

                    result[key] = RedactHttpUrl(raw);

                else

                    result[key] = raw;

            }
        }

        return result;
    }
}
