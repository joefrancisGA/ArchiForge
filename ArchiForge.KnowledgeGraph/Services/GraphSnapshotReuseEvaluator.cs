using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Services;

/// <summary>
/// Decides whether to rebuild a <see cref="GraphSnapshot"/> or clone the latest graph
/// for a prior context with an equivalent canonical fingerprint.
/// </summary>
public static class GraphSnapshotReuseEvaluator
{
    /// <summary>
    /// Returns a cloned graph when <paramref name="priorCommittedContext"/> is equivalent to
    /// <paramref name="contextSnapshot"/> and a graph exists for the prior context; otherwise builds fresh.
    /// </summary>
    public static async Task<GraphSnapshot> ResolveAsync(
        ContextSnapshot? priorCommittedContext,
        ContextSnapshot contextSnapshot,
        Guid runId,
        IKnowledgeGraphService knowledgeGraphService,
        IGraphSnapshotRepository graphSnapshotRepository,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(contextSnapshot);
        ArgumentNullException.ThrowIfNull(knowledgeGraphService);
        ArgumentNullException.ThrowIfNull(graphSnapshotRepository);

        if (!GraphSnapshotCanonicalFingerprint.AreEquivalent(priorCommittedContext, contextSnapshot))
            return await knowledgeGraphService.BuildSnapshotAsync(contextSnapshot, ct).ConfigureAwait(false);

        GraphSnapshot? priorGraph = await graphSnapshotRepository
            .GetLatestByContextSnapshotIdAsync(priorCommittedContext!.SnapshotId, ct).ConfigureAwait(false);

        if (priorGraph is null)
            return await knowledgeGraphService.BuildSnapshotAsync(contextSnapshot, ct).ConfigureAwait(false);

        return GraphSnapshotCloner.CloneForNewRun(priorGraph, contextSnapshot, runId);
    }
}
