using ArchLucid.Contracts.Architecture;

namespace ArchLucid.Application.Architecture;

/// <summary>
///     Builds the coordinator <see cref="ArchitectureRunProvenanceGraph" /> for API and UI trace views.
/// </summary>
public interface IArchitectureRunProvenanceService
{
    /// <summary>
    ///     Assembles linkage nodes, edges, timeline, and traceability gaps for <paramref name="runId" />.
    ///     Returns <see langword="null" /> when the run does not exist.
    /// </summary>
    Task<ArchitectureRunProvenanceGraph?> GetProvenanceAsync(
        string runId,
        CancellationToken cancellationToken = default);
}
