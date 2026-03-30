using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Services;

/// <summary>
/// Result of <see cref="GraphSnapshotReuseEvaluator.ResolveAsync"/>: the snapshot plus how it was produced (for diagnostics).
/// </summary>
/// <param name="Snapshot">The graph snapshot for the run.</param>
/// <param name="ResolutionMode">
/// <c>fresh_canonical_change</c> — context fingerprint differs from latest committed; graph rebuilt.
/// <c>fresh_no_stored_graph</c> — context matched prior but no stored graph; rebuilt.
/// <c>cloned_from_prior_graph</c> — reused topology from a prior run with equivalent context (deterministic clone).
/// </param>
public sealed record GraphSnapshotResolutionResult(GraphSnapshot Snapshot, string ResolutionMode);
