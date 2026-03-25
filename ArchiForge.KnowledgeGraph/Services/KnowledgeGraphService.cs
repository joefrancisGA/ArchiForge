using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Services;

/// <summary>
/// Builds and validates a knowledge graph snapshot from a <see cref="ContextSnapshot"/>.
/// Delegates graph construction to <see cref="IGraphBuilder"/> and validation to <see cref="IGraphValidator"/>.
/// </summary>
public class KnowledgeGraphService(
    IGraphBuilder graphBuilder,
    IGraphValidator graphValidator)
    : IKnowledgeGraphService
{
    public async Task<GraphSnapshot> BuildSnapshotAsync(
        ContextSnapshot contextSnapshot,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(contextSnapshot);

        GraphBuildResult buildResult = await graphBuilder.BuildAsync(contextSnapshot, ct);

        GraphSnapshot snapshot = new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = contextSnapshot.SnapshotId,
            RunId = contextSnapshot.RunId,
            CreatedUtc = DateTime.UtcNow,
            Nodes = buildResult.Nodes,
            Edges = buildResult.Edges,
            Warnings = buildResult.Warnings
        };

        graphValidator.Validate(snapshot);

        return snapshot;
    }
}
