namespace ArchiForge.Persistence.GraphSnapshots;

/// <summary>
/// Reserved <see cref="ArchiForge.KnowledgeGraph.Models.GraphEdge.Properties"/> key used only when persisting
/// <see cref="ArchiForge.KnowledgeGraph.Models.GraphEdge.Label"/> into <c>dbo.GraphSnapshotEdgeProperties</c>.
/// User-supplied properties with this exact key are not written (the explicit Label field wins).
/// </summary>
internal static class GraphSnapshotEdgeRelationalConstants
{
    internal const string StoredLabelPropertyKey = "$ArchiForge:EdgeLabel";
}
