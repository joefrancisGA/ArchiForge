using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Api.Services;

public interface IArchitectureApplicationService
{
    Task<GetRunResult?> GetRunAsync(string runId, CancellationToken cancellationToken = default);
    Task<SubmitResultResult> SubmitAgentResultAsync(string runId, AgentResult result, CancellationToken cancellationToken = default);
    Task<GoldenManifest?> GetManifestAsync(string version, CancellationToken cancellationToken = default);
    Task<SeedFakeResultsResult> SeedFakeResultsAsync(string runId, CancellationToken cancellationToken = default);
}

public sealed record GetRunResult(ArchitectureRun Run, IReadOnlyList<AgentTask> Tasks, IReadOnlyList<AgentResult> Results);

public sealed record SubmitResultResult(bool Success, string? ResultId, string? Error);

public sealed record SeedFakeResultsResult(bool Success, int ResultCount, string? Error);
