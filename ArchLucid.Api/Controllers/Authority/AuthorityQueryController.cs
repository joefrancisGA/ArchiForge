using ArchLucid.Api.Contracts;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Audit;
using ArchLucid.Application.Common;
using ArchLucid.Application.Explanation;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Decisioning.Models;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Queries;
using ArchLucid.Provenance;
using ArchLucid.Provenance.Analysis;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Authority;

/// <summary>
///     Read-only HTTP surface for authority runs and golden-manifest summaries scoped to the caller’s
///     tenant/workspace/project.
/// </summary>
/// <remarks>
///     Delegates to <see cref="IAuthorityQueryService" />; routes under <c>api/authority</c>. Run detail returns
///     <see cref="RunDetailDto" /> directly (embedded domain models).
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/authority")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status429TooManyRequests)]
public sealed class AuthorityQueryController(
    IAuthorityQueryService queryService,
    IRunRationaleService runRationaleService,
    IRunPipelineAuditTimelineService pipelineAuditTimeline,
    IScopeContextProvider scopeProvider,
    IProvenanceBuilder provenanceBuilder,
    IAuditService auditService,
    IActorContext actorContext) : ControllerBase
{
    /// <summary>
    ///     Lists runs for an authority project slug (e.g. <c>default</c>). Prefer <paramref name="cursor" /> +
    ///     <paramref name="take" /> (stable keyset). Legacy <paramref name="page" />/<paramref name="pageSize" /> is kept
    ///     only for page 1.
    /// </summary>
    /// <param name="projectId">Path segment: authority project id/slug, not the scope GUID.</param>
    /// <param name="cursor">Opaque next-cursor token from the previous response.</param>
    /// <param name="take">Max rows when using cursor mode (default per <see cref="RunPagination.DefaultTake" />).</param>
    /// <param name="page">
    ///     Legacy only: must be <c>1</c>. Page <c>&gt;</c><c>1</c> requires passing <paramref name="cursor" />.
    /// </param>
    /// <param name="pageSize">
    ///     Legacy page size when <paramref name="page" /> is set (ignored when <paramref name="cursor" /> is supplied).
    /// </param>
    /// <returns>Newest-first <see cref="CursorPagedResponse{T}" /> of <see cref="RunSummaryResponse" />.</returns>
    [HttpGet("projects/{projectId}/runs")]
    [ProducesResponseType(typeof(CursorPagedResponse<RunSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListRunsByProject(
        string projectId,
        [FromQuery] string? cursor = null,
        [FromQuery] int take = RunPagination.DefaultTake,
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return this.BadRequestProblem("projectId is required.", ProblemTypes.BadRequest);

        if (page is int beyondFirst && beyondFirst > 1 && string.IsNullOrWhiteSpace(cursor))

            return this.BadRequestProblem(
                "Paging beyond page 1 requires the nextCursor token from the prior response.",
                ProblemTypes.BadRequest);

        DateTime? cu = null;
        Guid? rid = null;

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            (DateTime CreatedUtc, Guid RunId)? decoded = RunCursorCodec.TryDecode(cursor.Trim());

            if (!decoded.HasValue)

                return this.BadRequestProblem("cursor is invalid.", ProblemTypes.ValidationFailed);

            cu = decoded.Value.CreatedUtc;

            rid = decoded.Value.RunId;
        }

        int effectiveTake =
            string.IsNullOrWhiteSpace(cursor) && page.HasValue
                ? RunPagination.ClampTake(pageSize)
                : RunPagination.ClampTake(take);

        ScopeContext scope = scopeProvider.GetCurrentScope();
            await queryService.ListRunsByProjectKeysetAsync(scope, projectId, cu, rid, effectiveTake, ct);

        string? nextCursor =
            hasMore && items.Count > 0 ? RunCursorCodec.Encode(items[^1].CreatedUtc, items[^1].RunId) : null;

        IReadOnlyList<RunSummaryResponse> mapped = items.Select(ToRunSummaryResponse).ToList();

        return Ok(
            new CursorPagedResponse<RunSummaryResponse>

            {

                Items = mapped,

                NextCursor = nextCursor,

                HasMore = hasMore,

                RequestedTake = effectiveTake

            });
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
        return result is null
            ? this.NotFoundProblem($"Run summary '{runId}' was not found.", ProblemTypes.RunNotFound)
            : Ok(ToRunSummaryResponse(result));
    }

    /// <summary>Loads full run detail including hydrated snapshots and golden manifest when available.</summary>
    /// <param name="runId">Run to load.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="RunDetailDto" /> JSON, or 404 when missing or out of scope.</returns>
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
        return result is null
            ? this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound)
            : Ok(result);
    }

    /// <summary>
    ///     Audit events associated with this run, oldest-first (pipeline / lifecycle visibility for operators).
    /// </summary>
    [HttpGet("runs/{runId:guid}/pipeline-timeline")]
    [HttpGet("/v{version:apiVersion}/runs/{runId:guid}/review-trail")]
    [ProducesResponseType(typeof(IReadOnlyList<RunPipelineTimelineItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunPipelineTimeline(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        IReadOnlyList<RunPipelineTimelineItemDto>? items =
            await pipelineAuditTimeline.GetTimelineAsync(scope, runId, ct);

        if (items is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);


        IReadOnlyList<RunPipelineTimelineItemResponse> body = items
            .Select(i => new RunPipelineTimelineItemResponse
            {
                EventId = i.EventId,
                OccurredUtc = i.OccurredUtc,
                EventType = i.EventType,
                ActorUserName = i.ActorUserName,
                CorrelationId = i.CorrelationId
            })
            .ToList();

        await LogRunScopedAuditAsync(AuditEventTypes.ReviewTrailAccessed, runId, manifestId: null, ct);

        return Ok(body);
    }

    /// <summary>Unified decision rationale (authority or coordinator) for operator triage.</summary>
    [HttpGet("runs/{runId:guid}/rationale")]
    [HttpGet("/v{version:apiVersion}/runs/{runId:guid}/review-trail/rationale")]
    [ProducesResponseType(typeof(RunRationale), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRunRationale(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunRationale? rationale = await runRationaleService.GetRunRationaleAsync(scope, runId, ct);

        return rationale is null
            ? this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound)
            : Ok(rationale);
    }

    /// <summary>Gets compact counts/metadata for a golden manifest in the current scope.</summary>
    /// <param name="manifestId">Manifest primary key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ManifestSummaryResponse" />, or 404 when unknown or out of scope.</returns>
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
                $"{result.DecisionCount} decisions, {result.WarningCount} warnings, {result.UnresolvedIssueCount} unresolved issues, status {result.Status}"
        });
    }

    /// <summary>
    ///     Returns a structural provenance graph (nodes + edges) linking graph, findings, rules, decisions, manifest, and
    ///     artifacts.
    /// </summary>
    /// <remarks>Requires a completed authority pipeline; coordinator-only runs return 422.</remarks>
    [HttpGet("runs/{runId:guid}/provenance")]
    [HttpGet("/v{version:apiVersion}/runs/{runId:guid}/review-trail/provenance")]
    [ProducesResponseType(typeof(DecisionProvenanceGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetRunProvenance(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? detail = await queryService.GetRunDetailAsync(scope, runId, ct);

        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);


        if (detail.GoldenManifest is null ||
            detail.GraphSnapshot is null ||
            detail.FindingsSnapshot is null ||
            detail.AuthorityTrace is null)

            return this.UnprocessableEntityProblem(
                "Provenance requires golden manifest, graph snapshot, findings snapshot, and authority decision trace. " +
                "Coordinator-only or in-progress runs do not satisfy this contract.");


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

        await LogRunScopedAuditAsync(AuditEventTypes.ProvenanceAccessed, runId, manifestId: null, ct);

        return Ok(graph);
    }

    /// <summary>Returns the hydrated golden manifest JSON for the run when committed.</summary>
    [HttpGet("runs/{runId:guid}/manifest")]
    [HttpGet("/v{version:apiVersion}/runs/{runId:guid}/manifest")]
    [ProducesResponseType(typeof(ManifestDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRunGoldenManifest(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? detail = await queryService.GetRunDetailAsync(scope, runId, ct);

        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);


        if (detail.GoldenManifest is null)
            return this.NotFoundProblem(
                $"Golden manifest for run '{runId}' was not found.",
                ProblemTypes.ManifestNotFound);


        await LogRunScopedAuditAsync(
            AuditEventTypes.ManifestViewed,
            runId,
            manifestId: detail.GoldenManifest.ManifestId,
            ct);

        return Ok(detail.GoldenManifest);
    }

    private async Task LogRunScopedAuditAsync(string eventType, Guid runId, Guid? manifestId, CancellationToken ct)
    {
        string actor = actorContext.GetActor();
        ScopeContext scope = scopeProvider.GetCurrentScope();

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = eventType,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runId,
                ManifestId = manifestId
            },
            ct);
    }

    private static RunSummaryResponse ToRunSummaryResponse(RunSummaryDto x)
    {
        return new RunSummaryResponse
        {
            RunId = x.RunId,
            ProjectId = x.ProjectId,
            Description = x.Description,
            CreatedUtc = x.CreatedUtc,
            HasContextSnapshot = x.HasContextSnapshot,
            HasGraphSnapshot = x.HasGraphSnapshot,
            HasFindingsSnapshot = x.HasFindingsSnapshot,
            HasGoldenManifest = x.HasGoldenManifest,
            HasDecisionTrace = x.HasDecisionTrace,
            HasArtifactBundle = x.HasArtifactBundle
        };
    }
}
