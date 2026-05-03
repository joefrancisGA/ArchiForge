namespace ArchLucid.Host.Core.Startup.Validation.Rules;

/// <summary>
/// Detects API key strings that are unsuitable for Production when API key authentication is enabled.
/// Callers must not log values passed here.
/// </summary>
internal static class ApiKeyPlaceholderDetection
{
    /// <summary>
    /// API keys under 20 characters have insufficient entropy for production use.
    /// </summary>
    private const int MinimumProductionKeyLength = 20;

    private static readonly string[] ExactBlocklist =
    [
        "changeme",
        "placeholder",
        "your-api-key",
        "replace-me",
        "secret",
        "test",
        "admin",
        "password",
        "example",
        "default",
        "demo",
        "key",
        "apikey",
        "api-key",
    ];

    private static readonly string[] SubstringBlocklist =
    [
        "todo",
        "fixme",
        "changeme",
        "placeholder",
        "replace",
    ];

    /// <summary>
    /// Returns whether <paramref name="value"/> should be treated as a placeholder or weak key for Production.
    /// Null, empty, and whitespace-only yield <c>false</c> (empty keys are validated separately).
    /// </summary>
    internal static bool IsPlaceholderValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();

        if (ExactBlocklist.Any(token => string.Equals(trimmed, token, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (SubstringBlocklist.Any(fragment => trimmed.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            return true;

        return trimmed.Length < MinimumProductionKeyLength;
    }
}
