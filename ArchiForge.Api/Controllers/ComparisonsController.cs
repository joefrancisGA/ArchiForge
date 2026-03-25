using System.IO.Compression;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Mapping;
using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

using Asp.Versioning;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ApiReplayComparisonRequest = ArchiForge.Api.Models.ReplayComparisonRequest;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// HTTP API for managing architectural run comparison records, drift analysis, and comparison replay.
/// </summary>
/// <remarks>
/// Routes are prefixed <c>v{version}/architecture</c>. Read actions (comparison history, drift reports,
/// export downloads) require <see cref="ArchiForgePolicies.ReadAuthority"/>. Replay and mutation actions
/// additionally require <see cref="ArchiForgePolicies.CanReplayComparisons"/>.
/// Run existence is validated through <see cref="IRunDetailQueryService"/> before acting on comparison records.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
public sealed class ComparisonsController(
    IRunDetailQueryService runDetailQueryService,
    IRunExportRecordRepository runExportRecordRepository,
    IComparisonRecordRepository comparisonRecordRepository,
    IComparisonReplayApiService comparisonReplayApiService,
    IDriftReportFormatter driftReportFormatter,
    DriftReportDocxExport driftReportDocxExport,
    IValidator<ComparisonHistoryQuery> comparisonHistoryQueryValidator)
    : ControllerBase
{

    [HttpGet("run/{runId}/comparisons")]
    [ProducesResponseType(typeof(ComparisonHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunComparisonHistory(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        ArchitectureRunDetail? runDetail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        if (runDetail is null)
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        IReadOnlyList<ComparisonRecord> records = await comparisonRecordRepository.GetByRunIdAsync(runId, cancellationToken);

        return Ok(new ComparisonHistoryResponse
        {
            Records = records.ToList()
        });
    }

    [HttpGet("run/exports/{exportRecordId}/comparisons")]
    [ProducesResponseType(typeof(ComparisonHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExportRecordComparisonHistory(
        [FromRoute] string exportRecordId,
        CancellationToken cancellationToken)
    {
        RunExportRecord? export = await runExportRecordRepository.GetByIdAsync(exportRecordId, cancellationToken);
        if (export is null)
        {
            return this.NotFoundProblem($"Export record '{exportRecordId}' was not found.", ProblemTypes.ResourceNotFound);
        }

        IReadOnlyList<ComparisonRecord> records = await comparisonRecordRepository.GetByExportRecordIdAsync(exportRecordId, cancellationToken);

        return Ok(new ComparisonHistoryResponse
        {
            Records = records.ToList()
        });
    }

    [HttpGet("comparisons/{comparisonRecordId}")]
    [ProducesResponseType(typeof(ComparisonRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComparisonRecord(
        [FromRoute] string comparisonRecordId,
        CancellationToken cancellationToken)
    {
        ComparisonRecord? record = await comparisonRecordRepository.GetByIdAsync(comparisonRecordId, cancellationToken);
        if (record is null)
        {
            return this.NotFoundProblem($"Comparison record '{comparisonRecordId}' was not found.", ProblemTypes.ResourceNotFound);
        }

        return Ok(new ComparisonRecordResponse
        {
            Record = record
        });
    }

    [HttpGet("comparisons/{comparisonRecordId}/summary")]
    [ProducesResponseType(typeof(ComparisonSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComparisonSummary(
        [FromRoute] string comparisonRecordId,
        CancellationToken cancellationToken)
    {
        ComparisonRecord? record = await comparisonRecordRepository.GetByIdAsync(comparisonRecordId, cancellationToken);
        if (record is null)
        {
            return this.NotFoundProblem($"Comparison record '{comparisonRecordId}' was not found.", ProblemTypes.ResourceNotFound);
        }

        if (!string.IsNullOrWhiteSpace(record.SummaryMarkdown))
        {
            return Ok(new ComparisonSummaryResponse
            {
                ComparisonRecordId = record.ComparisonRecordId,
                ComparisonType = record.ComparisonType,
                Format = "markdown",
                Summary = record.SummaryMarkdown
            });
        }

        ReplayComparisonResult replay = await comparisonReplayApiService.ReplayAsync(
            ReplayComparisonRequestMapper.ForSummaryMarkdown(comparisonRecordId),
            metadataOnly: false,
            cancellationToken);

        return Ok(new ComparisonSummaryResponse
        {
            ComparisonRecordId = replay.ComparisonRecordId,
            ComparisonType = replay.ComparisonType,
            Format = "markdown",
            Summary = replay.Content ?? string.Empty
        });
    }

    [HttpGet("comparisons")]
    [ProducesResponseType(typeof(ComparisonHistoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchComparisonRecords(
        [FromQuery] ComparisonHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidationResult? vr = await comparisonHistoryQueryValidator.ValidateAsync(query, cancellationToken);
        if (!vr.IsValid)
        {
            return this.BadRequestProblem(
                string.Join(" ", vr.Errors.Select(e => e.ErrorMessage)),
                ProblemTypes.ValidationFailed);
        }

        if (!ApiPaging.TryParseUtcTicksIdCursor(query.Cursor, out DateTime? cursorCreatedUtc, out string? cursorId, out string? cursorError))
            return this.BadRequestProblem(cursorError!, ProblemTypes.ValidationFailed);

        string? normalizedType = string.IsNullOrWhiteSpace(query.ComparisonType) ? null : query.ComparisonType.Trim();
        List<string> normalizedTags = ComparisonHistoryQuery.NormalizeTagList(query.Tag, query.Tags);
        int limit = query.Limit <= 0 ? 50 : query.Limit;
        string sortBy = query.SortBy ?? "createdUtc";
        string sortDir = query.SortDir ?? "desc";

        IReadOnlyList<ComparisonRecord> records;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            records = await comparisonRecordRepository.SearchByCursorAsync(
                normalizedType,
                query.LeftRunId,
                query.RightRunId,
                query.CreatedFromUtc,
                query.CreatedToUtc,
                query.LeftExportRecordId,
                query.RightExportRecordId,
                query.Label,
                normalizedTags,
                sortBy,
                sortDir,
                cursorCreatedUtc,
                cursorId,
                limit,
                cancellationToken);
        }
        else
        {
            records = await comparisonRecordRepository.SearchAsync(
                normalizedType,
                query.LeftRunId,
                query.RightRunId,
                query.CreatedFromUtc,
                query.CreatedToUtc,
                query.LeftExportRecordId,
                query.RightExportRecordId,
                query.Label,
                normalizedTags,
                sortBy,
                sortDir,
                query.Skip,
                limit,
                cancellationToken);
        }

        string? nextCursor = records.Count > 0 && string.Equals(sortBy, "createdUtc", StringComparison.OrdinalIgnoreCase)
            ? $"{records.Last().CreatedUtc.Ticks}:{records.Last().ComparisonRecordId}"
            : null;

        return Ok(new ComparisonHistoryResponse
        {
            Records = records.ToList(),
            Limit = limit,
            Skip = query.Skip,
            ComparisonType = query.ComparisonType,
            LeftRunId = query.LeftRunId,
            RightRunId = query.RightRunId,
            LeftExportRecordId = query.LeftExportRecordId,
            RightExportRecordId = query.RightExportRecordId,
            Label = query.Label,
            CreatedFromUtc = query.CreatedFromUtc,
            CreatedToUtc = query.CreatedToUtc,
            Tag = query.Tag,
            Tags = normalizedTags,
            SortBy = sortBy,
            SortDir = sortDir,
            NextCursor = nextCursor
        });
    }

    [HttpPatch("comparisons/{comparisonRecordId}")]
    [ProducesResponseType(typeof(ComparisonRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateComparisonRecord(
        [FromRoute] string comparisonRecordId,
        [FromBody] UpdateComparisonRecordRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        bool updated = await comparisonRecordRepository.UpdateLabelAndTagsAsync(
            comparisonRecordId,
            request.Label,
            request.Tags,
            cancellationToken);
        if (!updated)
            return this.NotFoundProblem($"Comparison record '{comparisonRecordId}' was not found.", ProblemTypes.ResourceNotFound);

        ComparisonRecord? record = await comparisonRecordRepository.GetByIdAsync(comparisonRecordId, cancellationToken);
        if (record is null)
            return this.NotFoundProblem($"Comparison record '{comparisonRecordId}' was not found after update.", ProblemTypes.ResourceNotFound);

        return Ok(new ComparisonRecordResponse { Record = record });
    }

    [HttpPost("comparisons/{comparisonRecordId}/replay")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [Authorize(Policy = ArchiForgePolicies.CanReplayComparisons)]
    [EnableRateLimiting("replay")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReplayComparison(
        [FromRoute] string comparisonRecordId,
        [FromQuery] string? format,
        [FromBody] ApiReplayComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);
        ReplayComparisonResult result = await comparisonReplayApiService.ReplayAsync(
            ReplayComparisonRequestMapper.ToApplicationForReplayEndpoint(comparisonRecordId, request, format),
            metadataOnly: false,
            cancellationToken);

        ReplayComparisonResultHeaders.ApplyFull(Response, result);

        return ReplayArtifactResponseFactory.ComparisonReplayFileOrBadRequest(
            Request,
            result,
            () => this.BadRequestProblem(
                $"Unsupported replay result format '{result.Format}'.",
                ProblemTypes.BadRequest));
    }

    [HttpPost("comparisons/{comparisonRecordId}/drift")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(DriftAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeComparisonDrift(
        [FromRoute] string comparisonRecordId,
        CancellationToken cancellationToken)
    {
        DriftAnalysisResult drift = await comparisonReplayApiService.AnalyzeDriftAsync(comparisonRecordId, cancellationToken);
        return Ok(MapDriftAnalysis(drift));
    }

    [HttpGet("comparisons/{comparisonRecordId}/drift-report")]
    [Authorize(Policy = ArchiForgePolicies.CanReplayComparisons)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComparisonDriftReport(
        [FromRoute] string comparisonRecordId,
        [FromQuery] string format = "markdown",
        CancellationToken cancellationToken = default)
    {
        DriftAnalysisResult drift = await comparisonReplayApiService.AnalyzeDriftAsync(comparisonRecordId, cancellationToken);
        string normalizedFormat = format.Trim().ToLowerInvariant();

        switch (normalizedFormat)
        {
            case "markdown":
                {
                    string content = driftReportFormatter.FormatMarkdown(drift, comparisonRecordId);
                    return ApiFileResults.RangeText(Request, content, "text/markdown", $"drift-report_{comparisonRecordId}.md");
                }
            case "html":
                {
                    string content = driftReportFormatter.FormatHtml(drift, comparisonRecordId);
                    return ApiFileResults.RangeText(Request, content, "text/html", $"drift-report_{comparisonRecordId}.html");
                }
            case "docx":
                {
                    byte[] bytes = driftReportDocxExport.GenerateDocx(drift, comparisonRecordId);
                    return ApiFileResults.RangeBytes(
                        Request,
                        bytes,
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        $"drift-report_{comparisonRecordId}.docx");
                }
            default:
                return this.BadRequestProblem(
                    $"Unsupported drift report format '{format}'. Use markdown, html, or docx.",
                    ProblemTypes.BadRequest);
        }
    }

    [HttpPost("comparisons/{comparisonRecordId}/replay/metadata")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [Authorize(Policy = ArchiForgePolicies.CanReplayComparisons)]
    [EnableRateLimiting("replay")]
    [ProducesResponseType(typeof(ReplayComparisonMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayComparisonMetadata(
        [FromRoute] string comparisonRecordId,
        [FromBody] ApiReplayComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ApiReplayComparisonRequest();
        ReplayComparisonResult result = await comparisonReplayApiService.ReplayAsync(
            ReplayComparisonRequestMapper.ToApplication(comparisonRecordId, request),
            metadataOnly: true,
            cancellationToken);

        ReplayComparisonResultHeaders.ApplyMetadata(Response, result);

        return Ok(new ReplayComparisonMetadataResponse
        {
            ComparisonRecordId = result.ComparisonRecordId,
            ComparisonType = result.ComparisonType,
            Format = result.Format,
            FileName = result.FileName,
            ReplayMode = result.ReplayMode,
            VerificationPassed = result.VerificationPassed,
            VerificationMessage = result.VerificationMessage,
            DriftAnalysis = result.DriftAnalysis is null ? null : MapDriftAnalysis(result.DriftAnalysis),
            LeftRunId = result.LeftRunId,
            RightRunId = result.RightRunId,
            LeftExportRecordId = result.LeftExportRecordId,
            RightExportRecordId = result.RightExportRecordId,
            CreatedUtc = result.CreatedUtc,
            FormatProfile = result.FormatProfile,
            PersistedReplayRecordId = result.PersistedReplayRecordId
        });
    }

    [HttpPost("comparisons/replay/batch")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [Authorize(Policy = ArchiForgePolicies.CanReplayComparisons)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayComparisonsBatch(
        [FromBody] BatchReplayComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        if (request.ComparisonRecordIds.Count == 0)
        {
            return this.BadRequestProblem(
                "comparisonRecordIds is required.",
                ProblemTypes.ValidationFailed);
        }

        if (request.ComparisonRecordIds.Count > 50)
        {
            return this.BadRequestProblem(
                "comparisonRecordIds max is 50.",
                ProblemTypes.ValidationFailed);
        }

        List<string> blankIds = request.ComparisonRecordIds
            .Where(string.IsNullOrWhiteSpace)
            .ToList();
        if (blankIds.Count > 0)
        {
            return this.BadRequestProblem(
                "comparisonRecordIds must not contain blank or whitespace-only entries.",
                ProblemTypes.ValidationFailed);
        }

        string format = request.Format;
        string mode = request.ReplayMode;

        MemoryStream ms = new MemoryStream();

        await using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (string id in request.ComparisonRecordIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ReplayComparisonResult result = await comparisonReplayApiService.ReplayAsync(
                    ReplayComparisonRequestMapper.ToApplicationForBatchEntry(
                        id,
                        format,
                        mode,
                        request.Profile,
                        request.PersistReplay),
                    metadataOnly: false,
                    cancellationToken);

                string entryName = result.FileName;
                if (string.IsNullOrWhiteSpace(entryName))
                {
                    entryName = $"comparison_{id}.{result.Format}";
                }

                ZipArchiveEntry entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                await using Stream entryStream = await entry.OpenAsync(cancellationToken);
                byte[] payload = ReplayArtifactResponseFactory.GetComparisonReplayEntryBytes(result);
                await entryStream.WriteAsync(payload, cancellationToken);
            }
        }

        ms.Position = 0;
        return File(ms, "application/zip", "comparison_replays.zip");
    }

    private static DriftAnalysisResponse MapDriftAnalysis(DriftAnalysisResult drift)
    {
        return new DriftAnalysisResponse
        {
            DriftDetected = drift.DriftDetected,
            Summary = drift.Summary,
            Items = drift.Items.Select(i => new DriftItemResponse
            {
                Category = i.Category,
                Path = i.Path,
                StoredValue = i.StoredValue,
                RegeneratedValue = i.RegeneratedValue,
                Description = i.Description
            }).ToList()
        };
    }
}

