namespace ArchLucid.ArtifactSynthesis.Sanitization;

/// <summary>
/// Strips control/bidi override characters from LLM- or user-origin strings before Markdown/DOCX embedding
/// (reduces confusing Unicode / RTL spoofing in generated artifacts).
/// </summary>
public static class LlmArtifactFreeTextSanitizer
{
    /// <summary>Sanitize free text for embedding; preserves common whitespace (space, tab, CR, LF).</summary>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        return new string(text.Where(static c => !IsStripped(c)).ToArray());
    }

    private static bool IsStripped(char c)
    {
        if (c is '\n' or '\r' or '\t')
            return false;

        if (c < 0x20)
            return true;

        // Bidi / isolates / overrides (Unicode TR #9)
        if (c is >= '\u202A' and <= '\u202E')
            return true;

        if (c is >= '\u2066' and <= '\u2069')
            return true;

        if (c is '\u200B' or '\u200C' or '\u200D' or '\u200E' or '\u200F' or '\uFEFF')
            return true;

        return false;
    }
}
