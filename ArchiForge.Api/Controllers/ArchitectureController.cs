using ArchiForge.Api.Models;
using ArchiForge.Api.Services;
using ArchiForge.Application;
using ArchiForge.Application.Diffs;
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
    private readonly IReplayRunService _replayRunService;
    private readonly IArchitectureApplicationService _architectureApplicationService;
    private readonly IArchitectureRunRepository _runRepository;
    private readonly IGoldenManifestRepository _manifestRepository;
    private readonly IDecisionTraceRepository _decisionTraceRepository;
    private readonly IDiagramGenerator _diagramGenerator;
    private readonly IManifestSummaryGenerator _summaryGenerator;
    private readonly IArchitectureExportService _exportService;
    private readonly IAgentEvidencePackageRepository _agentEvidencePackageRepository;
    private readonly IAgentExecutionTraceRepository _agentExecutionTraceRepository;
    private readonly IManifestDiffService _manifestDiffService;
    private readonly IManifestDiffSummaryFormatter _manifestDiffSummaryFormatter;
    private readonly IManifestDiffExportService _manifestDiffExportService;

    public ArchitectureController(
        IArchitectureRunService architectureRunService,
        IReplayRunService replayRunService,
        IArchitectureApplicationService architectureApplicationService,
        IArchitectureRunRepository runRepository,
        IGoldenManifestRepository manifestRepository,
        IDecisionTraceRepository decisionTraceRepository,
        IDiagramGenerator diagramGenerator,
        IManifestSummaryGenerator summaryGenerator,
        IArchitectureExportService exportService,
        IAgentEvidencePackageRepository agentEvidencePackageRepository,
        IAgentExecutionTraceRepository agentExecutionTraceRepository,
        IManifestDiffService manifestDiffService,
        IManifestDiffSummaryFormatter manifestDiffSummaryFormatter,
        IManifestDiffExportService manifestDiffExportService)
    {
        _architectureRunService = architectureRunService;
        _replayRunService = replayRunService;
        _architectureApplicationService = architectureApplicationService;
        _runRepository = runRepository;
        _manifestRepository = manifestRepository;
        _decisionTraceRepository = decisionTraceRepository;
        _diagramGenerator = diagramGenerator;
        _summaryGenerator = summaryGenerator;
        _exportService = exportService;
        _agentEvidencePackageRepository = agentEvidencePackageRepository;
        _agentExecutionTraceRepository = agentExecutionTraceRepository;
        _manifestDiffService = manifestDiffService;
        _manifestDiffSummaryFormatter = manifestDiffSummaryFormatter;
        _manifestDiffExportService = manifestDiffExportService;
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

    [HttpPost("run/{runId}/replay")]
    [ProducesResponseType(typeof(ReplayRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayRun(
        [FromRoute] string runId,
        [FromBody] ReplayRunRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ReplayRunRequest();

        try
        {
            var result = await _replayRunService.ReplayAsync(
                runId,
                request.ExecutionMode,
                request.CommitReplay,
                request.ManifestVersionOverride,
                cancellationToken);

            return Ok(new ReplayRunResponse
            {
                OriginalRunId = result.OriginalRunId,
                ReplayRunId = result.ReplayRunId,
                ExecutionMode = result.ExecutionMode,
                Results = result.Results,
                Manifest = result.Manifest,
                DecisionTraces = result.DecisionTraces,
                Warnings = result.Warnings
            });
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

    [HttpGet("manifest/compare")]
    [ProducesResponseType(typeof(ManifestCompareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareManifests(
        [FromQuery] string leftVersion,
        [FromQuery] string rightVersion,
        CancellationToken cancellationToken)
    {
        var left = await _manifestRepository.GetByVersionAsync(leftVersion, cancellationToken);
        if (left is null)
        {
            return NotFound(new { error = $"Manifest '{leftVersion}' was not found." });
        }

        var right = await _manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
        {
            return NotFound(new { error = $"Manifest '{rightVersion}' was not found." });
        }

        var diff = _manifestDiffService.Compare(left, right);

        return Ok(new ManifestCompareResponse
        {
            LeftManifest = left,
            RightManifest = right,
            Diff = diff
        });
    }

    [HttpGet("manifest/compare/summary")]
    [ProducesResponseType(typeof(ManifestCompareSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareManifestsSummary(
        [FromQuery] string leftVersion,
        [FromQuery] string rightVersion,
        CancellationToken cancellationToken)
    {
        var left = await _manifestRepository.GetByVersionAsync(leftVersion, cancellationToken);
        if (left is null)
        {
            return NotFound(new { error = $"Manifest '{leftVersion}' was not found." });
        }

        var right = await _manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
        {
            return NotFound(new { error = $"Manifest '{rightVersion}' was not found." });
        }

        var diff = _manifestDiffService.Compare(left, right);
        var summary = _manifestDiffSummaryFormatter.FormatMarkdown(diff);

        return Ok(new ManifestCompareSummaryResponse
        {
            LeftManifestVersion = leftVersion,
            RightManifestVersion = rightVersion,
            Format = "markdown",
            Summary = summary,
            Diff = diff
        });
    }

    [HttpGet("manifest/compare/export")]
    [ProducesResponseType(typeof(ManifestCompareExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareManifestsExport(
        [FromQuery] string leftVersion,
        [FromQuery] string rightVersion,
        CancellationToken cancellationToken)
    {
        var left = await _manifestRepository.GetByVersionAsync(leftVersion, cancellationToken);
        if (left is null)
        {
            return NotFound(new { error = $"Manifest '{leftVersion}' was not found." });
        }

        var right = await _manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
        {
            return NotFound(new { error = $"Manifest '{rightVersion}' was not found." });
        }

        var diff = _manifestDiffService.Compare(left, right);
        var summary = _manifestDiffSummaryFormatter.FormatMarkdown(diff);
        var content = _manifestDiffExportService.GenerateMarkdownExport(left, right, diff, summary);

        return Ok(new ManifestCompareExportResponse
        {
            LeftManifestVersion = leftVersion,
            RightManifestVersion = rightVersion,
            Format = "markdown",
            FileName = $"compare_{leftVersion}_to_{rightVersion}.md",
            Content = content
        });
    }

    [HttpGet("manifest/compare/export/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadCompareManifestsExport(
        [FromQuery] string leftVersion,
        [FromQuery] string rightVersion,
        CancellationToken cancellationToken)
    {
        var left = await _manifestRepository.GetByVersionAsync(leftVersion, cancellationToken);
        if (left is null)
        {
            return NotFound(new { error = $"Manifest '{leftVersion}' was not found." });
        }

        var right = await _manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
        {
            return NotFound(new { error = $"Manifest '{rightVersion}' was not found." });
        }

        var diff = _manifestDiffService.Compare(left, right);
        var summary = _manifestDiffSummaryFormatter.FormatMarkdown(diff);
        var content = _manifestDiffExportService.GenerateMarkdownExport(left, right, diff, summary);

        var fileName = $"compare_{leftVersion}_to_{rightVersion}.md";
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);

        return File(bytes, "text/markdown", fileName);
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

    [HttpGet("run/{runId}/traces")]
    [ProducesResponseType(typeof(AgentExecutionTraceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunTraces(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return NotFound(new { error = $"Run '{runId}' was not found." });
        }

        var traces = await _agentExecutionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

        return Ok(new AgentExecutionTraceResponse
        {
            Traces = traces.ToList()
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
        object? evidence = null;
        IEnumerable<object> decisionTraces = [];
        IEnumerable<object> agentExecutionTraces = [];

        if (!string.IsNullOrWhiteSpace(data.Run.CurrentManifestVersion))
        {
            manifest = await _manifestRepository.GetByVersionAsync(data.Run.CurrentManifestVersion, cancellationToken);
            decisionTraces = await _decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken);
        }

        evidence = await _agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken);
        agentExecutionTraces = await _agentExecutionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

        return Ok(new
        {
            run = data.Run,
            tasks = data.Tasks,
            results = data.Results,
            manifest,
            evidence,
            decisionTraces,
            agentExecutionTraces
        });
    }
}
