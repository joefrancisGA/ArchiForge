using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

using JetBrains.Annotations;

namespace ArchLucid.Application;

public interface IArchitectureApplicationService
{
    [UsedImplicitly]
    Task<GetRunResult?> GetRunAsync(string runId, CancellationToken cancellationToken = default);
    Task<SubmitResultResult> SubmitAgentResultAsync(string runId, AgentResult? result, CancellationToken cancellationToken = default);
    Task<GoldenManifest?> GetManifestAsync(string version, CancellationToken cancellationToken = default);
    Task<SeedFakeResultsResult> SeedFakeResultsAsync(
        string runId,
        PilotSeedFakeResultsOptions? pilotOptions = null,
        CancellationToken cancellationToken = default);
}
