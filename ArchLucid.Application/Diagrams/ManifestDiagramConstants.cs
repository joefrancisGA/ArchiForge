namespace ArchLucid.Application.Diagrams;

/// <summary>
///     Well-known string values for <see cref="ManifestDiagramOptions" /> properties and the
///     normalization helpers in <see cref="ManifestDiagramService" />.
/// </summary>
public static class ManifestDiagramConstants
{
    // ── Layout ────────────────────────────────────────────────────────────────
    /// <summary>Left-to-right flowchart layout (Mermaid <c>LR</c>). Default.</summary>
    public const string LayoutLr = "LR";

    /// <summary>Top-to-bottom flowchart layout (Mermaid <c>TB</c>).</summary>
    public const string LayoutTb = "TB";

    // ── Relationship labels ───────────────────────────────────────────────────
    /// <summary>Show the relationship type as an edge label. Default.</summary>
    public const string RelationshipLabelsType = "type";

    /// <summary>Suppress all edge labels.</summary>
    public const string RelationshipLabelsNone = "none";

    // ── Group-by ──────────────────────────────────────────────────────────────
    /// <summary>No subgraph grouping. Default.</summary>
    public const string GroupByNone = "none";

    /// <summary>Group services into subgraphs by <c>RuntimePlatform</c>.</summary>
    public const string GroupByRuntimePlatform = "runtimeplatform";

    /// <summary>Group services into subgraphs by <c>ServiceType</c>.</summary>
    public const string GroupByServiceType = "servicetype";
}
