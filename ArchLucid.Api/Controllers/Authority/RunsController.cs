using ArchLucid.Api.Attributes;
using ArchLucid.Api.Logging;
using ArchLucid.Api.Mapping;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Common;
using ArchLucid.Application.Determinism;
using ArchLucid.Application.Runs;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Services;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;

namespace ArchLucid.Api.Controllers.Authority;

/// <summary>
///     HTTP API for mutating architecture run operations: create, execute, replay, commit, submit results, and seed.
/// </summary>
/// <remarks>
///     Base route <c>v1/architecture</c>. Read-only endpoints live on <see cref="RunQueryController" /> and
///     <see cref="RunAgentEvaluationController" />.
///     Mutating endpoints require <see cref="ArchLucidPolicies.ExecuteAuthority" />; reads use
///     <see cref="ArchLucidPolicies.ReadAuthority" />.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
[CoordinatorPipelineDeprecated]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed partial class RunsController(
    IArchitectureRunService architectureRunService,
    IReplayRunService replayRunService,
    IArchitectureApplicationService architectureApplicationService,
    IDeterminismCheckService determinismCheckService,
    IScopeContextProvider scopeContextProvider,
    IActorContext actorContext,
    IAuditService auditService,
    ILogger<RunsController> logger)
    : ControllerBase
{
    // Required by LoggerMessage source generator (SYSLIB1019): concrete ILogger field named _logger.

    /// <summary>
    ///     Creates a run, evidence bundle, and starter tasks from <paramref name="request" />; supports <c>Idempotency-Key</c>
    ///     replay semantics.
    /// </summary>
    /// <returns>
    ///     201 with <see cref="CreateArchitectureRunResponse" /> for new runs, or 200 with <c>Idempotency-Replayed</c>
    ///     header when the key matches a prior success.
    /// </returns>
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

        if (Request.Headers.TryGetValue("Idempotency-Key", out StringValues rawKeyHeader))
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
            CreateRunResult result =
                await architectureRunService.CreateRunAsync(request, idempotency, cancellationToken);

            CreateArchitectureRunResponse response =
                RunResponseMapper.ToCreateRunResponse(result.Run, result.EvidenceBundle, result.Tasks);

            LogRunCreated(result.Run.RunId, request.RequestId, user, correlationId);

            if (!result.IdempotentReplay)
                return CreatedAtAction(
                    nameof(RunQueryController.GetRun),
                    "RunQuery",
                    new { runId = result.Run.RunId },
                    response);

            Response.Headers.Append("Idempotency-Replayed", "true");

            return Ok(response);
        }
        catch (ConflictException ex)
        {
            logger.LogWarningWithSanitizedUserArg(ex, "CreateRun conflict for request '{RequestId}'.",
                request.RequestId);

            return this.ConflictProblem(ex.Message, ProblemTypes.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarningWithSanitizedUserArg(ex, "CreateRun failed for request '{RequestId}'.", request.RequestId);
            return this.InvalidOperationProblem(ex, ProblemTypes.BadRequest);
        }
    }

    private CreateRunIdempotencyState? TryBuildCreateRunIdempotency(ArchitectureRequest request)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out StringValues raw) ||
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
    ///     Dispatches all pending tasks for <paramref name="runId" /> through the agent executor and persists results.
    /// </summary>
    /// <returns><see cref="ExecuteRunResponse" /> with agent results.</returns>
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
        bool pilotTryRealMode = IsPilotTryRealModeRequest();

        try
        {
            if (pilotTryRealMode)
            {
                ArchLucidInstrumentation.RecordTryRealModePilotAttempted();
                await LogPilotTryRealModeAuditAsync(
                    AuditEventTypes.FirstRealValueRunStarted,
                    runId,
                    user,
                    cancellationToken);
            }

            ExecuteRunResult result = await architectureRunService.ExecuteRunAsync(runId, cancellationToken);

            ExecuteRunResponse response = RunResponseMapper.ToExecuteRunResponse(result.RunId, result.Results);

            LogRunExecuted(runId, result.Results.Count, user, correlationId);

            if (pilotTryRealMode)
            {
                ArchLucidInstrumentation.RecordTryRealModePilotSucceeded();
                await LogPilotTryRealModeAuditAsync(
                    AuditEventTypes.FirstRealValueRunCompleted,
                    runId,
                    user,
                    cancellationToken);
            }

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarningWithSanitizedUserArg(ex, "ExecuteRun failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.BadRequest);
        }
        catch (RunNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    /// <summary>
    ///     Re-executes agents for <paramref name="runId" /> under <paramref name="request" />.
    ///     <see cref="ReplayRunRequest.ExecutionMode" /> and optionally commits a replay manifest.
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
            logger.LogWarningWithSanitizedUserArg(ex, "ReplayRun failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
    }

    /// <summary>
    ///     Runs bounded replay iterations for <paramref name="runId" /> to compare agent results and manifest hashes against
    ///     the baseline run.
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

            return Ok(new DeterminismCheckResponse { Result = result });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarningWithSanitizedUserArg(ex, "DeterminismCheck failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
    }

    /// <summary>
    ///     Merges agent results through the decision engine and persists the golden manifest and decision traces for
    ///     <paramref name="runId" />.
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
        catch (PreCommitGovernanceBlockedException ex)
        {
            logger.LogWarningWithSanitizedUserArg(
                ex,
                "CommitRun blocked by pre-commit governance for run '{RunId}'.",
                runId);
            return this.GovernancePreCommitBlockedProblem(ex.Result);
        }
        catch (ConflictException ex)
        {
            logger.LogWarningWithSanitizedUserArg(ex, "CommitRun conflict for run '{RunId}'.", runId);
            return this.ConflictProblem(ex.Message, ProblemTypes.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarningWithSanitizedUserArg(ex, "CommitRun failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
        catch (RunNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    /// <summary>
    ///     Accepts one <see cref="ArchLucid.Contracts.Agents.AgentResult" /> for an in-progress run (custom agent
    ///     integrations).
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

        SubmitResultResult result =
            await architectureApplicationService.SubmitAgentResultAsync(runId, request.Result, cancellationToken);

        return result.Success
            ? Ok(new SubmitAgentResultResponse { ResultId = result.ResultId! })
            : MapApplicationServiceFailure(result.Error, result.FailureKind, "Submission failed.");
    }

    [HttpPost("run/{runId}/seed-fake-results")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(SeedFakeResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "CanSeedResults")]
    public async Task<IActionResult> SeedFakeResults(
        [FromRoute] string runId,
        [FromQuery] bool pilotTryRealModeFellBack = false,
        CancellationToken cancellationToken = default)
    {
        string user = actorContext.GetActor();
        string correlationId = HttpContext.TraceIdentifier;

        PilotSeedFakeResultsOptions? pilot =
            pilotTryRealModeFellBack ? new PilotSeedFakeResultsOptions(true) : null;

        SeedFakeResultsResult result =
            await architectureApplicationService.SeedFakeResultsAsync(runId, pilot, cancellationToken);
        if (!result.Success)
            return MapApplicationServiceFailure(result.Error, result.FailureKind, "Seed failed.");

        LogFakeResultsSeeded(runId, result.ResultCount, user, correlationId);

        return Ok(new SeedFakeResultsResponse { ResultCount = result.ResultCount });
    }

    private bool IsPilotTryRealModeRequest()
    {
        if (!Request.Headers.TryGetValue(PilotTryRealModeHeaders.PilotTryRealMode, out StringValues raw))
            return false;


        return string.Equals(raw.ToString().Trim(), "1", StringComparison.Ordinal);
    }

    private async Task LogPilotTryRealModeAuditAsync(
        string eventType,
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        Guid? runGuid = TryParseRunGuidForAudit(runId);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = eventType,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runGuid
            },
            cancellationToken);
    }

    private static Guid? TryParseRunGuidForAudit(string runId)
    {
        if (Guid.TryParseExact(runId, "N", out Guid g))
            return g;


        return Guid.TryParse(runId, out g) ? g : null;
    }

    private IActionResult MapApplicationServiceFailure(string? error, ApplicationServiceFailureKind? kind,
        string defaultBadRequestDetail)
    {
        string detail = string.IsNullOrWhiteSpace(error) ? defaultBadRequestDetail : error;
        return kind switch
        {
            ApplicationServiceFailureKind.RunNotFound => this.NotFoundProblem(detail, ProblemTypes.RunNotFound),
            ApplicationServiceFailureKind.ResourceNotFound => this.NotFoundProblem(detail,
                ProblemTypes.ResourceNotFound),
            _ => this.BadRequestProblem(detail)
        };
    }
}
