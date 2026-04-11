using ArchLucid.Api.Auth.Models;
using ArchLucid.Api.Contracts;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Queries;
using ArchLucid.Provenance;
using ArchLucid.Provenance.Analysis;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Read-only HTTP surface for authority runs and golden-manifest summaries scoped to the caller’s tenant/workspace/project.
/// </summary>
/// <remarks>
/// Delegates to <see cref="IAuthorityQueryService"/>; routes under <c>api/authority</c>. Run detail returns <see cref="RunDetailDto"/> directly (embedded domain models).
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/authority")]
[EnableRateLimiting("fixed")]
public sealed class AuthorityQueryController(
    IAuthorityQueryService queryService,
    IScopeContextProvider scopeProvider,
    IProvenanceBuilder provenanceBuilder) : ControllerBase
{
    /// <summary>Lists recent runs for an authority project slug (e.g. <c>default</c>).</summary>
    /// <param name="projectId">Path segment: authority project id/slug, not the scope GUID.</param>
    /// <param name="take">Max rows when <paramref name="page"/> is not set (default 20, clamped 1–200).</param>
    /// <param name="page">One-based page. When set, response is <see cref="PagedResponse{T}"/> of <see cref="RunSummaryResponse"/>.</param>
    /// <param name="pageSize">Page size when <paramref name="page"/> is set (clamped 1–200; default 50).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Newest-first. Without <paramref name="page"/>: JSON array. With <paramref name="page"/>: paged envelope.</returns>
    [HttpGet("projects/{projectId}/runs")]
    [ProducesResponseType(typeof(IReadOnlyList<RunSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<RunSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListRunsByProject(
        string projectId,
        [FromQuery] int take = 20,
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return this.BadRequestProblem("projectId is required.", ProblemTypes.BadRequest);

        ScopeContext scope = scopeProvider.GetCurrentScope();

        if (page.HasValue)
        {
            (int safePage, int safePageSize) = PaginationDefaults.Normalize(page.Value, pageSize);
            int skip = PaginationDefaults.ToSkip(safePage, safePageSize);
            (IReadOnlyList<RunSummaryDto> items, int total) =
                await queryService.ListRunsByProjectPagedAsync(scope, projectId, skip, safePageSize, ct);

            IReadOnlyList<RunSummaryResponse> mapped = items.Select(ToRunSummaryResponse).ToList();

            return Ok(PagedResponseBuilder.FromDatabasePage(mapped, total, safePage, safePageSize));
        }

        take = Math.Clamp(take, 1, 200);
        IReadOnlyList<RunSummaryDto> results = await queryService.ListRunsByProjectAsync(scope, projectId, take, ct);

        return Ok(results.Select(ToRunSummaryResponse).ToList());
    }

    [HttpGet("runs/{runId:guid}/summary")]
    [ProducesResponseType(typeof(RunSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRunSummary(
        Guid runId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunSummaryDto? result = await queryService.GetRunSummaryAsync(scope, runId, ct);
        return result is null ? this.NotFoundProblem($"Run summary '{runId}' was not found.", ProblemTypes.RunNotFound) : Ok(ToRunSummaryResponse(result));
    }

    /// <summary>Loads full run detail including hydrated snapshots and golden manifest when available.</summary>
    /// <param name="runId">Run to load.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="RunDetailDto"/> JSON, or 404 when missing or out of scope.</returns>
    [HttpGet("runs/{runId:guid}")]
    [ProducesResponseType(typeof(RunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRunDetail(
        Guid runId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? result = await queryService.GetRunDetailAsync(scope, runId, ct);
        return result is null ? this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound) : Ok(result);
    }

    /// <summary>Gets compact counts/metadata for a golden manifest in the current scope.</summary>
    /// <param name="manifestId">Manifest primary key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ManifestSummaryResponse"/>, or 404 when unknown or out of scope.</returns>
    [HttpGet("manifests/{manifestId:guid}/summary")]
    [ProducesResponseType(typeof(ManifestSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetManifestSummary(
        Guid manifestId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        ManifestSummaryDto? result = await queryService.GetManifestSummaryAsync(scope, manifestId, ct);
        if (result is null)
            return this.NotFoundProblem($"Manifest '{manifestId}' was not found.", ProblemTypes.ManifestNotFound);

        return Ok(new ManifestSummaryResponse
        {
            ManifestId = result.ManifestId,
            RunId = result.RunId,
            CreatedUtc = result.CreatedUtc,
            ManifestHash = result.ManifestHash,
            RuleSetId = result.RuleSetId,
            RuleSetVersion = result.RuleSetVersion,
            DecisionCount = result.DecisionCount,
            WarningCount = result.WarningCount,
            UnresolvedIssueCount = result.UnresolvedIssueCount,
            Status = result.Status,
            HasWarnings = result.WarningCount > 0,
            HasUnresolvedIssues = result.UnresolvedIssueCount > 0,
            OperatorSummary =
                $"{result.DecisionCount} decisions, {result.WarningCount} warnings, {result.UnresolvedIssueCount} unresolved issues, status {result.Status}",
        });
    }

    /// <summary>
    /// Returns a structural provenance graph (nodes + edges) linking graph, findings, rules, decisions, manifest, and artifacts.
    /// </summary>
    /// <remarks>Requires a completed authority pipeline; coordinator-only runs return 422.</remarks>
    [HttpGet("runs/{runId:guid}/provenance")]
    [ProducesResponseType(typeof(DecisionProvenanceGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetRunProvenance(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? detail = await queryService.GetRunDetailAsync(scope, runId, ct);

        if (detail is null)
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        if (detail.GoldenManifest is null ||
            detail.GraphSnapshot is null ||
            detail.FindingsSnapshot is null ||
            detail.AuthorityTrace is null)
        {
            return this.UnprocessableEntityProblem(
                "Provenance requires golden manifest, graph snapshot, findings snapshot, and authority decision trace. " +
                "Coordinator-only or in-progress runs do not satisfy this contract.");
        }

        IReadOnlyList<SynthesizedArtifact> artifacts = detail.ArtifactBundle?.Artifacts ?? [];
        DecisionProvenanceGraph graph = provenanceBuilder.Build(
            detail.Run.RunId,
            detail.FindingsSnapshot,
            detail.GraphSnapshot,
            detail.GoldenManifest,
            detail.AuthorityTrace,
            artifacts);

        ProvenanceCompletenessResult completeness = ProvenanceCompletenessAnalyzer.Analyze(graph);

        ArchLucidInstrumentation.ProvenanceCompleteness.Record(
            completeness.CoverageRatio,
            new KeyValuePair<string, object?>("surface", "authority_query"));

        return Ok(graph);
    }

    private static RunSummaryResponse ToRunSummaryResponse(RunSummaryDto x) =>
        new()
        {
            RunId = x.RunId,
            ProjectId = x.ProjectId,
            Description = x.Description,
            CreatedUtc = x.CreatedUtc,
            ContextSnapshotId = x.ContextSnapshotId,
            GraphSnapshotId = x.GraphSnapshotId,
            FindingsSnapshotId = x.FindingsSnapshotId,
            GoldenManifestId = x.GoldenManifestId,
            DecisionTraceId = x.DecisionTraceId,
            ArtifactBundleId = x.ArtifactBundleId,
            HasContextSnapshot = x.HasContextSnapshot,
            HasGraphSnapshot = x.HasGraphSnapshot,
            HasFindingsSnapshot = x.HasFindingsSnapshot,
            HasGoldenManifest = x.HasGoldenManifest,
            HasDecisionTrace = x.HasDecisionTrace,
            HasArtifactBundle = x.HasArtifactBundle,
        };
}
