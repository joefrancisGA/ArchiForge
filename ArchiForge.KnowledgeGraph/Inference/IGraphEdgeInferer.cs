using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Inference;

public interface IGraphEdgeInferer
{
    IReadOnlyList<GraphEdge> InferEdges(
        ContextSnapshot contextSnapshot,
        IReadOnlyList<GraphNode> nodes);
}
