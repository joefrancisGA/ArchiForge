using ArchLucid.Contracts.Explanation;

namespace ArchLucid.Application.Explanation;

/// <summary>
/// Resolves persisted artifact pointers for one finding on a run (no LLM narrative).
/// </summary>
public interface IFindingEvidenceChainService
{
    /// <summary>
    /// Returns chain pointers, or <see langword="null"/> when the run is missing, out of scope, or the finding id is unknown.
    /// </summary>
    Task<FindingEvidenceChainResponse?> BuildAsync(
        string runId,
        string findingId,
        CancellationToken cancellationToken = default);
}
