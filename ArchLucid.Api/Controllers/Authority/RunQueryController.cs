using System.Globalization;

using ArchLucid.Api.Mapping;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Architecture;
using ArchLucid.Application.Explanation;
using ArchLucid.Application.Traceability;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ArchLucid.Api.Support;

namespace ArchLucid.Api.Controllers.Authority;

/// <summary>
///     Read-only HTTP API for architecture runs: detail, provenance, decisions, evidence, traces, and list.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status429TooManyRequests)]
public sealed class RunQueryController(
    IRunDetailQueryService runDetailQueryService,
    IRunRoiEstimator runRoiEstimator,
    IArchitectureRunProvenanceService architectureRunProvenanceService,
    IRunRepository authorityRunRepository,
    IDecisionNodeRepository decisionNodeRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IAgentExecutionTraceRepository agentExecutionTraceRepository,
    IFindingEvidenceChainService findingEvidenceChainService,
    IFindingInspectReadRepository findingInspectReadRepository,
    IScopeContextProvider scopeContextProvider,
    ITraceabilityBundleBuilder traceabilityBundleBuilder,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    ///     Returns the canonical run aggregate (tasks, results, manifest, decision traces) for <paramref name="runId" />.
    /// </summary>
    [HttpGet("run/{runId}")]
    [HttpGet("/v{version:apiVersion}/runs/{runId}")]
    [ProducesResponseType(typeof(RunDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);

        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);

        if (!string.IsNullOrWhiteSpace(detail.Run.CurrentManifestVersion) && detail.Manifest is null)
            return this.NotFoundProblem(
                $"Manifest referenced by run '{runId}' could not be found.",
                ProblemTypes.ResourceNotFound);

        RunDetailsResponse response = RunResponseMapper.ToRunDetailsResponse(
            detail.Run,
            detail.Tasks,
            detail.Results,
            detail.Manifest,
            detail.DecisionTraces);

        response.ExecutionFlavorBuyerSummary = RunExecutionFlavorSummary.Build(
            detail.Run,
            configuration["AgentExecution:Mode"]);

        return Ok(response);
    }

    /// <summary>Directional analyst-hour estimate for packaging work implied by this run (configured multipliers).</summary>
    [HttpGet("run/{runId}/roi")]
    [ProducesResponseType(typeof(RunRoiScorecardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunRoiEstimate(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);

        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);

        RunRoiScorecardDto estimate = runRoiEstimator.Estimate(detail);

        return Ok(estimate);
    }

    /// <summary>
    ///     Returns the coordinator linkage graph (request, tasks, results, findings, manifest, traces, decisions) and a sorted
    ///     trace timeline.
    /// </summary>
    [HttpGet("runs/{runId}/provenance")]
    [ProducesResponseType(typeof(ArchitectureRunProvenanceGraph), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArchitectureRunProvenance(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        ArchitectureRunProvenanceGraph? graph = await architectureRunProvenanceService
            .GetProvenanceAsync(runId, cancellationToken);

        if (graph is null)
            return this.NotFoundProblem(
                $"Run '{runId}' was not found, or its manifest reference is broken.",
                ProblemTypes.RunNotFound);

        return Ok(graph);
    }

    /// <summary>
    ///     Returns decision-tree nodes materialized for <paramref name="runId" /> after commit (empty before commit yields
    ///     404).
    /// </summary>
    [HttpGet("run/{runId}/decisions")]
    [ProducesResponseType(typeof(DecisionNodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunDecisions(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        if (!await AuthorityRunExistsInScopeAsync(runId, cancellationToken))
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);

        IReadOnlyList<DecisionNode> decisions = await decisionNodeRepository.GetByRunIdAsync(runId, cancellationToken);

        if (decisions.Count == 0)
            return this.NotFoundProblem(
                $"No decisions found for run '{runId}'. Decisions are available after the run has been committed.",
                ProblemTypes.ResourceNotFound);

        return Ok(new DecisionNodeResponse { Decisions = decisions.ToList() });
    }

    /// <summary>
    ///     Returns the hydrated <see cref="AgentEvidencePackage" /> used when agents ran for <paramref name="runId" />.
    /// </summary>
    [HttpGet("run/{runId}/evidence")]
    [ProducesResponseType(typeof(AgentEvidencePackageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunEvidence(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        if (!await AuthorityRunExistsInScopeAsync(runId, cancellationToken))
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);

        AgentEvidencePackage? evidence = await agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken);
        return evidence is null
            ? this.NotFoundProblem($"Evidence for run '{runId}' was not found.", ProblemTypes.ResourceNotFound)
            : Ok(new AgentEvidencePackageResponse { Evidence = evidence });
    }

    /// <summary>
    ///     Returns a page of <see cref="AgentExecutionTrace" /> rows for <paramref name="runId" /> (prompt/response audit
    ///     trail).
    /// </summary>
    [HttpGet("run/{runId}/traces")]
    [ProducesResponseType(typeof(AgentExecutionTraceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunTraces(
        [FromRoute] string runId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            return this.BadRequestProblem("pageNumber must be at least 1.", ProblemTypes.ValidationFailed);

        if (pageSize is < 1 or > PagingParameters.MaxPageSize)
            return this.BadRequestProblem(
                $"pageSize must be between 1 and {PagingParameters.MaxPageSize}.",
                ProblemTypes.ValidationFailed);

        if (!await AuthorityRunExistsInScopeAsync(runId, cancellationToken))
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);

        PagingParameters paging = new() { PageNumber = pageNumber, PageSize = pageSize };
        (int skip, int take) = paging.Normalize();

        (IReadOnlyList<AgentExecutionTrace> pagedTraces, int totalCount) =
            await agentExecutionTraceRepository.GetPagedByRunIdAsync(
                runId,
                skip,
                take,
                cancellationToken);

        return Ok(new AgentExecutionTraceResponse
        {
            Traces = pagedTraces.ToList(),
            TotalCount = totalCount,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        });
    }

    /// <summary>
    ///     Lists runs visible in the current scope (keyset pagination with <paramref name="cursor" />). Legacy
    ///     <paramref name="page" /> is limited to page <c>1</c> without a cursor.
    /// </summary>
    [HttpGet("runs")]
    [HttpGet("/v{version:apiVersion}/runs")]
    [ProducesResponseType(typeof(CursorPagedResponse<RunListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuns(
        [FromQuery] string? cursor = null,
        [FromQuery] int take = RunPagination.DefaultTake,
        [FromQuery] int page = PaginationDefaults.DefaultPage,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (page > 1 && string.IsNullOrWhiteSpace(cursor))

            return this.BadRequestProblem(
                "Paging beyond page 1 requires the nextCursor token from the prior response.",
                ProblemTypes.ValidationFailed);

        (_, int normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);

        int effectiveTake =
            string.IsNullOrWhiteSpace(cursor)
                ? RunPagination.ClampTake(normalizedPageSize)
                : RunPagination.ClampTake(take);

        (IReadOnlyList<RunSummary> summaries, bool hasMore, string? nextCursor) =
            await runDetailQueryService.ListRunSummariesKeysetAsync(cursor, effectiveTake, cancellationToken);

        List<RunListItemResponse> mapped = summaries
            .Select(r => new RunListItemResponse
            {
                RunId = r.RunId,
                RequestId = r.RequestId,
                Status = r.Status,
                CreatedUtc = r.CreatedUtc,
                CompletedUtc = r.CompletedUtc,
                CurrentManifestVersion = r.CurrentManifestVersion,
                SystemName = r.SystemName
            })
            .ToList();

        return Ok(
            new CursorPagedResponse<RunListItemResponse>
            {
                Items = mapped, NextCursor = nextCursor, HasMore = hasMore, RequestedTake = effectiveTake
            });
    }


    /// <summary>
    ///     Returns persisted artifact pointers for one finding (manifest snapshot ids, graph nodes, agent trace ids).
    /// </summary>
    [HttpGet("run/{runId}/findings/{findingId}/evidence-chain")]
    [ProducesResponseType(typeof(FindingEvidenceChainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFindingEvidenceChain(
        [FromRoute] string runId,
        [FromRoute] string findingId,
        CancellationToken cancellationToken)
    {
        FindingEvidenceChainResponse? chain =
            await findingEvidenceChainService.BuildAsync(runId, findingId, cancellationToken);

        if (chain is null)
            return this.NotFoundProblem(
                $"Evidence chain is not available for run '{runId}' and finding '{findingId}'.",
                ProblemTypes.ResourceNotFound);

        return Ok(chain);
    }

    /// <summary>
    ///     Same payload as <c>GET /v1/findings/{findingId}/inspect</c>; returns <c>404</c> when the finding&apos;s persisted
    ///     run identifier does not match <paramref name="runId" /> (prevents cross-run ambiguity in deep links).
    /// </summary>
    [HttpGet("run/{runId}/findings/{findingId}/inspect")]
    [ProducesResponseType(typeof(FindingInspectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFindingInspectForRun(
        [FromRoute] string runId,
        [FromRoute] string findingId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return this.BadRequestProblem("Run id is required.", ProblemTypes.ValidationFailed);

        if (string.IsNullOrWhiteSpace(findingId))
            return this.BadRequestProblem("Finding id is required.", ProblemTypes.ValidationFailed);

        if (findingId.Trim().Length > 64)
            return this.BadRequestProblem("Finding id exceeds maximum length (64).", ProblemTypes.ValidationFailed);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        FindingInspectResponse? body =
            await findingInspectReadRepository.GetInspectAsync(scope, findingId.Trim(), cancellationToken);

        if (body is null)
        {
            return this.NotFoundProblem(
                $"Finding '{findingId.Trim()}' was not found in the current scope.",
                ProblemTypes.ResourceNotFound);
        }

        if (!SameAuthorityRunIdentifier(runId.Trim(), body.RunId))
        {
            return this.NotFoundProblem(
                $"Finding '{findingId.Trim()}' was not found for run '{runId.Trim()}'.",
                ProblemTypes.ResourceNotFound);
        }

        return Ok(body);
    }

    /// <summary>ZIP bundle: run summary, audit slice for the run, and decision traces (size-capped).</summary>
    [HttpGet("run/{runId}/traceability-bundle.zip")]
    [HttpGet("/v{version:apiVersion}/runs/{runId}/review-trail/export")]
    [Produces("application/zip")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> GetTraceabilityBundleZip(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        const long maxZipBytes = 1_500_000L;
        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        try
        {
            byte[]? zip = await traceabilityBundleBuilder.BuildAsync(runId, scope, maxZipBytes, cancellationToken);

            return zip is null
                ? this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound)
                : File(zip, "application/zip", $"traceability-{runId}.zip");
        }
        catch (TraceabilityBundleTooLargeException ex)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new
                {
                    title = "Traceability bundle exceeds size cap",
                    detail = ex.Message,
                    attemptedBytes = ex.AttemptedBytes,
                    maxBytes = ex.MaxBytes
                });
        }
    }

    private async Task<bool> AuthorityRunExistsInScopeAsync(string runId, CancellationToken cancellationToken)
    {
        if (!TryParseRunId(runId, out Guid runGuid))
            return false;

        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        return await authorityRunRepository.GetByIdAsync(scope, runGuid, cancellationToken) is not null;
    }

    private static bool TryParseRunId(string runId, out Guid runGuid)
    {
        return Guid.TryParseExact(runId, "N", out runGuid) || Guid.TryParse(runId, out runGuid);
    }

    /// <summary>Hyphen/format-insensitive GUID comparison (aligned with UI <c>sameAuthorityRunId</c>).</summary>
    private static bool SameAuthorityRunIdentifier(string routeRunId, Guid payloadRunId)
    {
        return string.Equals(
            Norm(routeRunId),
            Norm(payloadRunId.ToString("D", CultureInfo.InvariantCulture)),
            StringComparison.Ordinal);

        static string Norm(string value)
        {
            return value.Replace("-", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();
        }
    }
}
