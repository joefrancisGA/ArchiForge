using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using ArchiForge.Application.Determinism;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Diagrams;
using ArchiForge.Application.Exports;
using ArchiForge.Application.Summaries;
using Microsoft.AspNetCore.Authorization;
using ArchiForge.Data.Repositories;
using ArchiForge.Contracts.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ApiConsultingDocxProfileRecommendationRequest =
    ArchiForge.Api.Models.ConsultingDocxProfileRecommendationRequest;
using AppConsultingDocxProfileRecommendationRequest =
    ArchiForge.Application.Analysis.ConsultingDocxProfileRecommendationRequest;
using AppConsultingDocxExportProfileSelector =
    ArchiForge.Application.Analysis.IConsultingDocxExportProfileSelector;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ApiKey")]
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
    private readonly IAgentResultRepository _resultRepository;
    private readonly IAgentResultDiffService _agentResultDiffService;
    private readonly IAgentResultDiffSummaryFormatter _agentResultDiffSummaryFormatter;
    private readonly IDeterminismCheckService _determinismCheckService;
    private readonly IArchitectureAnalysisService _architectureAnalysisService;
    private readonly IArchitectureAnalysisExportService _architectureAnalysisExportService;
    private readonly IArchitectureAnalysisDocxExportService _docxExportService;
    private readonly IArchitectureAnalysisConsultingDocxExportService _architectureAnalysisConsultingDocxExportService;
    private readonly IConsultingDocxTemplateRecommendationService _consultingDocxTemplateRecommendationService;
    private readonly AppConsultingDocxExportProfileSelector _consultingDocxExportProfileSelector;
    private readonly IRunExportAuditService _runExportAuditService;
    private readonly IRunExportRecordRepository _runExportRecordRepository;

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
        IManifestDiffExportService manifestDiffExportService,
        IAgentResultRepository resultRepository,
        IAgentResultDiffService agentResultDiffService,
        IAgentResultDiffSummaryFormatter agentResultDiffSummaryFormatter,
        IDeterminismCheckService determinismCheckService,
        IArchitectureAnalysisService architectureAnalysisService,
        IArchitectureAnalysisExportService architectureAnalysisExportService,
        IArchitectureAnalysisDocxExportService docxExportService,
        IArchitectureAnalysisConsultingDocxExportService architectureAnalysisConsultingDocxExportService,
        IConsultingDocxTemplateRecommendationService consultingDocxTemplateRecommendationService,
        AppConsultingDocxExportProfileSelector consultingDocxExportProfileSelector,
        IRunExportAuditService runExportAuditService,
        IRunExportRecordRepository runExportRecordRepository)
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
        _resultRepository = resultRepository;
        _agentResultDiffService = agentResultDiffService;
        _agentResultDiffSummaryFormatter = agentResultDiffSummaryFormatter;
        _determinismCheckService = determinismCheckService;
        _architectureAnalysisService = architectureAnalysisService;
        _architectureAnalysisExportService = architectureAnalysisExportService;
        _docxExportService = docxExportService;
        _architectureAnalysisConsultingDocxExportService = architectureAnalysisConsultingDocxExportService;
        _consultingDocxTemplateRecommendationService = consultingDocxTemplateRecommendationService;
        _consultingDocxExportProfileSelector = consultingDocxExportProfileSelector;
        _runExportAuditService = runExportAuditService;
        _runExportRecordRepository = runExportRecordRepository;
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
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);
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
            return this.BadRequestProblem(ex.Message);
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
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
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
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPost("run/{runId}/determinism-check")]
    [ProducesResponseType(typeof(DeterminismCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunDeterminismCheck(
        [FromRoute] string runId,
        [FromBody] DeterminismCheckRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new DeterminismCheckRequest();
        request.RunId = runId;

        try
        {
            var result = await _determinismCheckService.RunAsync(request, cancellationToken);

            return Ok(new DeterminismCheckResponse
            {
                Result = result
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPost("run/{runId}/commit")]
    [ProducesResponseType(typeof(CommitRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "CanCommitRuns")]
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
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
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
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
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
            return this.BadRequestProblem("Agent result is required.", ProblemTypes.AgentResultRequired);
        }

        var result = await _architectureApplicationService.SubmitAgentResultAsync(runId, request.Result, cancellationToken);
        if (!result.Success)
        {
            if (result.Error is not null && result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return this.NotFoundProblem(result.Error, ProblemTypes.RunNotFound);
            }
            return this.BadRequestProblem(result.Error ?? "Submission failed.");
        }

        return Ok(new SubmitAgentResultResponse { ResultId = result.ResultId! });
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
        var result = await _architectureApplicationService.SeedFakeResultsAsync(runId, cancellationToken);
        if (!result.Success)
        {
            if (result.Error is not null && result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return this.NotFoundProblem(result.Error, ProblemTypes.RunNotFound);
            }
            return this.BadRequestProblem(result.Error ?? "Seed failed.");
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
            return this.NotFoundProblem($"Manifest '{leftVersion}' was not found.", ProblemTypes.ManifestNotFound);
        }

        var right = await _manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
        {
            return this.NotFoundProblem($"Manifest '{rightVersion}' was not found.", ProblemTypes.ManifestNotFound);
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
            return this.NotFoundProblem($"Manifest '{leftVersion}' was not found.", ProblemTypes.ManifestNotFound);
        }

        var right = await _manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
        {
            return this.NotFoundProblem($"Manifest '{rightVersion}' was not found.", ProblemTypes.ManifestNotFound);
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
            return this.NotFoundProblem($"Manifest '{leftVersion}' was not found.", ProblemTypes.ManifestNotFound);
        }

        var right = await _manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
        {
            return this.NotFoundProblem($"Manifest '{rightVersion}' was not found.", ProblemTypes.ManifestNotFound);
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
            return this.NotFoundProblem($"Manifest '{leftVersion}' was not found.", ProblemTypes.ManifestNotFound);
        }

        var right = await _manifestRepository.GetByVersionAsync(rightVersion, cancellationToken);
        if (right is null)
        {
            return this.NotFoundProblem($"Manifest '{rightVersion}' was not found.", ProblemTypes.ManifestNotFound);
        }

        var diff = _manifestDiffService.Compare(left, right);
        var summary = _manifestDiffSummaryFormatter.FormatMarkdown(diff);
        var content = _manifestDiffExportService.GenerateMarkdownExport(left, right, diff, summary);

        var fileName = $"compare_{leftVersion}_to_{rightVersion}.md";
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);

        return File(bytes, "text/markdown", fileName);
    }

    [HttpGet("run/compare/agents")]
    [ProducesResponseType(typeof(AgentResultCompareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareAgentResults(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        var leftRun = await _runRepository.GetByIdAsync(leftRunId, cancellationToken);
        if (leftRun is null)
        {
            return this.NotFoundProblem($"Run '{leftRunId}' was not found.", ProblemTypes.RunNotFound);
        }

        var rightRun = await _runRepository.GetByIdAsync(rightRunId, cancellationToken);
        if (rightRun is null)
        {
            return this.NotFoundProblem($"Run '{rightRunId}' was not found.", ProblemTypes.RunNotFound);
        }

        var leftResults = await _resultRepository.GetByRunIdAsync(leftRunId, cancellationToken);
        var rightResults = await _resultRepository.GetByRunIdAsync(rightRunId, cancellationToken);

        var diff = _agentResultDiffService.Compare(leftRunId, leftResults, rightRunId, rightResults);

        return Ok(new AgentResultCompareResponse
        {
            Diff = diff
        });
    }

    [HttpGet("run/compare/agents/summary")]
    [ProducesResponseType(typeof(AgentResultCompareSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareAgentResultsSummary(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        var leftRun = await _runRepository.GetByIdAsync(leftRunId, cancellationToken);
        if (leftRun is null)
        {
            return this.NotFoundProblem($"Run '{leftRunId}' was not found.", ProblemTypes.RunNotFound);
        }

        var rightRun = await _runRepository.GetByIdAsync(rightRunId, cancellationToken);
        if (rightRun is null)
        {
            return this.NotFoundProblem($"Run '{rightRunId}' was not found.", ProblemTypes.RunNotFound);
        }

        var leftResults = await _resultRepository.GetByRunIdAsync(leftRunId, cancellationToken);
        var rightResults = await _resultRepository.GetByRunIdAsync(rightRunId, cancellationToken);

        var diff = _agentResultDiffService.Compare(leftRunId, leftResults, rightRunId, rightResults);
        var summary = _agentResultDiffSummaryFormatter.FormatMarkdown(diff);

        return Ok(new AgentResultCompareSummaryResponse
        {
            Format = "markdown",
            Summary = summary,
            Diff = diff
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
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);
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
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);
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
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);
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
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);
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
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);
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
            return this.NotFoundProblem($"Manifest '{version}' was not found.", ProblemTypes.ManifestNotFound);
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
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        var evidence = await _agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
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
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
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

    [HttpPost("run/{runId}/analysis-report")]
    [ProducesResponseType(typeof(ArchitectureAnalysisReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BuildAnalysisReport(
        [FromRoute] string runId,
        [FromBody] ArchitectureAnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ArchitectureAnalysisRequest();
        request.RunId = runId;

        try
        {
            var report = await _architectureAnalysisService.BuildAsync(request, cancellationToken);

            return Ok(new ArchitectureAnalysisReportResponse
            {
                Report = report
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPost("run/{runId}/analysis-report/export")]
    [ProducesResponseType(typeof(ArchitectureAnalysisExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportAnalysisReport(
        [FromRoute] string runId,
        [FromBody] ArchitectureAnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ArchitectureAnalysisRequest();
        request.RunId = runId;

        try
        {
            var report = await _architectureAnalysisService.BuildAsync(request, cancellationToken);
            var content = _architectureAnalysisExportService.GenerateMarkdown(report);

            return Ok(new ArchitectureAnalysisExportResponse
            {
                RunId = runId,
                Format = "markdown",
                FileName = $"analysis_{runId}.md",
                Content = content
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPost("run/{runId}/analysis-report/export/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAnalysisReport(
        [FromRoute] string runId,
        [FromBody] ArchitectureAnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ArchitectureAnalysisRequest();
        request.RunId = runId;

        try
        {
            var report = await _architectureAnalysisService.BuildAsync(request, cancellationToken);
            var content = _architectureAnalysisExportService.GenerateMarkdown(report);
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);

            return File(bytes, "text/markdown", $"analysis_{runId}.md");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPost("run/{runId}/analysis-report/export/docx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportAnalysisReportDocx(
        [FromRoute] string runId,
        [FromBody] ArchitectureAnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ArchitectureAnalysisRequest();
        request.RunId = runId;

        try
        {
            var report = await _architectureAnalysisService.BuildAsync(request, cancellationToken);
            var bytes = await _docxExportService.GenerateDocxAsync(report, cancellationToken);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"analysis_{runId}.docx");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPost("analysis-report/export/docx/consulting/resolve-profile")]
    [ProducesResponseType(typeof(ConsultingDocxExportResponse), StatusCodes.Status200OK)]
    [Authorize(Policy = "CanExportConsultingDocx")]
    public IActionResult ResolveConsultingDocxExportProfile(
        [FromBody] ConsultingDocxExportRequest? request)
    {
        request ??= new ConsultingDocxExportRequest();

        var resolved = _consultingDocxExportProfileSelector.Resolve(
            request.TemplateProfile,
            new AppConsultingDocxProfileRecommendationRequest
            {
                Audience = request.Audience,
                ExternalDelivery = request.ExternalDelivery,
                ExecutiveFriendly = request.ExecutiveFriendly,
                RegulatedEnvironment = request.RegulatedEnvironment,
                NeedDetailedEvidence = request.NeedDetailedEvidence,
                NeedExecutionTraces = request.NeedExecutionTraces,
                NeedDeterminismOrCompareAppendices = request.NeedDeterminismOrCompareAppendices
            });

        return Ok(new ConsultingDocxExportResponse
        {
            RunId = string.Empty,
            SelectedProfileName = resolved.SelectedProfileName,
            SelectedProfileDisplayName = resolved.SelectedProfileDisplayName,
            WasAutoSelected = resolved.WasAutoSelected,
            ResolutionReason = resolved.ResolutionReason,
            FileName = string.Empty
        });
    }

    [HttpGet("run/{runId}/exports")]
    [ProducesResponseType(typeof(RunExportHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunExportHistory(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        var exports = await _runExportRecordRepository.GetByRunIdAsync(runId, cancellationToken);

        return Ok(new RunExportHistoryResponse
        {
            Exports = exports.ToList()
        });
    }

    [HttpGet("run/exports/{exportRecordId}")]
    [ProducesResponseType(typeof(RunExportRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunExportRecord(
        [FromRoute] string exportRecordId,
        CancellationToken cancellationToken)
    {
        var record = await _runExportRecordRepository.GetByIdAsync(exportRecordId, cancellationToken);
        if (record is null)
        {
            return this.NotFoundProblem($"Export record '{exportRecordId}' was not found.", ProblemTypes.ResourceNotFound);
        }

        return Ok(new RunExportRecordResponse
        {
            Record = record
        });
    }

    [HttpPost("run/{runId}/analysis-report/export/docx/consulting")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "CanExportConsultingDocx")]
    public async Task<IActionResult> ExportConsultingAnalysisReportDocx(
        [FromRoute] string runId,
        [FromBody] ConsultingDocxExportRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ConsultingDocxExportRequest();

        try
        {
            var resolved = _consultingDocxExportProfileSelector.Resolve(
                request.TemplateProfile,
                new AppConsultingDocxProfileRecommendationRequest
                {
                    Audience = request.Audience,
                    ExternalDelivery = request.ExternalDelivery,
                    ExecutiveFriendly = request.ExecutiveFriendly,
                    RegulatedEnvironment = request.RegulatedEnvironment,
                    NeedDetailedEvidence = request.NeedDetailedEvidence,
                    NeedExecutionTraces = request.NeedExecutionTraces,
                    NeedDeterminismOrCompareAppendices = request.NeedDeterminismOrCompareAppendices
                });

            var analysisRequest = new ArchitectureAnalysisRequest
            {
                RunId = runId,
                IncludeEvidence = request.IncludeEvidence,
                IncludeExecutionTraces = request.IncludeExecutionTraces,
                IncludeManifest = request.IncludeManifest,
                IncludeDiagram = request.IncludeDiagram,
                IncludeSummary = request.IncludeSummary,
                IncludeDeterminismCheck = request.IncludeDeterminismCheck,
                DeterminismIterations = request.DeterminismIterations,
                IncludeManifestCompare = request.IncludeManifestCompare,
                CompareManifestVersion = request.CompareManifestVersion,
                IncludeAgentResultCompare = request.IncludeAgentResultCompare,
                CompareRunId = request.CompareRunId
            };

            var report = await _architectureAnalysisService.BuildAsync(
                analysisRequest,
                cancellationToken);

            var bytes = await _architectureAnalysisConsultingDocxExportService.GenerateDocxAsync(
                report,
                cancellationToken);

            var fileName = $"analysis_{resolved.SelectedProfileName}_{runId}.docx";

            await _runExportAuditService.RecordAsync(
                runId: runId,
                exportType: "analysis-report-consulting-docx",
                format: "docx",
                fileName: fileName,
                templateProfile: resolved.SelectedProfileName,
                templateProfileDisplayName: resolved.SelectedProfileDisplayName,
                wasAutoSelected: resolved.WasAutoSelected,
                resolutionReason: resolved.ResolutionReason,
                manifestVersion: report.Manifest?.Metadata.ManifestVersion,
                notes: "Consulting DOCX export generated.",
                cancellationToken: cancellationToken);

            Response.Headers["X-ArchiForge-Selected-Profile"] = resolved.SelectedProfileName;
            Response.Headers["X-ArchiForge-Profile-AutoSelected"] = resolved.WasAutoSelected.ToString();
            Response.Headers["X-ArchiForge-Profile-Reason"] = resolved.ResolutionReason;

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message);
        }
    }

    [HttpPost("analysis-report/export/docx/consulting/profiles/recommend")]
    [ProducesResponseType(typeof(ConsultingDocxProfileRecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RecommendConsultingDocxTemplateProfile(
        [FromBody] ApiConsultingDocxProfileRecommendationRequest? request)
    {
        request ??= new ApiConsultingDocxProfileRecommendationRequest();

        var recommendation = _consultingDocxTemplateRecommendationService.Recommend(
            new AppConsultingDocxProfileRecommendationRequest
            {
                Audience = request.Audience,
                ExternalDelivery = request.ExternalDelivery,
                ExecutiveFriendly = request.ExecutiveFriendly,
                RegulatedEnvironment = request.RegulatedEnvironment,
                NeedDetailedEvidence = request.NeedDetailedEvidence,
                NeedExecutionTraces = request.NeedExecutionTraces,
                NeedDeterminismOrCompareAppendices = request.NeedDeterminismOrCompareAppendices
            });

        return Ok(new ConsultingDocxProfileRecommendationResponse
        {
            Recommendation = recommendation
        });
    }
}
