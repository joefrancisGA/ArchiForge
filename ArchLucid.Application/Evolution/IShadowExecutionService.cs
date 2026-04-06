using ArchiForge.Application.Analysis;
using ArchiForge.Contracts.Evolution;

namespace ArchiForge.Application.Evolution;

/// <summary>
/// Isolated shadow execution: one read of run detail, in-memory clone + candidate overlay, analysis on the clone only (no writes).
/// </summary>
public interface IShadowExecutionService
{
    /// <summary>
    /// Loads baseline detail once, deep-clones it, applies <paramref name="request"/>.CandidateChangeSet on the clone,
    /// runs <see cref="IArchitectureAnalysisService"/> without determinism/replay/evidence DB paths, returns the report.
    /// </summary>
    Task<ArchitectureAnalysisReport> ExecuteAsync(
        ShadowExecutionRequest request,
        CancellationToken cancellationToken = default);
}
