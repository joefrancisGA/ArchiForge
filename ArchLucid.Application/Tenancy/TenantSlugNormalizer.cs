using System.Text;

namespace ArchLucid.Application.Tenancy;

/// <summary>Derives URL-safe tenant slugs from display names.</summary>
public static class TenantSlugNormalizer
{
    /// <summary>Normalizes <paramref name="name"/> to a lowercase slug (letters, digits, hyphen).</summary>
    public static string FromName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        StringBuilder sb = new();
        bool lastWasHyphen = false;

        foreach (char c in name.Trim().ToLowerInvariant())

            if (char.IsAsciiLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasHyphen = false;
            }
            else if (char.IsWhiteSpace(c) || c is '-' or '_' or '.')
                if (sb.Length > 0 && !lastWasHyphen)
                {
                    sb.Append('-');
                    lastWasHyphen = true;
                }

        while (sb.Length > 0 && sb[0] == '-')
            sb.Remove(0, 1);

        while (sb.Length > 0 && sb[^1] == '-')
            sb.Remove(sb.Length - 1, 1);

        string slug = sb.ToString();

        if (slug.Length > 100)
            slug = slug[..100].TrimEnd('-');

        return string.IsNullOrEmpty(slug) ? throw new InvalidOperationException("Tenant name must contain at least one letter or digit for slug generation.") : slug;
    }
}
