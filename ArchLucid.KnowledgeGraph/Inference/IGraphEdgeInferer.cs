using ArchLucid.ContextIngestion.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.KnowledgeGraph.Inference;

/// <summary>
///     Infers edges between graph nodes for a given context snapshot (e.g. CONTAINS, PROTECTS, APPLIES_TO).
/// </summary>
public interface IGraphEdgeInferer
{
    IReadOnlyList<GraphEdge> InferEdges(
        ContextSnapshot contextSnapshot,
        IReadOnlyList<GraphNode> nodes);
}
