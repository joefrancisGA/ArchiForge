using ArchiForge.Api.Diagnostics;
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
    private readonly IArchitectureRequestRepository _requestRepository;

    public ArchitectureController(
        ICoordinatorService coordinatorService,
        IDecisionEngineService decisionEngineService,
        IArchitectureRunRepository runRepository,
        IAgentTaskRepository taskRepository,
        IAgentResultRepository resultRepository,
        IGoldenManifestRepository manifestRepository,
        IEvidenceBundleRepository evidenceBundleRepository,
        IDecisionTraceRepository decisionTraceRepository,
        IArchitectureRequestRepository requestRepository)
    {
        _coordinatorService = coordinatorService;
        _decisionEngineService = decisionEngineService;
        _runRepository = runRepository;
        _taskRepository = taskRepository;
        _resultRepository = resultRepository;
        _manifestRepository = manifestRepository;
        _evidenceBundleRepository = evidenceBundleRepository;
        _decisionTraceRepository = decisionTraceRepository;
        _requestRepository = requestRepository;
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

        await _requestRepository.CreateAsync(request, cancellationToken);
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

        var request = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken);
        if (request is null)
        {
            return NotFound(new
            {
                error = $"ArchitectureRequest '{run.RequestId}' for run '{runId}' was not found."
            });
        }

        var allResults = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        if (allResults.Count == 0)
        {
            return BadRequest(new { error = "Cannot commit run with no agent results." });
        }

        var manifestVersion = string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
            ? "v1"
            : IncrementManifestVersion(run.CurrentManifestVersion);

        var merge = _decisionEngineService.MergeResults(
            runId,
            request,
            manifestVersion,
            allResults,
            parentManifestVersion: run.CurrentManifestVersion);

        if (!merge.Success)
        {
            await _runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.Failed,
                currentManifestVersion: run.CurrentManifestVersion,
                completedUtc: DateTime.UtcNow,
                cancellationToken: cancellationToken);

            return BadRequest(new
            {
                errors = merge.Errors,
                warnings = merge.Warnings
            });
        }

        await _manifestRepository.CreateAsync(merge.Manifest, cancellationToken);
        await _decisionTraceRepository.CreateManyAsync(merge.DecisionTraces, cancellationToken);

        await _runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.Committed,
            currentManifestVersion: merge.Manifest.Metadata.ManifestVersion,
            completedUtc: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        var response = new CommitRunResponse
        {
            Manifest = merge.Manifest,
            DecisionTraces = merge.DecisionTraces,
            Warnings = merge.Warnings
        };

        return Ok(response);
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

    private static string IncrementManifestVersion(string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
            return "v1";

        if (currentVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(currentVersion[1..], out var versionNumber))
        {
            return $"v{versionNumber + 1}";
        }

        return "v1";
    }

    [HttpPost("run/{runId}/seed-fake-results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeedFakeResults(
    [FromRoute] string runId,
    CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return NotFound(new { error = $"Run '{runId}' was not found." });
        }

        var architectureRequest = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken);
        if (architectureRequest is null)
        {
            return NotFound(new
            {
                error = $"ArchitectureRequest '{run.RequestId}' for run '{runId}' was not found."
            });
        }

        var tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);
        if (tasks.Count == 0)
        {
            return BadRequest(new { error = "No tasks exist for this run." });
        }

        var fakeResults = FakeAgentResultFactory.CreateStarterResults(runId, tasks, architectureRequest);

        foreach (var result in fakeResults)
        {
            await _resultRepository.CreateAsync(result, cancellationToken);
        }

        await _runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.ReadyForCommit,
            currentManifestVersion: run.CurrentManifestVersion,
            completedUtc: null,
            cancellationToken: cancellationToken);

        return Ok(new
        {
            message = "Fake results seeded.",
            runId,
            resultCount = fakeResults.Count
        });
    }
}