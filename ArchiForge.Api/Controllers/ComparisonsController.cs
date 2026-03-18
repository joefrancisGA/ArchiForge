using System.IO.Compression;
using ArchiForge.Api;
using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Application.Analysis;
using ArchiForge.Data.Repositories;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;

using ApiReplayComparisonRequest = ArchiForge.Api.Models.ReplayComparisonRequest;
using AppReplayComparisonRequest = ArchiForge.Application.Analysis.ReplayComparisonRequest;

namespace ArchiForge.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public sealed class ComparisonsController : ControllerBase
{
    private readonly IArchitectureRunRepository _runRepository;
    private readonly IRunExportRecordRepository _runExportRecordRepository;
    private readonly IComparisonRecordRepository _comparisonRecordRepository;
    private readonly IComparisonReplayApiService _comparisonReplayApiService;
    private readonly Application.Analysis.IDriftReportFormatter _driftReportFormatter;
    private readonly Application.Analysis.DriftReportDocxExport _driftReportDocxExport;
    private readonly ILogger<ComparisonsController> _logger;

    public ComparisonsController(
        IArchitectureRunRepository runRepository,
        IRunExportRecordRepository runExportRecordRepository,
        IComparisonRecordRepository comparisonRecordRepository,
        IComparisonReplayApiService comparisonReplayApiService,
        Application.Analysis.IDriftReportFormatter driftReportFormatter,
        Application.Analysis.DriftReportDocxExport driftReportDocxExport,
        ILogger<ComparisonsController> logger)
    {
        _runRepository = runRepository;
        _runExportRecordRepository = runExportRecordRepository;
        _comparisonRecordRepository = comparisonRecordRepository;
        _comparisonReplayApiService = comparisonReplayApiService;
        _driftReportFormatter = driftReportFormatter;
        _driftReportDocxExport = driftReportDocxExport;
        _logger = logger;
    }

    private static readonly ComparisonHistoryQueryValidator ComparisonHistoryValidator = new();

    private sealed class ComparisonHistoryQueryValidator : AbstractValidator<ComparisonHistoryQuery>
    {
        public ComparisonHistoryQueryValidator()
        {
            RuleFor(x => x.ComparisonType)
                .Must(t => string.IsNullOrWhiteSpace(t)
                           || string.Equals(t.Trim(), "end-to-end-replay", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(t.Trim(), "export-record-diff", StringComparison.OrdinalIgnoreCase))
                .WithMessage("comparisonType must be empty, 'end-to-end-replay', or 'export-record-diff'.");

            RuleFor(x => x)
                .Must(q => q.CreatedFromUtc is null || q.CreatedToUtc is null || q.CreatedFromUtc <= q.CreatedToUtc)
                .WithMessage("createdFromUtc must be <= createdToUtc.");

            RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).WithMessage("skip must be >= 0.");

            RuleFor(x => x.Limit).InclusiveBetween(0, 500).WithMessage("limit must be between 0 and 500 (0 = default 50).");

            RuleFor(x => x.SortDir)
                .Must(d => string.IsNullOrWhiteSpace(d)
                           || string.Equals(d, "asc", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(d, "desc", StringComparison.OrdinalIgnoreCase))
                .WithMessage("sortDir must be 'asc' or 'desc'.");

            RuleFor(x => x.SortBy)
                .Must(s => string.IsNullOrWhiteSpace(s)
                           || string.Equals(s, "createdUtc", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(s, "type", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(s, "label", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(s, "leftRunId", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(s, "rightRunId", StringComparison.OrdinalIgnoreCase))
                .WithMessage("sortBy must be one of: createdUtc, type, label, leftRunId, rightRunId.");

            RuleFor(x => x)
                .Must(q => string.IsNullOrWhiteSpace(q.Cursor)
                           || string.Equals(q.SortBy ?? "createdUtc", "createdUtc", StringComparison.OrdinalIgnoreCase))
                .WithMessage("cursor paging currently requires sortBy=createdUtc.");
        }
    }

    [HttpGet("run/{runId}/comparisons")]
    [ProducesResponseType(typeof(ComparisonHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunComparisonHistory(
        [FromRoute] string runId,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        }

        var records = await _comparisonRecordRepository.GetByRunIdAsync(runId, cancellationToken);

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
        var export = await _runExportRecordRepository.GetByIdAsync(exportRecordId, cancellationToken);
        if (export is null)
        {
            return this.NotFoundProblem($"Export record '{exportRecordId}' was not found.", ProblemTypes.ResourceNotFound);
        }

        var records = await _comparisonRecordRepository.GetByExportRecordIdAsync(exportRecordId, cancellationToken);

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
        var record = await _comparisonRecordRepository.GetByIdAsync(comparisonRecordId, cancellationToken);
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
        var record = await _comparisonRecordRepository.GetByIdAsync(comparisonRecordId, cancellationToken);
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

        var replay = await _comparisonReplayApiService.ReplayAsync(
            new AppReplayComparisonRequest
            {
                ComparisonRecordId = comparisonRecordId,
                Format = "markdown",
                ReplayMode = "artifact",
                PersistReplay = false
            },
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
        var vr = await ComparisonHistoryValidator.ValidateAsync(query, cancellationToken);
        if (!vr.IsValid)
        {
            return this.BadRequestProblem(
                string.Join(" ", vr.Errors.Select(e => e.ErrorMessage)),
                ProblemTypes.ValidationFailed);
        }

        if (!ApiPaging.TryParseUtcTicksIdCursor(query.Cursor, out var cursorCreatedUtc, out var cursorId, out var cursorError))
            return this.BadRequestProblem(cursorError!, ProblemTypes.ValidationFailed);

        var normalizedType = string.IsNullOrWhiteSpace(query.ComparisonType) ? null : query.ComparisonType.Trim();
        var normalizedTags = ComparisonHistoryQuery.NormalizeTagList(query.Tag, query.Tags);
        var limit = query.Limit <= 0 ? 50 : query.Limit;
        var sortBy = query.SortBy ?? "createdUtc";
        var sortDir = query.SortDir ?? "desc";

        IReadOnlyList<ArchiForge.Contracts.Metadata.ComparisonRecord> records;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            records = await _comparisonRecordRepository.SearchByCursorAsync(
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
            records = await _comparisonRecordRepository.SearchAsync(
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

        var nextCursor = records.Count > 0 && string.Equals(sortBy, "createdUtc", StringComparison.OrdinalIgnoreCase)
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateComparisonRecord(
        [FromRoute] string comparisonRecordId,
        [FromBody] UpdateComparisonRecordRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new UpdateComparisonRecordRequest();
        var exists = await _comparisonRecordRepository.GetByIdAsync(comparisonRecordId, cancellationToken);
        if (exists is null)
            return this.NotFoundProblem($"Comparison record '{comparisonRecordId}' was not found.", ProblemTypes.ResourceNotFound);

        var updated = await _comparisonRecordRepository.UpdateLabelAndTagsAsync(
            comparisonRecordId,
            request.Label,
            request.Tags,
            cancellationToken);
        if (!updated)
            return this.NotFoundProblem($"Comparison record '{comparisonRecordId}' was not found.", ProblemTypes.ResourceNotFound);

        var record = await _comparisonRecordRepository.GetByIdAsync(comparisonRecordId, cancellationToken);
        return Ok(new ComparisonRecordResponse { Record = record! });
    }

    [HttpPost("comparisons/{comparisonRecordId}/replay")]
    [Authorize(Policy = "CanReplayComparisons")]
    [EnableRateLimiting("replay")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayComparison(
        [FromRoute] string comparisonRecordId,
        [FromQuery] string? format,
        [FromBody] ApiReplayComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ApiReplayComparisonRequest();
        if (!string.IsNullOrWhiteSpace(format) && string.IsNullOrWhiteSpace(request.Format))
            request.Format = format;
        var result = await _comparisonReplayApiService.ReplayAsync(
            new AppReplayComparisonRequest
            {
                ComparisonRecordId = comparisonRecordId,
                Format = request.Format,
                ReplayMode = request.ReplayMode,
                Profile = request.Profile,
                PersistReplay = request.PersistReplay
            },
            metadataOnly: false,
            cancellationToken);

            Response.Headers["X-ArchiForge-ComparisonRecordId"] = result.ComparisonRecordId;
            Response.Headers["X-ArchiForge-ComparisonType"] = result.ComparisonType;
            Response.Headers["X-ArchiForge-ReplayMode"] = result.ReplayMode;
            Response.Headers["X-ArchiForge-VerificationPassed"] = result.VerificationPassed.ToString();
            if (result.VerificationMessage is { } msg)
            {
                Response.Headers["X-ArchiForge-VerificationMessage"] = msg;
            }
            if (result.LeftRunId is { } leftRunId)
            {
                Response.Headers["X-ArchiForge-LeftRunId"] = leftRunId;
            }
            if (result.RightRunId is { } rightRunId)
            {
                Response.Headers["X-ArchiForge-RightRunId"] = rightRunId;
            }
            if (result.LeftExportRecordId is { } leftExportId)
            {
                Response.Headers["X-ArchiForge-LeftExportRecordId"] = leftExportId;
            }
            if (result.RightExportRecordId is { } rightExportId)
            {
                Response.Headers["X-ArchiForge-RightExportRecordId"] = rightExportId;
            }
            if (result.CreatedUtc is { } createdUtc)
            {
                Response.Headers["X-ArchiForge-CreatedUtc"] = createdUtc.ToString("O");
            }
            if (result.FormatProfile is { } formatProfile)
            {
                Response.Headers["X-ArchiForge-Format-Profile"] = formatProfile;
            }
            if (result.PersistedReplayRecordId is { } persistedId)
            {
                Response.Headers["X-ArchiForge-PersistedReplayRecordId"] = persistedId;
            }

            return ReplayArtifactResponseFactory.ComparisonReplayFileOrBadRequest(
                Request,
                result,
                () => this.BadRequestProblem(
                    $"Unsupported replay result format '{result.Format}'.",
                    ProblemTypes.BadRequest));
    }

    [HttpPost("comparisons/{comparisonRecordId}/drift")]
    [ProducesResponseType(typeof(DriftAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeComparisonDrift(
        [FromRoute] string comparisonRecordId,
        CancellationToken cancellationToken)
    {
        var drift = await _comparisonReplayApiService.AnalyzeDriftAsync(comparisonRecordId, cancellationToken);
        return Ok(MapDriftAnalysis(drift));
    }

    [HttpGet("comparisons/{comparisonRecordId}/drift-report")]
    [Authorize(Policy = "CanReplayComparisons")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComparisonDriftReport(
        [FromRoute] string comparisonRecordId,
        [FromQuery] string format = "markdown",
        CancellationToken cancellationToken = default)
    {
        var drift = await _comparisonReplayApiService.AnalyzeDriftAsync(comparisonRecordId, cancellationToken);
        var normalizedFormat = (format ?? "markdown").Trim().ToLowerInvariant();

        if (normalizedFormat == "markdown")
        {
            var content = _driftReportFormatter.FormatMarkdown(drift, comparisonRecordId);
            return ApiFileResults.RangeText(Request, content, "text/markdown", $"drift-report_{comparisonRecordId}.md");
        }
        if (normalizedFormat == "html")
        {
            var content = _driftReportFormatter.FormatHtml(drift, comparisonRecordId);
            return ApiFileResults.RangeText(Request, content, "text/html", $"drift-report_{comparisonRecordId}.html");
        }
        if (normalizedFormat == "docx")
        {
            var bytes = _driftReportDocxExport.GenerateDocx(drift, comparisonRecordId);
            return ApiFileResults.RangeBytes(
                Request,
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"drift-report_{comparisonRecordId}.docx");
        }

        return this.BadRequestProblem(
            $"Unsupported drift report format '{format}'. Use markdown, html, or docx.",
            ProblemTypes.BadRequest);
    }

    [HttpPost("comparisons/{comparisonRecordId}/replay/metadata")]
    [Authorize(Policy = "CanReplayComparisons")]
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
        var result = await _comparisonReplayApiService.ReplayAsync(
            new AppReplayComparisonRequest
            {
                ComparisonRecordId = comparisonRecordId,
                Format = request.Format,
                ReplayMode = request.ReplayMode,
                Profile = request.Profile,
                PersistReplay = request.PersistReplay
            },
            metadataOnly: true,
            cancellationToken);

            if (result.LeftRunId is { } leftRunId)
            {
                Response.Headers["X-ArchiForge-LeftRunId"] = leftRunId;
            }
            if (result.RightRunId is { } rightRunId)
            {
                Response.Headers["X-ArchiForge-RightRunId"] = rightRunId;
            }
            if (result.LeftExportRecordId is { } leftExportId)
            {
                Response.Headers["X-ArchiForge-LeftExportRecordId"] = leftExportId;
            }
            if (result.RightExportRecordId is { } rightExportId)
            {
                Response.Headers["X-ArchiForge-RightExportRecordId"] = rightExportId;
            }
            if (result.CreatedUtc is { } createdUtc)
            {
                Response.Headers["X-ArchiForge-CreatedUtc"] = createdUtc.ToString("O");
            }
            if (result.FormatProfile is { } formatProfile)
            {
                Response.Headers["X-ArchiForge-Format-Profile"] = formatProfile;
            }
            if (result.PersistedReplayRecordId is { } persistedIdMeta)
            {
                Response.Headers["X-ArchiForge-PersistedReplayRecordId"] = persistedIdMeta;
            }

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
    [Authorize(Policy = "CanReplayComparisons")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReplayComparisonsBatch(
        [FromBody] BatchReplayComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new BatchReplayComparisonRequest();

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

        var format = request.Format ?? "markdown";
        var mode = request.ReplayMode ?? "artifact";

        await using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var id in request.ComparisonRecordIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var result = await _comparisonReplayApiService.ReplayAsync(
                    new AppReplayComparisonRequest
                    {
                        ComparisonRecordId = id,
                        Format = format,
                        ReplayMode = mode,
                        Profile = request.Profile,
                        PersistReplay = request.PersistReplay
                    },
                    metadataOnly: false,
                    cancellationToken);

                var entryName = result.FileName;
                if (string.IsNullOrWhiteSpace(entryName))
                {
                    entryName = $"comparison_{id}.{result.Format}";
                }

                var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                var payload = ReplayArtifactResponseFactory.GetComparisonReplayEntryBytes(result);
                await entryStream.WriteAsync(payload, cancellationToken);
            }
        }

        ms.Position = 0;
        return ApiFileResults.SimpleBytes(ms.ToArray(), "application/zip", "comparison_replays.zip");
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

