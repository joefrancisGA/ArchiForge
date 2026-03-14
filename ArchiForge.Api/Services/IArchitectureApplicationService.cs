using ArchiForge.Api.Models;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Api.Services;

public interface IArchitectureApplicationService
{
    Task<CreateRunResult> CreateRunAsync(ArchitectureRequest request, CancellationToken cancellationToken = default);
    Task<GetRunResult?> GetRunAsync(string runId, CancellationToken cancellationToken = default);
    Task<SubmitResultResult> SubmitAgentResultAsync(string runId, AgentResult result, CancellationToken cancellationToken = default);
    Task<CommitRunResult> CommitRunAsync(string runId, CancellationToken cancellationToken = default);
    Task<GoldenManifest?> GetManifestAsync(string version, CancellationToken cancellationToken = default);
    Task<SeedFakeResultsResult> SeedFakeResultsAsync(string runId, CancellationToken cancellationToken = default);
}

public sealed record CreateRunResult(bool Success, CreateArchitectureRunResponse? Response, IReadOnlyList<string> Errors);

public sealed record GetRunResult(ArchitectureRun Run, IReadOnlyList<AgentTask> Tasks, IReadOnlyList<AgentResult> Results);

public sealed record SubmitResultResult(bool Success, string? ResultId, string? Error);

public sealed record CommitRunResult(bool Success, CommitRunResponse? Response, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);

public sealed record SeedFakeResultsResult(bool Success, int ResultCount, string? Error);
