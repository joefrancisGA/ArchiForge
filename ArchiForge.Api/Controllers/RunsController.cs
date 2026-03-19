using ArchiForge.Api.Models;
using ArchiForge.Api.Mapping;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Application;
using ArchiForge.Application.Determinism;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Data.Repositories;
using ArchiForge.Contracts.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ApiKey")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
public sealed partial class RunsController(
    IArchitectureRunService architectureRunService,
    IReplayRunService replayRunService,
    IArchitectureApplicationService architectureApplicationService,
    IArchitectureRunRepository runRepository,
    IAgentTaskRepository taskRepository,
    IAgentResultRepository resultRepository,
    IGoldenManifestRepository manifestRepository,
    IDecisionTraceRepository decisionTraceRepository,
    IDeterminismCheckService determinismCheckService,
    IDecisionNodeRepository decisionNodeRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IAgentExecutionTraceRepository agentExecutionTraceRepository,
    ILogger<RunsController> logger)
    : ControllerBase
{
    // Explicit field for Microsoft.Extensions.Logging LoggerMessage source generator (SYSLIB1019 with primary constructors).
    private readonly ILogger<RunsController> _logger = logger;

    [HttpPost("request")]
    [ProducesResponseType(typeof(CreateArchitectureRunResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRun(
        [FromBody] ArchitectureRequest request,
        CancellationToken cancellationToken)
    {
        var user = User.Identity?.Name ?? "anonymous";
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var result = await architectureRunService.CreateRunAsync(request, cancellationToken);

            var response = RunResponseMapper.ToCreateRunResponse(result.Run, result.EvidenceBundle, result.Tasks);

            LogRunCreated(result.Run.RunId, request.RequestId, user, correlationId);

            return CreatedAtAction(
                nameof(GetRun),
                new { runId = result.Run.RunId },
                response);
        }
        catch (InvalidOperationException ex)
        {
            return this.InvalidOperationProblem(ex, ProblemTypes.BadRequest, ProblemTypes.RunNotFound);
        }
    }

    [HttpPost("run/{runId}/execute")]
    [ProducesResponseType(typeof(ExecuteRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("expensive")]
    public async Task<IActionResult> ExecuteRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var user = User.Identity?.Name ?? "anonymous";
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var result = await architectureRunService.ExecuteRunAsync(runId, cancellationToken);

            var response = RunResponseMapper.ToExecuteRunResponse(result.RunId, result.Results);

            LogRunExecuted(runId, result.Results.Count, user, correlationId);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return this.InvalidOperationProblem(ex, ProblemTypes.DeterminismFailed, ProblemTypes.RunNotFound);
        }
    }

    [HttpPost("run/{runId}/replay")]
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

        var user = User.Identity?.Name ?? "anonymous";
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var result = await replayRunService.ReplayAsync(
                runId,
                request.ExecutionMode,
                request.CommitReplay,
                request.ManifestVersionOverride,
                cancellationToken);

            var response = RunResponseMapper.ToReplayRunResponse(
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
        catch (InvalidOperationException ex)
        {
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed, ProblemTypes.RunNotFound);
        }
    }

    [HttpPost("run/{runId}/determinism-check")]
    [ProducesResponseType(typeof(DeterminismCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("expensive")]
    public async Task<IActionResult> RunDeterminismCheck(
        [FromRoute] string runId,
        [FromBody] DeterminismCheckRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new DeterminismCheckRequest();
        request.RunId = runId;

        try
        {
            var result = await determinismCheckService.RunAsync(request, cancellationToken);

            return Ok(new DeterminismCheckResponse
            {
                Result = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed, ProblemTypes.RunNotFound);
        }
    }

    [HttpPost("run/{runId}/commit")]
    [ProducesResponseType(typeof(CommitRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Policy = "CanCommitRuns")]
    public async Task<IActionResult> CommitRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var user = User.Identity?.Name ?? "anonymous";
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var result = await architectureRunService.CommitRunAsync(runId, cancellationToken);

            var response = RunResponseMapper.ToCommitRunResponse(
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
        catch (InvalidOperationException ex)
        {
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed, ProblemTypes.RunNotFound);
        }
    }

    [HttpGet("run/{runId}")]
    [ProducesResponseType(typeof(RunDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var response = await BuildRunDetailsResponseAsync(runId, cancellationToken);
        if (response is null)
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        return Ok(response);
    }

    [HttpPost("run/{runId}/result")]
    [ProducesResponseType(typeof(SubmitAgentResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAgentResult(
        [FromRoute] string runId,
        [FromBody] SubmitAgentResultRequest request,
        CancellationToken cancellationToken)
    {
        var result = await architectureApplicationService.SubmitAgentResultAsync(runId, request.Result, cancellationToken);

        if (result.Success)
            return Ok(new SubmitAgentResultResponse { ResultId = result.ResultId! });

        return MapApplicationServiceFailure(result.Error, result.FailureKind, "Submission failed.");
    }

    [HttpPost("run/{runId}/seed-fake-results")]
    [ProducesResponseType(typeof(SeedFakeResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "CanSeedResults")]
    public async Task<IActionResult> SeedFakeResults(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var user = User.Identity?.Name ?? "anonymous";
        var correlationId = HttpContext.TraceIdentifier;

        var result = await architectureApplicationService.SeedFakeResultsAsync(runId, cancellationToken);
        if (!result.Success)
            return MapApplicationServiceFailure(result.Error, result.FailureKind, "Seed failed.");

        LogFakeResultsSeeded(runId, result.ResultCount, user, correlationId);

        return Ok(new SeedFakeResultsResponse { ResultCount = result.ResultCount });
    }

    [HttpGet("run/{runId}/decisions")]
    [ProducesResponseType(typeof(DecisionNodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunDecisions(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var run = await runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        var decisions = await decisionNodeRepository.GetByRunIdAsync(runId, cancellationToken);

        return Ok(new DecisionNodeResponse
        {
            Decisions = decisions.ToList()
        });
    }

    [HttpGet("run/{runId}/evidence")]
    [ProducesResponseType(typeof(AgentEvidencePackageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunEvidence(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var run = await runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        var evidence = await agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken);
        if (evidence is null)
        {
            return this.NotFoundProblem($"Evidence for run '{runId}' was not found.", ProblemTypes.ResourceNotFound);
        }

        return Ok(new AgentEvidencePackageResponse
        {
            Evidence = evidence
        });
    }

    [HttpGet("run/{runId}/traces")]
    [ProducesResponseType(typeof(AgentExecutionTraceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunTraces(
        [FromRoute] string runId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var run = await runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        var allTraces = await agentExecutionTraceRepository.GetByRunIdAsync(runId, cancellationToken);
        var paging = new PagingParameters { PageNumber = pageNumber, PageSize = pageSize };
        var (skip, take) = paging.Normalize();

        var pagedTraces = allTraces
            .OrderBy(t => t.CreatedUtc)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Ok(new AgentExecutionTraceResponse
        {
            Traces = pagedTraces,
            TotalCount = allTraces.Count,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        });
    }

    [HttpGet("runs")]
    [ProducesResponseType(typeof(List<RunListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuns(CancellationToken cancellationToken)
    {
        var items = await runRepository.ListAsync(cancellationToken);

        var response = items
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

    private async Task<RunDetailsResponse?> BuildRunDetailsResponseAsync(
        string runId,
        CancellationToken cancellationToken)
    {
        var run = await runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return null;
        }

        var tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken);
        var results = await resultRepository.GetByRunIdAsync(runId, cancellationToken);

        GoldenManifest? manifest = null;
        List<DecisionTrace> decisionTraces = [];

        if (!string.IsNullOrWhiteSpace(run.CurrentManifestVersion))
        {
            manifest = await manifestRepository.GetByVersionAsync(run.CurrentManifestVersion, cancellationToken);
            decisionTraces = (await decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken)).ToList();
        }

        return RunResponseMapper.ToRunDetailsResponse(
            run,
            tasks.ToList(),
            results.ToList(),
            manifest,
            decisionTraces);
    }

    private IActionResult MapApplicationServiceFailure(string? error, ApplicationServiceFailureKind? kind, string defaultBadRequestDetail)
    {
        var detail = string.IsNullOrWhiteSpace(error) ? defaultBadRequestDetail : error!;
        return kind switch
        {
            ApplicationServiceFailureKind.RunNotFound => this.NotFoundProblem(detail, ProblemTypes.RunNotFound),
            ApplicationServiceFailureKind.ResourceNotFound => this.NotFoundProblem(detail, ProblemTypes.ResourceNotFound),
            _ => this.BadRequestProblem(detail),
        };
    }
}

