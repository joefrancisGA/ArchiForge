using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Mapping;

public class GraphNodeFactory : IGraphNodeFactory
{
    public GraphNode CreateNode(CanonicalObject item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Dictionary<string, string> properties = item.Properties;
        properties.TryGetValue("category", out string? category);

        return new GraphNode
        {
            NodeId = $"obj-{item.ObjectId}",
            NodeType = item.ObjectType,
            Label = item.Name,
            Category = category,
            SourceType = item.SourceType,
            SourceId = item.SourceId,
            Properties = new Dictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase)
        };
    }
}
