using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Mapping;

public interface IGraphNodeFactory
{
    GraphNode CreateNode(CanonicalObject item);
}
