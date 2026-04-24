using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

using JetBrains.Annotations;

namespace ArchLucid.Host.Core.Services;

/// <summary>Optional flags for development-only <see cref="IArchitectureApplicationService.SeedFakeResultsAsync" />.</summary>
public sealed record PilotSeedFakeResultsOptions(bool MarkRealModeFellBackToSimulator);

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

public sealed record GetRunResult(ArchitectureRun Run, IReadOnlyList<AgentTask> Tasks, IReadOnlyList<AgentResult> Results);

/// <summary>How the API should map a failed submit/seed operation to HTTP (when <see cref="SubmitResultResult.Success"/> is false).</summary>
public enum ApplicationServiceFailureKind
{
    BadRequest,
    RunNotFound,
    ResourceNotFound
}

public sealed record SubmitResultResult(
    bool Success,
    string? ResultId,
    string? Error,
    ApplicationServiceFailureKind? FailureKind = null);

public sealed record SeedFakeResultsResult(
    bool Success,
    int ResultCount,
    string? Error,
    ApplicationServiceFailureKind? FailureKind = null);
