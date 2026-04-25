using ArchLucid.ContextIngestion.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.KnowledgeGraph.Interfaces;

/// <summary>
///     Builds a <see cref="Models.GraphBuildResult" /> from an ingested
///     <see cref="ContextIngestion.Models.ContextSnapshot" />.
/// </summary>
public interface IGraphBuilder
{
    Task<GraphBuildResult> BuildAsync(
        ContextSnapshot contextSnapshot,
        CancellationToken ct);
}
