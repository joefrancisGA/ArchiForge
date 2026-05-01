namespace ArchLucid.Application.Summaries;

/// <summary>
///     Controls which sections are included in the Markdown output of <see cref="IManifestSummaryService" />.
/// </summary>
public sealed class ManifestSummaryOptions
{
    /// <summary>Pre-built instance with all sections enabled and no relationship limit.</summary>
    public static ManifestSummaryOptions Default
    {
        get;
    } = new();

    /// <summary>Include the manifest-level required-controls section. Defaults to <c>true</c>.</summary>
    public bool IncludeRequiredControls
    {
        get;
        set;
    } = true;

    /// <summary>Include per-component required-controls within the Services section. Defaults to <c>true</c>.</summary>
    public bool IncludeComponentControls
    {
        get;
        set;
    } = true;

    /// <summary>Include per-component tags within the Services section. Defaults to <c>true</c>.</summary>
    public bool IncludeTags
    {
        get;
        set;
    } = true;

    /// <summary>Include the Relationships section. Defaults to <c>true</c>.</summary>
    public bool IncludeRelationships
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     Maximum number of relationships to include. <c>null</c> (default) means no limit.
    ///     Useful when the manifest has very large relationship sets.
    /// </summary>
    public int? MaxRelationships
    {
        get;
        set;
    }

    /// <summary>Include the compliance-tags section from <c>manifest.Governance</c>. Defaults to <c>true</c>.</summary>
    public bool IncludeComplianceTags
    {
        get;
        set;
    } = true;
}
