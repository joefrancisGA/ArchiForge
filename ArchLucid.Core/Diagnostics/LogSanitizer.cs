namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Strips C0/C1 control characters (CR, LF, TAB, etc.) from values before structured logging,
///     preventing log injection (CWE-117) in plaintext sinks.
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    ///     Returns <paramref name="input" /> with every <see cref="char.IsControl" /> character
    ///     replaced by <c>'_'</c>. Returns <see cref="string.Empty" /> for null/empty input.
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // CodeQL cs/log-forging requires explicit Replace for \n and \r
        string replaced = input.Replace("\n", "_").Replace("\r", "_");

        // Fast path: no control chars at all (common case)
        bool clean = replaced.All(t => !char.IsControl(t));

        if (clean)
            return replaced;

        return string.Create(replaced.Length, replaced, static (span, src) =>
        {
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                span[i] = char.IsControl(c) ? '_' : c;
            }
        });
    }
}
