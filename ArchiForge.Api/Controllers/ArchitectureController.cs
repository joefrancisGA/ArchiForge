using ArchiForge.Api.Models;
using ArchiForge.Api.Services;
using ArchiForge.Application;
using ArchiForge.Application.Diagrams;
using ArchiForge.Data.Repositories;
using ArchiForge.Contracts.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
public sealed class ArchitectureController : ControllerBase
{
    private readonly IArchitectureRunService _architectureRunService;
    private readonly IArchitectureApplicationService _architectureApplicationService;
    private readonly IGoldenManifestRepository _manifestRepository;
    private readonly IDecisionTraceRepository _decisionTraceRepository;
    private readonly IDiagramGenerator _diagramGenerator;

    public ArchitectureController(
        IArchitectureRunService architectureRunService,
        IArchitectureApplicationService architectureApplicationService,
        IGoldenManifestRepository manifestRepository,
        IDecisionTraceRepository decisionTraceRepository,
        IDiagramGenerator diagramGenerator)
    {
        _architectureRunService = architectureRunService;
        _architectureApplicationService = architectureApplicationService;
        _manifestRepository = manifestRepository;
        _decisionTraceRepository = decisionTraceRepository;
        _diagramGenerator = diagramGenerator;
    }

    [HttpPost("request")]
    [ProducesResponseType(typeof(CreateArchitectureRunResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRun(
        [FromBody] ArchitectureRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        try
        {
            var result = await _architectureRunService.CreateRunAsync(request, cancellationToken);

            var response = new CreateArchitectureRunResponse
            {
                Run = result.Run,
                EvidenceBundle = result.EvidenceBundle,
                Tasks = result.Tasks
            };

            return CreatedAtAction(
                nameof(GetRun),
                new { runId = result.Run.RunId },
                response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("run/{runId}/execute")]
    [ProducesResponseType(typeof(ExecuteRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _architectureRunService.ExecuteRunAsync(runId, cancellationToken);

            var response = new ExecuteRunResponse
            {
                RunId = result.RunId,
                Results = result.Results
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("run/{runId}/commit")]
    [ProducesResponseType(typeof(CommitRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _architectureRunService.CommitRunAsync(runId, cancellationToken);

            var response = new CommitRunResponse
            {
                Manifest = result.Manifest,
                DecisionTraces = result.DecisionTraces,
                Warnings = result.Warnings
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("run/{runId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var data = await _architectureApplicationService.GetRunAsync(runId, cancellationToken);
        if (data is null)
        {
            return NotFound(new { error = $"Run '{runId}' was not found." });
        }

        return Ok(new
        {
            run = data.Run,
            tasks = data.Tasks,
            results = data.Results
        });
    }

    [HttpGet("manifest/{version}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifest(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await _architectureApplicationService.GetManifestAsync(version, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new { error = $"Manifest '{version}' was not found." });
        }

        return Ok(manifest);
    }

    [HttpGet("manifest/{version}/diagram")]
    [ProducesResponseType(typeof(DiagramResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestDiagram(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await _architectureApplicationService.GetManifestAsync(version, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new { error = $"Manifest '{version}' was not found." });
        }

        var mermaid = _diagramGenerator.GenerateMermaid(manifest);

        var response = new DiagramResponse
        {
            ManifestVersion = version,
            Format = "mermaid",
            Diagram = mermaid
        };

        return Ok(response);
    }

    [HttpGet("run/{runId}/full")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunFull(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var data = await _architectureApplicationService.GetRunAsync(runId, cancellationToken);
        if (data is null)
        {
            return NotFound(new { error = $"Run '{runId}' was not found." });
        }

        object? manifest = null;
        IEnumerable<object> decisionTraces = [];

        if (!string.IsNullOrWhiteSpace(data.Run.CurrentManifestVersion))
        {
            manifest = await _manifestRepository.GetByVersionAsync(data.Run.CurrentManifestVersion, cancellationToken);
            decisionTraces = await _decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken);
        }

        return Ok(new
        {
            run = data.Run,
            tasks = data.Tasks,
            results = data.Results,
            manifest,
            decisionTraces
        });
    }
}
