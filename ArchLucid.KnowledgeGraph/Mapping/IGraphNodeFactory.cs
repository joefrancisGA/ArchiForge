using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Mapping;

/// <summary>
/// Maps a single <see cref="ContextIngestion.Models.CanonicalObject"/> to a typed <see cref="Models.GraphNode"/>.
/// </summary>
public interface IGraphNodeFactory
{
    GraphNode CreateNode(CanonicalObject item);
}
