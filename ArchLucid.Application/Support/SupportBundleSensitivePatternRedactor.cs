using System.Collections;
using System.Text.RegularExpressions;

namespace ArchLucid.Application.Support;
/// <summary>
///     Server-side port of the redaction patterns used by the CLI support-bundle stack
///     (<c>ArchLucid.Cli.Support.SupportBundleRedactor</c>) — kept in lock-step so a
///     bundle assembled by the CLI and a bundle assembled here are equally safe to email.
/// </summary>
/// <remarks>
///     Layering note: <c>ArchLucid.Cli</c> already references <c>ArchLucid.Application</c>,
///     so we cannot reference the CLI redactor from here without inverting the dependency.
///     A future PR can move the CLI redactor here and have the CLI delegate to it; the
///     pattern set is intentionally byte-identical so that move is mechanical.
/// </remarks>
public static class SupportBundleSensitivePatternRedactor
{
    private static readonly Regex BearerHeader = new(@"(?i)(Authorization\s*:\s*Bearer\s+)[^\s\r\n""]+", RegexOptions.Compiled);
    private static readonly Regex ApiKeyHeader = new(@"(?i)(X-Api-Key\s*:\s*)[^\r\n]+", RegexOptions.Compiled);
    private static readonly Regex ConnectionSecret = new(@"(?i)(\b(?:Password|Pwd|AccountKey|SharedAccessKey)\s*=\s*)[^\s;""]+", RegexOptions.Compiled);
    private static readonly Regex EmailAddress = new(@"(?<![\w.+_-])([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})(?![\w.+_-])", RegexOptions.Compiled);
    /// <summary>JWT-looking three-part base64url segments (redacts entire token).</summary>
    private static readonly Regex LikelyJwt = new(@"\beyJ[a-zA-Z0-9_-]{5,}\.[a-zA-Z0-9_-]{5,}\.[a-zA-Z0-9_-]{5,}\b", RegexOptions.Compiled);
    private static readonly Regex SystemRoleBlock = new(@"(?im)(<\|\s*system\s*\|>|```\s*system|^\s*#+\s*system\s+prompt\s*:)", RegexOptions.Compiled);
    private static readonly HashSet<string> SensitiveEnvironmentNameSubstrings = ["PASSWORD", "SECRET", "API_KEY", "APIKEY", "TOKEN", "CREDENTIAL", "PRIVATE_KEY", "CONN", "CONNECTIONSTRING"];
    /// <summary>Strips <c>user:pass@</c> userinfo segments from a URL string.</summary>
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

    /// <summary>True when the variable name strongly suggests a secret value.</summary>
    public static bool IsSensitiveEnvironmentVariableName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrEmpty(name))
            return false;
        if (name.StartsWith("ARCHLUCID_", StringComparison.OrdinalIgnoreCase) && name.Contains("SQL", StringComparison.OrdinalIgnoreCase))
            return true;
        string upper = name.ToUpperInvariant();
        foreach (string fragment in SensitiveEnvironmentNameSubstrings)
        {
            if (upper.Contains(fragment, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Snapshots <c>ARCHLUCID_*</c> and <c>DOTNET_*</c> environment variables, replacing values
    ///     of secret-shaped names with the literals <c>(set)</c> / <c>(not set)</c>.
    /// </summary>
    public static IReadOnlyDictionary<string, string> SnapshotEnvironmentForBundle()
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            string key = entry.Key.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(key))
                continue;
            if (!key.StartsWith("ARCHLUCID_", StringComparison.OrdinalIgnoreCase) && !key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase))
                continue;
            if (IsSensitiveEnvironmentVariableName(key))
            {
                string? val = entry.Value?.ToString();
                result[key] = string.IsNullOrEmpty(val) ? "(not set)" : "(set)";
                continue;
            }

            string raw = entry.Value?.ToString() ?? string.Empty;
            if (string.Equals(key, "ARCHLUCID_API_URL", StringComparison.OrdinalIgnoreCase) && raw.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                result[key] = RedactHttpUrl(raw);
                continue;
            }

            result[key] = raw;
        }

        return result;
    }

    /// <summary>Replaces inline bearer tokens, X-Api-Key headers, and connection secrets with <c>[REDACTED]</c>.</summary>
    public static string RedactSensitivePatterns(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;
        string s = BearerHeader.Replace(text, m => m.Groups[1].Value + "[REDACTED]");
        s = ApiKeyHeader.Replace(s, m => m.Groups[1].Value + "[REDACTED]");
        s = ConnectionSecret.Replace(s, m => m.Groups[1].Value + "[REDACTED]");
        s = EmailAddress.Replace(s, "[REDACTED_EMAIL]");
        s = LikelyJwt.Replace(s, "[REDACTED_JWT]");
        s = SystemRoleBlock.Replace(s, "[REDACTED_LLM_PROMPT_MARKER]");
        return s;
    }
}