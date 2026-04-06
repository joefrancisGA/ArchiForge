using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Interfaces;

/// <summary>
/// Validates structural integrity of a <see cref="Models.GraphSnapshot"/> before persistence or downstream use.
/// </summary>
public interface IGraphValidator
{
    void Validate(GraphSnapshot snapshot);
}
