using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Services;

public class KnowledgeGraphService : IKnowledgeGraphService
{
    private readonly IGraphBuilder _graphBuilder;
    private readonly IGraphValidator _graphValidator;

    public KnowledgeGraphService(
        IGraphBuilder graphBuilder,
        IGraphValidator graphValidator)
    {
        _graphBuilder = graphBuilder;
        _graphValidator = graphValidator;
    }

    public async Task<GraphSnapshot> BuildSnapshotAsync(
        ContextSnapshot contextSnapshot,
        CancellationToken ct)
    {
        var buildResult = await _graphBuilder.BuildAsync(contextSnapshot, ct);

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

        _graphValidator.Validate(snapshot);

        return snapshot;
    }
}
