using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Interfaces;

/// <summary>
/// Builds a <see cref="Models.GraphBuildResult"/> from an ingested <see cref="ContextIngestion.Models.ContextSnapshot"/>.
/// </summary>
public interface IGraphBuilder
{
    Task<GraphBuildResult> BuildAsync(
        ContextSnapshot contextSnapshot,
        CancellationToken ct);
}

