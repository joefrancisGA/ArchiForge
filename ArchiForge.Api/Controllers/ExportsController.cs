using ArchiForge.Api.Models;
using ArchiForge.Api.Services;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Application.Analysis;
using ArchiForge.Data.Repositories;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ApiReplayExportRequest = ArchiForge.Api.Models.ReplayExportRequest;
using AppReplayExportRequest = ArchiForge.Application.Analysis.ReplayExportRequest;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ApiKey")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
public sealed class ExportsController : ControllerBase
{
    private readonly IArchitectureRunRepository _runRepository;
    private readonly IRunExportRecordRepository _runExportRecordRepository;
    private readonly IComparisonAuditService _comparisonAuditService;
    private readonly IExportReplayService _exportReplayService;
    private readonly IExportRecordDiffService _exportRecordDiffService;
    private readonly IExportRecordDiffSummaryFormatter _exportRecordDiffSummaryFormatter;
    private readonly ILogger<ExportsController> _logger;

    public ExportsController(
        IArchitectureRunRepository runRepository,
        IRunExportRecordRepository runExportRecordRepository,
        IComparisonAuditService comparisonAuditService,
        IExportReplayService exportReplayService,
        IExportRecordDiffService exportRecordDiffService,
        IExportRecordDiffSummaryFormatter exportRecordDiffSummaryFormatter,
        ILogger<ExportsController> logger)
    {
        _runRepository = runRepository;
        _runExportRecordRepository = runExportRecordRepository;
        _comparisonAuditService = comparisonAuditService;
        _exportReplayService = exportReplayService;
        _exportRecordDiffService = exportRecordDiffService;
        _exportRecordDiffSummaryFormatter = exportRecordDiffSummaryFormatter;
        _logger = logger;
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
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);

        var records = await _runExportRecordRepository.GetByRunIdAsync(runId, cancellationToken);

        return Ok(new RunExportHistoryResponse
        {
            Exports = records.ToList()
        });
    }

    [HttpGet("run/exports/{exportRecordId}")]
    [ProducesResponseType(typeof(RunExportRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExportRecord(
        [FromRoute] string exportRecordId,
        CancellationToken cancellationToken)
    {
        var record = await _runExportRecordRepository.GetByIdAsync(exportRecordId, cancellationToken);
        if (record is null)
            return this.NotFoundProblem($"Export record '{exportRecordId}' was not found.", ProblemTypes.ResourceNotFound);

        return Ok(new RunExportRecordResponse
        {
            Record = record
        });
    }

    [HttpGet("run/exports/compare")]
    [ProducesResponseType(typeof(ExportRecordDiffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareExportRecords(
        [FromQuery] string leftExportRecordId,
        [FromQuery] string rightExportRecordId,
        CancellationToken cancellationToken)
    {
        var left = await _runExportRecordRepository.GetByIdAsync(leftExportRecordId, cancellationToken);
        if (left is null)
            return this.NotFoundProblem($"Export record '{leftExportRecordId}' was not found.", ProblemTypes.ResourceNotFound);

        var right = await _runExportRecordRepository.GetByIdAsync(rightExportRecordId, cancellationToken);
        if (right is null)
            return this.NotFoundProblem($"Export record '{rightExportRecordId}' was not found.", ProblemTypes.ResourceNotFound);

        var diff = _exportRecordDiffService.Compare(left, right);

        return Ok(new ExportRecordDiffResponse
        {
            Diff = diff
        });
    }

    [HttpPost("run/exports/compare/summary")]
    [ProducesResponseType(typeof(ExportRecordDiffSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareExportRecordsSummary(
        [FromQuery] string leftExportRecordId,
        [FromQuery] string rightExportRecordId,
        [FromBody] PersistComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new PersistComparisonRequest();

        var left = await _runExportRecordRepository.GetByIdAsync(leftExportRecordId, cancellationToken);
        if (left is null)
            return this.NotFoundProblem($"Export record '{leftExportRecordId}' was not found.", ProblemTypes.ResourceNotFound);

        var right = await _runExportRecordRepository.GetByIdAsync(rightExportRecordId, cancellationToken);
        if (right is null)
            return this.NotFoundProblem($"Export record '{rightExportRecordId}' was not found.", ProblemTypes.ResourceNotFound);

        var diff = _exportRecordDiffService.Compare(left, right);
        var summary = _exportRecordDiffSummaryFormatter.FormatMarkdown(diff);

        if (request.Persist)
        {
            var comparisonRecordId = await _comparisonAuditService.RecordExportDiffAsync(
                diff,
                summary,
                cancellationToken);
            Response.Headers["X-ArchiForge-ComparisonRecordId"] = comparisonRecordId;
        }

        return Ok(new ExportRecordDiffSummaryResponse
        {
            Format = "markdown",
            Summary = summary
        });
    }

    [HttpPost("run/exports/{exportRecordId}/replay")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayExportRecord(
        [FromRoute] string exportRecordId,
        [FromBody] ApiReplayExportRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ApiReplayExportRequest();

        var result = await _exportReplayService.ReplayAsync(
            new AppReplayExportRequest
            {
                ExportRecordId = exportRecordId,
                RecordReplayExport = request.RecordReplayExport
            },
            cancellationToken);

        return ReplayArtifactResponseFactory.FromExportReplay(Request, result);
    }

    [HttpPost("run/exports/{exportRecordId}/replay/metadata")]
    [ProducesResponseType(typeof(ReplayExportMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayExportRecordMetadata(
        [FromRoute] string exportRecordId,
        [FromBody] ApiReplayExportRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ApiReplayExportRequest();

        var result = await _exportReplayService.ReplayAsync(
            new AppReplayExportRequest
            {
                ExportRecordId = exportRecordId,
                RecordReplayExport = request.RecordReplayExport
            },
            cancellationToken);

        return Ok(new ReplayExportMetadataResponse
        {
            ExportRecordId = result.ExportRecordId,
            Format = result.Format,
            FileName = result.FileName
        });
    }
}

