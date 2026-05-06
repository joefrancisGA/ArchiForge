namespace ArchLucid.Application.Analysis;
/// <summary>
///     Export profile for end-to-end comparison reports. Controls which sections
///     are included and the level of detail (headings, sections, verbosity).
/// </summary>
public static class EndToEndComparisonExportProfile
{
    /// <summary>Full content: all sections and full lists (current default).</summary>
    public const string Detailed = "detailed";
    /// <summary>Same as Detailed; backward-compatible default.</summary>
    public const string Default = "default";
    /// <summary>Minimal: title, run IDs, summary section only.</summary>
    public const string Short = "short";
    /// <summary>Executive: high-level summary, key counts, interpretation notes; minimal lists.</summary>
    public const string Executive = "executive";
    public static bool IsShort(string? profile)
    {
        return string.Equals(profile, Short, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsExecutive(string? profile)
    {
        return string.Equals(profile, Executive, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDetailedOrDefault(string? profile)
    {
        return string.IsNullOrWhiteSpace(profile) || string.Equals(profile, Default, StringComparison.OrdinalIgnoreCase) || string.Equals(profile, Detailed, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? profile)
    {
        return string.IsNullOrWhiteSpace(profile) ? Default : profile.Trim().ToLowerInvariant();
    }
}