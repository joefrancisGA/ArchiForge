namespace ArchiForge.Application.Diagrams;

/// <summary>
/// Shared helper that converts arbitrary strings into Mermaid-safe node identifiers.
/// Replaces non-alphanumeric characters with underscores, prefixes digit-leading IDs with
/// <c>n_</c>, and returns <c>node_unknown</c> when the input is blank or sanitizes to empty.
/// </summary>
internal static class DiagramIdSanitizer
{
    /// <summary>
    /// Sanitizes <paramref name="value"/> into a valid Mermaid node identifier.
    /// Returns <c>node_unknown</c> when the result would otherwise be blank.
    /// </summary>
    public static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "node_unknown";

        char[] chars = value.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        string cleaned = new string(chars);

        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "node_unknown";

        if (char.IsDigit(cleaned[0]))
            cleaned = $"n_{cleaned}";

        return cleaned;
    }
}
