using ArchLucid.Api.Auth.Models;
using ArchLucid.Api.Mapping;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Architecture;
using ArchLucid.Application.Common;
using ArchLucid.Application.Determinism;
using ArchLucid.Application.Runs;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Services;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// HTTP API for architecture runs: submit <see cref="ArchitectureRequest"/>, execute agents, replay, commit manifests, and query evidence, traces, and decisions.
/// </summary>
/// <remarks>
/// Base route <c>v1/architecture</c>. Mutating endpoints require <see cref="ArchLucidPolicies.ExecuteAuthority"/>; reads use <see cref="ArchLucidPolicies.ReadAuthority"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed partial class RunsController(
    IArchitectureRunService architectureRunService,
    IReplayRunService replayRunService,
    IArchitectureApplicationService architectureApplicationService,
    IRunDetailQueryService runDetailQueryService,
    IArchitectureRunProvenanceService architectureRunProvenanceService,
    IDeterminismCheckService determinismCheckService,
    IRunRepository authorityRunRepository,
    IDecisionNodeRepository decisionNodeRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IAgentExecutionTraceRepository agentExecutionTraceRepository,
    IScopeContextProvider scopeContextProvider,
    IActorContext actorContext,
    ILogger<RunsController> logger)
    : ControllerBase
{
    // Required by LoggerMessage source generator (SYSLIB1019): concrete ILogger field named _logger.

    /// <summary>
    /// Creates a run, evidence bundle, and starter tasks from <paramref name="request"/>; supports <c>Idempotency-Key</c> replay semantics.
    /// </summary>
    /// <returns>201 with <see cref="CreateArchitectureRunResponse"/> for new runs, or 200 with <c>Idempotency-Replayed</c> header when the key matches a prior success.</returns>
    [HttpPost("request")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(CreateArchitectureRunResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CreateArchitectureRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRun(
        [FromBody] ArchitectureRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)

            return this.BadRequestProblem("Request body is required.", ProblemTypes.ValidationFailed);


        string user = actorContext.GetActor();
        string correlationId = HttpContext.TraceIdentifier;

        if (Request.Headers.TryGetValue("Idempotency-Key", out Microsoft.Extensions.Primitives.StringValues rawKeyHeader))
        {
            string trimmedKey = rawKeyHeader.ToString().Trim();

            if (trimmedKey.Length > ArchitectureRunIdempotencyHashing.MaxIdempotencyKeyLength)

                return this.BadRequestProblem(
                    $"Idempotency-Key must be at most {ArchitectureRunIdempotencyHashing.MaxIdempotencyKeyLength} characters after trim.",
                    ProblemTypes.ValidationFailed);

        }

        CreateRunIdempotencyState? idempotency = TryBuildCreateRunIdempotency(request);

        try
        {
            CreateRunResult result = await architectureRunService.CreateRunAsync(request, idempotency, cancellationToken);

            CreateArchitectureRunResponse response = RunResponseMapper.ToCreateRunResponse(result.Run, result.EvidenceBundle, result.Tasks);

            LogRunCreated(result.Run.RunId, request.RequestId, user, correlationId);

            if (!result.IdempotentReplay)
                return CreatedAtAction(
                    nameof(GetRun),
                    new
                    {
                        runId = result.Run.RunId
                    },
                    response);
            Response.Headers.Append("Idempotency-Replayed", "true");

            return Ok(response);

        }
        catch (ConflictException ex)
        {
            logger.LogWarning(ex, "CreateRun conflict for request '{RequestId}'.", request.RequestId);

            return this.ConflictProblem(ex.Message, ProblemTypes.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "CreateRun failed for request '{RequestId}'.", request.RequestId);
            return this.InvalidOperationProblem(ex, ProblemTypes.BadRequest);
        }
    }

    private CreateRunIdempotencyState? TryBuildCreateRunIdempotency(ArchitectureRequest request)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out Microsoft.Extensions.Primitives.StringValues raw) ||
            string.IsNullOrWhiteSpace(raw.ToString()))

            return null;


        string trimmed = raw.ToString().Trim();

        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        return new CreateRunIdempotencyState(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ArchitectureRunIdempotencyHashing.HashIdempotencyKey(trimmed),
            ArchitectureRunIdempotencyHashing.FingerprintRequest(request));
    }

    /// <summary>
    /// Dispatches all pending tasks for <paramref name="runId"/> through the agent executor and persists results.
    /// </summary>
    /// <returns><see cref="ExecuteRunResponse"/> with agent results.</returns>
    [HttpPost("run/{runId}/execute")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(ExecuteRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("expensive")]
    public async Task<IActionResult> ExecuteRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        string user = User.Identity?.Name ?? "anonymous";
        string correlationId = HttpContext.TraceIdentifier;

        try
        {
            ExecuteRunResult result = await architectureRunService.ExecuteRunAsync(runId, cancellationToken);

            ExecuteRunResponse response = RunResponseMapper.ToExecuteRunResponse(result.RunId, result.Results);

            LogRunExecuted(runId, result.Results.Count, user, correlationId);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "ExecuteRun failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.BadRequest);
        }
        catch (RunNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    /// <summary>
    /// Re-executes agents for <paramref name="runId"/> under <paramref name="request"/>.<see cref="ReplayRunRequest.ExecutionMode"/> and optionally commits a replay manifest.
    /// </summary>
    [HttpPost("run/{runId}/replay")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(ReplayRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("expensive")]
    public async Task<IActionResult> ReplayRun(
        [FromRoute] string runId,
        [FromBody] ReplayRunRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ReplayRunRequest();

        string user = User.Identity?.Name ?? "anonymous";
        string correlationId = HttpContext.TraceIdentifier;

        try
        {
            ReplayRunResult result = await replayRunService.ReplayAsync(
                runId,
                request.ExecutionMode,
                request.CommitReplay,
                request.ManifestVersionOverride,
                cancellationToken);

            ReplayRunResponse response = RunResponseMapper.ToReplayRunResponse(
                result.OriginalRunId,
                result.ReplayRunId,
                result.ExecutionMode,
                result.Results,
                result.Manifest,
                result.DecisionTraces,
                result.Warnings);

            LogRunReplayed(result.OriginalRunId, result.ReplayRunId, result.ExecutionMode, user, correlationId);

            return Ok(response);
        }
        catch (RunNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "ReplayRun failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
    }

    /// <summary>
    /// Runs bounded replay iterations for <paramref name="runId"/> to compare agent results and manifest hashes against the baseline run.
    /// </summary>
    [HttpPost("run/{runId}/determinism-check")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(DeterminismCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("expensive")]
    public async Task<IActionResult> RunDeterminismCheck(
        [FromRoute] string runId,
        [FromBody] DeterminismCheckRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        request.RunId = runId;

        try
        {
            DeterminismCheckResult result = await determinismCheckService.RunAsync(request, cancellationToken);

            return Ok(new DeterminismCheckResponse
            {
                Result = result
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "DeterminismCheck failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
    }

    /// <summary>
    /// Merges agent results through the decision engine and persists the golden manifest and decision traces for <paramref name="runId"/>.
    /// </summary>
    [HttpPost("run/{runId}/commit")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [Authorize(Policy = ArchLucidPolicies.CanCommitRuns)]
    [ProducesResponseType(typeof(CommitRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CommitRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        string user = actorContext.GetActor();
        string correlationId = HttpContext.TraceIdentifier;

        try
        {
            CommitRunResult result = await architectureRunService.CommitRunAsync(runId, cancellationToken);

            CommitRunResponse response = RunResponseMapper.ToCommitRunResponse(
                result.Manifest,
                result.DecisionTraces,
                result.Warnings);

            LogRunCommitted(
                runId,
                result.Manifest.Metadata.ManifestVersion,
                result.Warnings.Count,
                user,
                correlationId);

            return Ok(response);
        }
        catch (ConflictException ex)
        {
            logger.LogWarning(ex, "CommitRun conflict for run '{RunId}'.", runId);
            return this.ConflictProblem(ex.Message, ProblemTypes.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "CommitRun failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
        catch (RunNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    /// <summary>
    /// Returns the canonical run aggregate (tasks, results, manifest, decision traces) for <paramref name="runId"/>.
    /// </summary>
    [HttpGet("run/{runId}")]
    [ProducesResponseType(typeof(RunDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);

        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);

        // A committed run that lost its manifest is a storage-integrity issue, not a simple 404.
        if (!string.IsNullOrWhiteSpace(detail.Run.CurrentManifestVersion) && detail.Manifest is null)
            return this.NotFoundProblem($"Manifest referenced by run '{runId}' could not be found.", ProblemTypes.ResourceNotFound);

        RunDetailsResponse response = RunResponseMapper.ToRunDetailsResponse(
            detail.Run,
            detail.Tasks,
            detail.Results,
            detail.Manifest,
            detail.DecisionTraces);

        return Ok(response);
    }

    /// <summary>
    /// Returns the coordinator linkage graph (request, tasks, results, findings, manifest, traces, decisions) and a sorted trace timeline.
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
    /// Accepts one <see cref="ArchLucid.Contracts.Agents.AgentResult"/> for an in-progress run (custom agent integrations).
    /// </summary>
    [HttpPost("run/{runId}/result")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(SubmitAgentResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAgentResult(
        [FromRoute] string runId,
        [FromBody] SubmitAgentResultRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.ValidationFailed);

        SubmitResultResult result = await architectureApplicationService.SubmitAgentResultAsync(runId, request.Result, cancellationToken);

        return result.Success ? Ok(new SubmitAgentResultResponse { ResultId = result.ResultId! }) : MapApplicationServiceFailure(result.Error, result.FailureKind, "Submission failed.");
    }

    [HttpPost("run/{runId}/seed-fake-results")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(SeedFakeResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "CanSeedResults")]
    public async Task<IActionResult> SeedFakeResults(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        string user = actorContext.GetActor();
        string correlationId = HttpContext.TraceIdentifier;

        SeedFakeResultsResult result = await architectureApplicationService.SeedFakeResultsAsync(runId, cancellationToken);
        if (!result.Success)
            return MapApplicationServiceFailure(result.Error, result.FailureKind, "Seed failed.");

        LogFakeResultsSeeded(runId, result.ResultCount, user, correlationId);

        return Ok(new SeedFakeResultsResponse { ResultCount = result.ResultCount });
    }

    /// <summary>
    /// Returns decision-tree nodes materialized for <paramref name="runId"/> after commit (empty before commit yields 404).
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


        return Ok(new DecisionNodeResponse
        {
            Decisions = decisions.ToList()
        });
    }

    /// <summary>
    /// Returns the hydrated <see cref="AgentEvidencePackage"/> used when agents ran for <paramref name="runId"/>.
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
        if (evidence is null)

            return this.NotFoundProblem($"Evidence for run '{runId}' was not found.", ProblemTypes.ResourceNotFound);


        return Ok(new AgentEvidencePackageResponse
        {
            Evidence = evidence
        });
    }

    /// <summary>
    /// Returns a page of <see cref="AgentExecutionTrace"/> rows for <paramref name="runId"/> (prompt/response audit trail).
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

        if (pageSize < 1 || pageSize > PagingParameters.MaxPageSize)
            return this.BadRequestProblem(
                $"pageSize must be between 1 and {PagingParameters.MaxPageSize}.",
                ProblemTypes.ValidationFailed);

        if (!await AuthorityRunExistsInScopeAsync(runId, cancellationToken))

            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);


        PagingParameters paging = new()
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        (int skip, int take) = paging.Normalize();

        (IReadOnlyList<AgentExecutionTrace> pagedTraces, int totalCount) = await agentExecutionTraceRepository.GetPagedByRunIdAsync(
            runId,
            offset: skip,
            limit: take,
            cancellationToken: cancellationToken);

        return Ok(new AgentExecutionTraceResponse
        {
            Traces = pagedTraces.ToList(),
            TotalCount = totalCount,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        });
    }

    /// <summary>
    /// Lists recent runs visible in the current scope (summary rows for dashboards and pickers).
    /// </summary>
    [HttpGet("runs")]
    [ProducesResponseType(typeof(List<RunListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuns(CancellationToken cancellationToken)
    {
        IReadOnlyList<RunSummary> summaries = await runDetailQueryService.ListRunSummariesAsync(cancellationToken);

        List<RunListItemResponse> response = summaries
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

        return Ok(response);
    }

    private async Task<bool> AuthorityRunExistsInScopeAsync(string runId, CancellationToken cancellationToken)
    {
        if (!TryParseRunId(runId, out Guid runGuid))
        {
            return false;
        }

        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        return await authorityRunRepository.GetByIdAsync(scope, runGuid, cancellationToken) is not null;
    }

    private static bool TryParseRunId(string runId, out Guid runGuid)
    {
        if (Guid.TryParseExact(runId, "N", out runGuid))
        {
            return true;
        }

        return Guid.TryParse(runId, out runGuid);
    }

    private IActionResult MapApplicationServiceFailure(string? error, ApplicationServiceFailureKind? kind, string defaultBadRequestDetail)
    {
        string detail = string.IsNullOrWhiteSpace(error) ? defaultBadRequestDetail : error;
        return kind switch
        {
            ApplicationServiceFailureKind.RunNotFound => this.NotFoundProblem(detail, ProblemTypes.RunNotFound),
            ApplicationServiceFailureKind.ResourceNotFound => this.NotFoundProblem(detail, ProblemTypes.ResourceNotFound),
            _ => this.BadRequestProblem(detail),
        };
    }
}

