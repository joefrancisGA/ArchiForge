using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Services;

public class KnowledgeGraphService(IGraphBuilder graphBuilder) : IKnowledgeGraphService
{
    public async Task<GraphSnapshot> BuildSnapshotAsync(
        ContextSnapshot contextSnapshot,
        CancellationToken ct)
    {
        var buildResult = await graphBuilder.BuildAsync(contextSnapshot, ct);

        return new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = contextSnapshot.SnapshotId,
            RunId = contextSnapshot.RunId,
            CreatedUtc = DateTime.UtcNow,
            Nodes = buildResult.Nodes,
            Edges = buildResult.Edges,
            Warnings = buildResult.Warnings
        };
    }
}
