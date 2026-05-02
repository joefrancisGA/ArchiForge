using System.Text.Json;

using ArchLucid.Api.Logging;
using ArchLucid.Api.Mapping;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Common;
using ArchLucid.Application.Notifications.Email;
using ArchLucid.Application.Runs;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using ArchLucid.Persistence.Serialization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;

namespace ArchLucid.Api.Controllers.Authority;

/// <summary>
///     HTTP API for mutating architecture runs: create, execute, commit, replay, submit agent results.
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
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed partial class RunsController(
    IArchitectureRunCreateOrchestrator architectureRunCreateOrchestrator,
    IArchitectureRunExecuteOrchestrator architectureRunExecuteOrchestrator,
    IArchitectureRunCommitOrchestrator architectureRunCommitOrchestrator,
    IArchitectureApplicationService architectureApplicationService,
    IReplayRunService replayRunService,
    IScopeContextProvider scopeContextProvider,
    IActorContext actorContext,
    IAuditService auditService,
    ICommitSponsorEmailNotifier commitSponsorEmailNotifier,
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
    [HttpPost("/v{version:apiVersion}/requests")]
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
                await architectureRunCreateOrchestrator.CreateRunAsync(request, idempotency, cancellationToken);

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
    [HttpPost("/v{version:apiVersion}/runs/{runId}/submit")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(ExecuteRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [EnableRateLimiting("expensive")]
    public async Task<IActionResult> ExecuteRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        string user = actorContext.GetActor();
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

            ExecuteRunResult result =
                await architectureRunExecuteOrchestrator.ExecuteRunAsync(runId, cancellationToken);

            ExecuteRunResponse response = RunResponseMapper.ToExecuteRunResponse(result.RunId, result.Results);

            LogRunExecuted(runId, result.Results.Count, user, correlationId);

            await LogRunSubmittedAuditAsync(runId, user, cancellationToken);

            if (!pilotTryRealMode)
                return Ok(response);

            ArchLucidInstrumentation.RecordTryRealModePilotSucceeded();
            await LogPilotTryRealModeAuditAsync(
                AuditEventTypes.FirstRealValueRunCompleted,
                runId,
                user,
                cancellationToken);

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
    ///     Merges agent results through the decision engine and persists the golden manifest and decision traces for
    ///     <paramref name="runId" />.
    /// </summary>
    [HttpPost("run/{runId}/commit")]
    [HttpPost("/v{version:apiVersion}/runs/{runId}/manifest/finalize")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [Authorize(Policy = ArchLucidPolicies.CanCommitRuns)]
    [ProducesResponseType(typeof(CommitRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CommitRun(
        [FromRoute] string runId,
        [FromBody] CommitRunRequest? request,
        CancellationToken cancellationToken)
    {
        string user = actorContext.GetActor();
        string correlationId = HttpContext.TraceIdentifier;

        try
        {
            CommitRunResult result = await architectureRunCommitOrchestrator.CommitRunAsync(runId, cancellationToken);

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

            if (request?.NotifySponsor != true)
                return Ok(response);

            Guid tenantId = scopeContextProvider.GetCurrentScope().TenantId;

            await commitSponsorEmailNotifier
                .NotifyAfterCommitAsync(tenantId, runId, cancellationToken)
                .ConfigureAwait(false);

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
            return this.InvalidOperationProblem(ex, ProblemTypes.BusinessRuleViolation);
        }
        catch (RunNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    /// <summary>
    ///     Re-executes agents for <paramref name="runId" /> from cloned tasks/evidence, optionally committing a replay manifest.
    /// </summary>
    [HttpPost("run/{runId}/replay")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(ReplayRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [EnableRateLimiting("expensive")]
    public async Task<IActionResult> ReplayRun(
        [FromRoute] string runId,
        [FromBody] ReplayRunRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ReplayRunRequest();

        string user = actorContext.GetActor();
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

            ScopeContext scope = scopeContextProvider.GetCurrentScope();
            string auditActor = actorContext.GetActor();
            Guid? auditRunId = Guid.TryParse(result.OriginalRunId, out Guid originalParsed) ? originalParsed : null;

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.ReplayExecuted,
                    ActorUserId = auditActor,
                    ActorUserName = auditActor,
                    TenantId = scope.TenantId,
                    WorkspaceId = scope.WorkspaceId,
                    ProjectId = scope.ProjectId,
                    RunId = auditRunId,
                    CorrelationId = correlationId,
                    DataJson = JsonSerializer.Serialize(new
                    {
                        result.OriginalRunId,
                        result.ReplayRunId,
                        resolvedExecutionMode = result.ExecutionMode,
                        requestedExecutionMode = request.ExecutionMode,
                        request.CommitReplay,
                        request.ManifestVersionOverride
                    },
                        AuditJsonSerializationOptions.Instance)
                },
                cancellationToken);

            logger.LogInformation(
                "Run replayed: OriginalRunId={OriginalRunId}, ReplayRunId={ReplayRunId}, ExecutionMode={ExecutionMode}, User={User}, CorrelationId={CorrelationId}",
                result.OriginalRunId,
                result.ReplayRunId,
                result.ExecutionMode,
                user,
                correlationId);

            return Ok(response);
        }
        catch (RunNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarningWithSanitizedUserArg(ex, "ReplayRun failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.BusinessRuleViolation);
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

    private async Task LogRunSubmittedAuditAsync(string runId, string actor, CancellationToken cancellationToken)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        Guid? runGuid = TryParseRunGuidForAudit(runId);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.RunSubmitted,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runGuid
            },
            cancellationToken);
    }

    private bool IsPilotTryRealModeRequest()
    {
        return Request.Headers.TryGetValue(PilotTryRealModeHeaders.PilotTryRealMode, out StringValues raw) &&
               string.Equals(raw.ToString().Trim(), "1", StringComparison.Ordinal);
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
