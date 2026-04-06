namespace ArchiForge.Application.Diagrams;

/// <summary>
/// Controls the rendering behaviour of <see cref="IManifestDiagramService.GenerateMermaid"/>.
/// All options have sensible defaults; callers only need to override what they want to change.
/// </summary>
public sealed class ManifestDiagramOptions
{
    /// <summary>
    /// Mermaid flowchart layout direction.
    /// Use <see cref="ManifestDiagramConstants.LayoutLr"/> (default) or <see cref="ManifestDiagramConstants.LayoutTb"/>.
    /// </summary>
    public string Layout { get; set; } = ManifestDiagramConstants.LayoutLr;

    /// <summary>When <c>true</c>, include <c>RuntimePlatform</c> in node labels. Defaults to <c>true</c>.</summary>
    public bool IncludeRuntimePlatform { get; set; } = true;

    /// <summary>
    /// Edge label mode. Use <see cref="ManifestDiagramConstants.RelationshipLabelsType"/> (default)
    /// or <see cref="ManifestDiagramConstants.RelationshipLabelsNone"/>.
    /// </summary>
    public string RelationshipLabels { get; set; } = ManifestDiagramConstants.RelationshipLabelsType;

    /// <summary>
    /// Subgraph grouping for services. Use <see cref="ManifestDiagramConstants.GroupByNone"/> (default),
    /// <see cref="ManifestDiagramConstants.GroupByRuntimePlatform"/>, or
    /// <see cref="ManifestDiagramConstants.GroupByServiceType"/>.
    /// </summary>
    public string GroupBy { get; set; } = ManifestDiagramConstants.GroupByNone;
}

