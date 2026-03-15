using ArchiForge.Api.Models;
using ArchiForge.Api.Services;
using ArchiForge.Application;
using ArchiForge.Application.Diagrams;
using ArchiForge.Application.Exports;
using ArchiForge.Application.Summaries;
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
    private readonly IArchitectureRunRepository _runRepository;
    private readonly IGoldenManifestRepository _manifestRepository;
    private readonly IDecisionTraceRepository _decisionTraceRepository;
    private readonly IDiagramGenerator _diagramGenerator;
    private readonly IManifestSummaryGenerator _summaryGenerator;
    private readonly IArchitectureExportService _exportService;
    private readonly IAgentEvidencePackageRepository _agentEvidencePackageRepository;

    public ArchitectureController(
        IArchitectureRunService architectureRunService,
        IArchitectureApplicationService architectureApplicationService,
        IArchitectureRunRepository runRepository,
        IGoldenManifestRepository manifestRepository,
        IDecisionTraceRepository decisionTraceRepository,
        IDiagramGenerator diagramGenerator,
        IManifestSummaryGenerator summaryGenerator,
        IArchitectureExportService exportService,
        IAgentEvidencePackageRepository agentEvidencePackageRepository)
    {
        _architectureRunService = architectureRunService;
        _architectureApplicationService = architectureApplicationService;
        _runRepository = runRepository;
        _manifestRepository = manifestRepository;
        _decisionTraceRepository = decisionTraceRepository;
        _diagramGenerator = diagramGenerator;
        _summaryGenerator = summaryGenerator;
        _exportService = exportService;
        _agentEvidencePackageRepository = agentEvidencePackageRepository;
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

    [HttpPost("run/{runId}/result")]
    [ProducesResponseType(typeof(SubmitAgentResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitAgentResult(
        [FromRoute] string runId,
        [FromBody] SubmitAgentResultRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Result is null)
        {
            return BadRequest(new { error = "Agent result is required." });
        }

        var result = await _architectureApplicationService.SubmitAgentResultAsync(runId, request.Result, cancellationToken);
        if (!result.Success)
        {
            if (result.Error is not null && result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { error = result.Error });
            }
            return BadRequest(new { error = result.Error });
        }

        return Ok(new SubmitAgentResultResponse { ResultId = result.ResultId! });
    }

    [HttpPost("run/{runId}/seed-fake-results")]
    [ProducesResponseType(typeof(SeedFakeResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeedFakeResults(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var result = await _architectureApplicationService.SeedFakeResultsAsync(runId, cancellationToken);
        if (!result.Success)
        {
            if (result.Error is not null && result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { error = result.Error });
            }
            return BadRequest(new { error = result.Error });
        }

        return Ok(new SeedFakeResultsResponse { ResultCount = result.ResultCount });
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

    [HttpGet("manifest/{version}/summary")]
    [ProducesResponseType(typeof(ManifestSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestSummary(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await _manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new { error = $"Manifest '{version}' was not found." });
        }

        var evidence = await _agentEvidencePackageRepository.GetByRunIdAsync(manifest.RunId, cancellationToken);
        var markdown = _summaryGenerator.GenerateMarkdown(manifest, evidence);

        var response = new ManifestSummaryResponse
        {
            ManifestVersion = version,
            Format = "markdown",
            Summary = markdown
        };

        return Ok(response);
    }

    [HttpGet("manifest/{version}/bundle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestBundle(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await _manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new { error = $"Manifest '{version}' was not found." });
        }

        var evidence = await _agentEvidencePackageRepository.GetByRunIdAsync(manifest.RunId, cancellationToken);
        var diagram = _diagramGenerator.GenerateMermaid(manifest);
        var summary = _summaryGenerator.GenerateMarkdown(manifest, evidence);

        return Ok(new
        {
            manifestVersion = version,
            manifest,
            diagram,
            summary
        });
    }

    [HttpGet("manifest/{version}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestExport(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await _manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new { error = $"Manifest '{version}' was not found." });
        }

        var evidence = await _agentEvidencePackageRepository.GetByRunIdAsync(manifest.RunId, cancellationToken);
        var diagram = _diagramGenerator.GenerateMermaid(manifest);
        var summary = _summaryGenerator.GenerateMarkdown(manifest, evidence);
        var markdown = _exportService.GenerateMarkdownPackage(manifest, diagram, summary, evidence);

        return Ok(new { manifestVersion = version, format = "markdown", content = markdown });
    }

    [HttpGet("manifest/{version}/export/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadManifestExport(
        [FromRoute] string version,
        CancellationToken cancellationToken)
    {
        var manifest = await _manifestRepository.GetByVersionAsync(version, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new { error = $"Manifest '{version}' was not found." });
        }

        var evidence = await _agentEvidencePackageRepository.GetByRunIdAsync(manifest.RunId, cancellationToken);
        var diagram = _diagramGenerator.GenerateMermaid(manifest);
        var summary = _summaryGenerator.GenerateMarkdown(manifest, evidence);
        var markdown = _exportService.GenerateMarkdownPackage(manifest, diagram, summary, evidence);

        var fileName = $"architecture-export-{version}.md";
        return File(
            System.Text.Encoding.UTF8.GetBytes(markdown),
            "text/markdown",
            fileName);
    }

    [HttpGet("run/{runId}/evidence")]
    [ProducesResponseType(typeof(AgentEvidencePackageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunEvidence(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return NotFound(new { error = $"Run '{runId}' was not found." });
        }

        var evidence = await _agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken);
        if (evidence is null)
        {
            return NotFound(new { error = $"Evidence for run '{runId}' was not found." });
        }

        return Ok(new AgentEvidencePackageResponse
        {
            Evidence = evidence
        });
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
