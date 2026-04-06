using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Serialization;

namespace ArchiForge.Persistence.GraphSnapshots;

/// <summary>Legacy JSON used when relational edge property rows are missing (merge path).</summary>
internal static class GraphSnapshotJsonFallback
{
    /// <remarks>TODO: remove after relational migration complete (merge-from-JSON path).</remarks>
    internal static List<GraphEdge> DeserializeEdgesForMerge(string edgesJson)
    {
        return JsonEntitySerializer.Deserialize<List<GraphEdge>>(edgesJson);
    }
}
