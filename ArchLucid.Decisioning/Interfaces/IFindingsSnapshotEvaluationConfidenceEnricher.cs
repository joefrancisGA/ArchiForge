using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Interfaces;

/// <summary>
///     Best-effort enrichment of persisted-shaped findings with evaluation-derived confidence after traces are scored.
/// </summary>
public interface IFindingsSnapshotEvaluationConfidenceEnricher
{
    Task TryEnrichAsync(FindingsSnapshot snapshot, CancellationToken cancellationToken);
}
