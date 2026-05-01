using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Evidence;

/// <summary>
///     Assembles an <see cref="AgentEvidencePackage" /> from an <see cref="ArchitectureRequest" />,
///     packaging policies, service catalog hints, patterns, prior manifest context, and notes
///     for consumption by agent executors.
/// </summary>
public interface IEvidenceBuilder
{
    /// <summary>
    ///     Builds and returns the evidence package for <paramref name="runId" />.
    /// </summary>
    Task<AgentEvidencePackage> BuildAsync(
        string runId,
        ArchitectureRequest request,
        CancellationToken cancellationToken = default);
}
