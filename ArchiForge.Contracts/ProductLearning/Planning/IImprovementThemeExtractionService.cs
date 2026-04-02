using ArchiForge.Contracts.ProductLearning;

namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// Derives recurring improvement themes from 58R aggregation snapshots and scoped pilot signals (deterministic; no NLP/LLM).
/// </summary>
public interface IImprovementThemeExtractionService
{
    /// <summary>
    /// Builds themes from rollups, artifact trends, repeated comment prefixes, JSON tags/annotations on signals, and optional triage hints.
    /// </summary>
    /// <param name="snapshot">58R aggregation snapshot (same scope as signals).</param>
    /// <param name="scopedSignals">
    /// Pilot signals already filtered to tenant/workspace/project (and typically the same <c>since</c> window as the snapshot).
    /// </param>
    /// <param name="triageQueue">Optional triage rows; rows with <see cref="TriageQueueItem.RelatedSignalId"/> can attach extra examples.</param>
    /// <param name="options">Thresholds and caps.</param>
    Task<IReadOnlyList<ImprovementThemeWithEvidence>> ExtractThemesAsync(
        ProductLearningAggregationSnapshot snapshot,
        IReadOnlyList<ProductLearningPilotSignalRecord> scopedSignals,
        IReadOnlyList<TriageQueueItem>? triageQueue,
        ImprovementThemeExtractionOptions options,
        CancellationToken cancellationToken);
}
