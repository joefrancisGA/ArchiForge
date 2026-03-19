using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Services;

public class KnowledgeGraphService(
    IGraphBuilder graphBuilder,
    IGraphSnapshotRepository repository)
    : IKnowledgeGraphService
{
    public async Task<GraphSnapshot> BuildSnapshotAsync(
        ContextSnapshot contextSnapshot,
        CancellationToken ct)
    {
        var buildResult = await graphBuilder.BuildAsync(contextSnapshot, ct);

        var snapshot = new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = contextSnapshot.SnapshotId,
            RunId = contextSnapshot.RunId,
            CreatedUtc = DateTime.UtcNow,
            Nodes = buildResult.Nodes,
            Edges = buildResult.Edges,
            Warnings = buildResult.Warnings
        };

        await repository.SaveAsync(snapshot, ct);

        return snapshot;
    }
}

