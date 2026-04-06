using ArchiForge.Contracts.Architecture;

namespace ArchiForge.Application;

/// <summary>
/// Single query path for assembling the canonical <see cref="ArchitectureRunDetail"/> aggregate.
/// All features that need to read run state — export, compare, governance, analysis — should
/// use this service rather than making their own bespoke repository calls.
/// </summary>
public interface IRunDetailQueryService
{
    /// <summary>
    /// Loads the full <see cref="ArchitectureRunDetail"/> for <paramref name="runId"/>:
    /// run record, agent tasks and results, golden manifest (if committed), and decision traces.
    /// Returns <see langword="null"/> when no run exists with that id.
    /// </summary>
    Task<ArchitectureRunDetail?> GetRunDetailAsync(
        string runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns lightweight <see cref="RunSummary"/> entries for the most recent runs,
    /// ordered descending by creation time. Suitable for list/dashboard views.
    /// </summary>
    Task<IReadOnlyList<RunSummary>> ListRunSummariesAsync(
        CancellationToken cancellationToken = default);
}
