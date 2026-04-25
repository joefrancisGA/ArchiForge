using ArchLucid.ContextIngestion.Models;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.KnowledgeGraph.Services;

/// <summary>
///     Decides whether to rebuild a <see cref="GraphSnapshot" /> or clone the latest graph
///     for a prior context with an equivalent canonical fingerprint.
/// </summary>
public static class GraphSnapshotReuseEvaluator
{
    /// <summary>
    ///     Returns a cloned graph when <paramref name="priorCommittedContext" /> is equivalent to
    ///     <paramref name="contextSnapshot" /> and a graph exists for the prior context; otherwise builds fresh.
    /// </summary>
    public static async Task<GraphSnapshotResolutionResult> ResolveAsync(
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
        {
            GraphSnapshot built = await knowledgeGraphService.BuildSnapshotAsync(contextSnapshot, ct);

            return new GraphSnapshotResolutionResult(built, "fresh_canonical_change");
        }

        GraphSnapshot? priorGraph = await graphSnapshotRepository
            .GetLatestByContextSnapshotIdAsync(priorCommittedContext!.SnapshotId, ct);

        if (priorGraph is null)
        {
            GraphSnapshot built = await knowledgeGraphService.BuildSnapshotAsync(contextSnapshot, ct);

            return new GraphSnapshotResolutionResult(built, "fresh_no_stored_graph");
        }

        GraphSnapshot cloned = GraphSnapshotCloner.CloneForNewRun(priorGraph, contextSnapshot, runId);

        return new GraphSnapshotResolutionResult(cloned, "cloned_from_prior_graph");
    }
}
