namespace ArchiForge.KnowledgeGraph.Models;

/// <summary>
/// Edge row materialized from <c>dbo.GraphSnapshotEdges</c> for filtered queries without full JSON deserialization.
/// </summary>
public sealed record GraphSnapshotIndexedEdge(
    string EdgeId,
    string FromNodeId,
    string ToNodeId,
    string EdgeType,
    double Weight);
