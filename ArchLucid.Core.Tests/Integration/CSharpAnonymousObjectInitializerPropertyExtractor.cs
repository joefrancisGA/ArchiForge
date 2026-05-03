namespace ArchLucid.Core.Tests.Integration;

/// <summary>
///     Extracts property names from a simple C# anonymous object initializer block (no nested inner <c>{ ... }</c>
///     inside the block). Used to guard integration payload shapes against silent renames.
/// </summary>
internal static class CSharpAnonymousObjectInitializerPropertyExtractor
{
    internal static IReadOnlyList<string> ExtractPropertyNames(string anchor, string source)
    {
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));
        if (source is null) throw new ArgumentNullException(nameof(source));

        int anchorIndex = source.IndexOf(anchor, StringComparison.Ordinal);
        if (anchorIndex < 0)
            throw new InvalidOperationException($"Could not find anchor '{anchor}' in publisher source.");

        ReadOnlySpan<char> tail = source.AsSpan(anchorIndex + anchor.Length);
        int braceOpen = tail.IndexOf('{');
        if (braceOpen < 0)
            throw new InvalidOperationException($"Expected '{{' after anchor '{anchor}'.");

        ReadOnlySpan<char> fromOpen = tail[braceOpen..];
        if (!TrySliceBalancedBraces(fromOpen, out ReadOnlySpan<char> inner))
            throw new InvalidOperationException("Unbalanced braces in anonymous object initializer.");

        List<string> names = [];
        foreach (string line in inner.ToString().Split('\n'))
        {
            string trimmed = line.Trim().TrimEnd('\r');
            if (trimmed.Length is 0)
                continue;

            if (trimmed is "{" or "}")
                continue;

            // `name = expr,` or trailing `name,` (target-typed / shorthand)
            int eq = trimmed.IndexOf('=');
            if (eq > 0)
            {
                ReadOnlySpan<char> left = trimmed.AsSpan(0, eq).TrimEnd();
                int lastSpace = left.LastIndexOf(' ');
                ReadOnlySpan<char> id = lastSpace >= 0 ? left[(lastSpace + 1)..] : left;

                if (IsIdentifier(id))
                    names.Add(id.ToString());

                continue;
            }

            if (trimmed.Length <= 0 || trimmed[^1] is not ',') continue;

            ReadOnlySpan<char> idOnly = trimmed.AsSpan(0, trimmed.Length - 1).Trim();

            if (IsIdentifier(idOnly))
                names.Add(idOnly.ToString());
        }

        return names;
    }

    private static bool TrySliceBalancedBraces(ReadOnlySpan<char> fromOpenBrace, out ReadOnlySpan<char> innerWithoutOuter)
    {
        innerWithoutOuter = default;

        if (fromOpenBrace.IsEmpty || fromOpenBrace[0] is not '{')
            return false;

        int depth = 0;

        for (int i = 0; i < fromOpenBrace.Length; i++)
        {
            char c = fromOpenBrace[i];

            if (c is '{')
            {
                depth++;

                continue;
            }

            if (c is not '}')
                continue;

            depth--;

            if (depth is not 0)
                continue;

            // Inner is between first '{' and matching '}'
            innerWithoutOuter = fromOpenBrace[1..i];

            return true;
        }

        return false;
    }

    private static bool IsIdentifier(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
            return false;

        if (!char.IsLetter(text[0]) && text[0] is not '_')
            return false;

        for (int i = 1; i < text.Length; i++)
        {
            char c = text[i];

            if (char.IsLetterOrDigit(c) || c is '_')
                continue;

            return false;
        }

        return true;
    }
}
