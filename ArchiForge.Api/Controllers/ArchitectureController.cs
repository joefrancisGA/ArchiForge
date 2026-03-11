using ArchiForge.Api.Models;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Route("architecture")]
public sealed class ArchitectureController : ControllerBase
{
    private readonly ICoordinatorService _coordinatorService;
    private readonly IDecisionEngineService _decisionEngineService;
    private readonly IArchitectureRunRepository _runRepository;
    private readonly IAgentTaskRepository _taskRepository;
    private readonly IAgentResultRepository _resultRepository;
    private readonly IGoldenManifestRepository _manifestRepository;
    private readonly IEvidenceBundleRepository _evidenceBundleRepository;
    private readonly IDecisionTraceRepository _decisionTraceRepository;

    public ArchitectureController(
        ICoordinatorService coordinatorService,
        IDecisionEngineService decisionEngineService,
        IArchitectureRunRepository runRepository,
        IAgentTaskRepository taskRepository,
        IAgentResultRepository resultRepository,
        IGoldenManifestRepository manifestRepository,
        IEvidenceBundleRepository evidenceBundleRepository,
        IDecisionTraceRepository decisionTraceRepository)
    {
        _coordinatorService = coordinatorService;
        _decisionEngineService = decisionEngineService;
        _runRepository = runRepository;
        _taskRepository = taskRepository;
        _resultRepository = resultRepository;
        _manifestRepository = manifestRepository;
        _evidenceBundleRepository = evidenceBundleRepository;
        _decisionTraceRepository = decisionTraceRepository;
    }

    [HttpPost("request")]
    [ProducesResponseType(typeof(CreateArchitectureRunResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRun(
        [FromBody] ArchitectureRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        var coordination = _coordinatorService.CreateRun(request);

        if (!coordination.Success)
        {
            return BadRequest(new { errors = coordination.Errors });
        }

        await _runRepository.CreateAsync(coordination.Run, cancellationToken);
        await _evidenceBundleRepository.CreateAsync(coordination.EvidenceBundle, cancellationToken);
        await _taskRepository.CreateManyAsync(coordination.Tasks, cancellationToken);

        var response = new CreateArchitectureRunResponse
        {
            Run = coordination.Run,
            EvidenceBundle = coordination.EvidenceBundle,
            Tasks = coordination.Tasks
        };

        return CreatedAtAction(
            nameof(GetRun),
            new { runId = coordination.Run.RunId },
            response);
    }

    [HttpGet("run/{runId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return NotFound(new { error = $"Run '{runId}' was not found." });
        }

        var tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);
        var results = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);

        return Ok(new
        {
            run,
            tasks,
            results
        });
    }

    [HttpPost("run/{runId}/result")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAgentResult(
        [FromRoute] string runId,
        [FromBody] SubmitAgentResultRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Result is null)
        {
            return BadRequest(new { error = "Agent result is required." });
        }

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return NotFound(new { error = $"Run '{runId}' was not found." });
        }

        if (!string.Equals(request.Result.RunId, runId, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                error = $"Result RunId '{request.Result.RunId}' does not match route runId '{runId}'."
            });
        }

        await _resultRepository.CreateAsync(request.Result, cancellationToken);

        var allResults = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        if (allResults.Count >= 3 && run.Status == ArchitectureRunStatus.TasksGenerated)
        {
            await _runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.ReadyForCommit,
                currentManifestVersion: run.CurrentManifestVersion,
                completedUtc: null,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.WaitingForResults,
                currentManifestVersion: run.CurrentManifestVersion,
                completedUtc: null,
                cancellationToken: cancellationToken);
        }

        return Accepted(new
        {
            message = "Agent result accepted.",
            runId,
            resultId = request.Result.ResultId
        });
    }

    [HttpPost("run/{runId}/commit")]
    [ProducesResponseType(typeof(CommitRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return NotFound(new { error = $"Run '{runId}' was not found." });
        }

        var allResults = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        if (allResults.Count == 0)
        {
            return BadRequest(new { error = "Cannot commit run with no agent results." });
        }

        // In Week 1, request reconstruction can be replaced later by a real request repository.
        // For now, derive a minimal request from run metadata if needed, or preferably persist requests separately.
        // Here we assume you have the original request available from a request repository.
        return BadRequest(new
        {
            error = "Week 1 commit requires access to the original ArchitectureRequest. Add an IArchitectureRequestRepository and wire it here."
        });
    }

    [HttpGet("manifest/{version}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifest(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await _manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new { error = $"Manifest '{version}' was not found." });
        }

        return Ok(manifest);
    }
}