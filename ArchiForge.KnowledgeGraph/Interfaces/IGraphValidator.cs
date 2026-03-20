using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Interfaces;

public interface IGraphValidator
{
    void Validate(GraphSnapshot snapshot);
}
