using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Http;
using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ApiReplayExportRequest = ArchiForge.Api.Models.ReplayExportRequest;
using AppReplayExportRequest = ArchiForge.Application.Analysis.ReplayExportRequest;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Query and trigger run exports (history, diff, replay export) and audit comparisons tied to export records.
/// </summary>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
public sealed class ExportsController(
    IRunDetailQueryService runDetailQueryService,
    IRunExportRecordRepository runExportRecordRepository,
    IComparisonAuditService comparisonAuditService,
    IExportReplayService exportReplayService,
    IExportRecordDiffService exportRecordDiffService,
    IExportRecordDiffSummaryFormatter exportRecordDiffSummaryFormatter) : ControllerBase
{
    [HttpGet("run/{runId}/exports")]
    [ProducesResponseType(typeof(RunExportHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunExportHistory(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        if (await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken) is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);

        IReadOnlyList<RunExportRecord> records = await runExportRecordRepository.GetByRunIdAsync(runId, cancellationToken);

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
        RunExportRecord? record = await runExportRecordRepository.GetByIdAsync(exportRecordId, cancellationToken);
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
        LoadedExportRecordPair loaded = await LoadExportRecordPairAsync(leftExportRecordId, rightExportRecordId, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;

        ExportRecordDiffResult diff = exportRecordDiffService.Compare(loaded.Left!, loaded.Right!);

        return Ok(new ExportRecordDiffResponse
        {
            Diff = diff
        });
    }

    [HttpPost("run/exports/compare/summary")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(ExportRecordDiffSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareExportRecordsSummary(
        [FromQuery] string leftExportRecordId,
        [FromQuery] string rightExportRecordId,
        [FromBody] PersistComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        LoadedExportRecordPair loaded = await LoadExportRecordPairAsync(leftExportRecordId, rightExportRecordId, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;

        request ??= new PersistComparisonRequest();

        ExportRecordDiffResult diff = exportRecordDiffService.Compare(loaded.Left!, loaded.Right!);
        string summary = exportRecordDiffSummaryFormatter.FormatMarkdown(diff);

        if (!request.Persist)
            return Ok(new ExportRecordDiffSummaryResponse
            {
                Format = "markdown",
                Summary = summary
            });

        string comparisonRecordId = await comparisonAuditService.RecordExportDiffAsync(
            diff,
            summary,
            cancellationToken);
        Response.Headers[ArchiForgeHttpHeaders.ComparisonRecordId] = comparisonRecordId;

        return Ok(new ExportRecordDiffSummaryResponse
        {
            Format = "markdown",
            Summary = summary
        });
    }

    [HttpPost("run/exports/{exportRecordId}/replay")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayExportRecord(
        [FromRoute] string exportRecordId,
        [FromBody] ApiReplayExportRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ApiReplayExportRequest(); // body is optional; defaults apply when omitted

        ReplayExportResult result = await exportReplayService.ReplayAsync(
            new AppReplayExportRequest
            {
                ExportRecordId = exportRecordId,
                RecordReplayExport = request.RecordReplayExport
            },
            cancellationToken);

        return ReplayArtifactResponseFactory.FromExportReplay(Request, result);
    }

    [HttpPost("run/exports/{exportRecordId}/replay/metadata")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(ReplayExportMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayExportRecordMetadata(
        [FromRoute] string exportRecordId,
        [FromBody] ApiReplayExportRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ApiReplayExportRequest();

        ReplayExportResult result = await exportReplayService.ReplayAsync(
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

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Validates query parameters and loads both export records.
    /// Returns a non-null <see cref="LoadedExportRecordPair.Error"/> on any validation or 404 failure.
    /// </summary>
    private async Task<LoadedExportRecordPair> LoadExportRecordPairAsync(
        string leftExportRecordId,
        string rightExportRecordId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(leftExportRecordId))
            return new LoadedExportRecordPair { Error = this.BadRequestProblem("leftExportRecordId is required.", ProblemTypes.ValidationFailed) };

        if (string.IsNullOrWhiteSpace(rightExportRecordId))
            return new LoadedExportRecordPair { Error = this.BadRequestProblem("rightExportRecordId is required.", ProblemTypes.ValidationFailed) };

        RunExportRecord? left = await runExportRecordRepository.GetByIdAsync(leftExportRecordId, cancellationToken);
        if (left is null)
            return new LoadedExportRecordPair { Error = this.NotFoundProblem($"Export record '{leftExportRecordId}' was not found.", ProblemTypes.ResourceNotFound) };

        RunExportRecord? right = await runExportRecordRepository.GetByIdAsync(rightExportRecordId, cancellationToken);
        
        return right is null ? new LoadedExportRecordPair { Error = this.NotFoundProblem($"Export record '{rightExportRecordId}' was not found.", ProblemTypes.ResourceNotFound) } : new LoadedExportRecordPair { Left = left, Right = right };
    }

    private sealed class LoadedExportRecordPair
    {
        public IActionResult? Error { get; init; }
        public RunExportRecord? Left { get; init; }
        public RunExportRecord? Right { get; init; }
    }
}

