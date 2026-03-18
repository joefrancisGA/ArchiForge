using System.Diagnostics;
using System.IO.Compression;
using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Analysis;
using ArchiForge.Data.Repositories;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    private readonly IAgentResultRepository _resultRepository;
    private readonly IAgentResultDiffService _agentResultDiffService;
    private readonly IAgentResultDiffSummaryFormatter _agentResultDiffSummaryFormatter;
    private readonly IEndToEndReplayComparisonService _endToEndReplayComparisonService;
    private readonly IEndToEndReplayComparisonSummaryFormatter _endToEndReplayComparisonSummaryFormatter;
    private readonly IEndToEndReplayComparisonExportService _endToEndReplayComparisonExportService;
    private readonly IComparisonAuditService _comparisonAuditService;
    private readonly IComparisonRecordRepository _comparisonRecordRepository;
    private readonly IComparisonReplayService _comparisonReplayService;
    private readonly IReplayDiagnosticsRecorder _replayDiagnosticsRecorder;
    private readonly Application.Analysis.IDriftReportFormatter _driftReportFormatter;
    private readonly Application.Analysis.DriftReportDocxExport _driftReportDocxExport;
    private readonly ILogger<ComparisonsController> _logger;

    public ComparisonsController(
        IArchitectureRunRepository runRepository,
        IRunExportRecordRepository runExportRecordRepository,
        IAgentResultRepository resultRepository,
        IAgentResultDiffService agentResultDiffService,
        IAgentResultDiffSummaryFormatter agentResultDiffSummaryFormatter,
        IEndToEndReplayComparisonService endToEndReplayComparisonService,
        IEndToEndReplayComparisonSummaryFormatter endToEndReplayComparisonSummaryFormatter,
        IEndToEndReplayComparisonExportService endToEndReplayComparisonExportService,
        IComparisonAuditService comparisonAuditService,
        IComparisonRecordRepository comparisonRecordRepository,
        IComparisonReplayService comparisonReplayService,
        IReplayDiagnosticsRecorder replayDiagnosticsRecorder,
        Application.Analysis.IDriftReportFormatter driftReportFormatter,
        Application.Analysis.DriftReportDocxExport driftReportDocxExport,
        ILogger<ComparisonsController> logger)
    {
        _runRepository = runRepository;
        _runExportRecordRepository = runExportRecordRepository;
        _resultRepository = resultRepository;
        _agentResultDiffService = agentResultDiffService;
        _agentResultDiffSummaryFormatter = agentResultDiffSummaryFormatter;
        _endToEndReplayComparisonService = endToEndReplayComparisonService;
        _endToEndReplayComparisonSummaryFormatter = endToEndReplayComparisonSummaryFormatter;
        _endToEndReplayComparisonExportService = endToEndReplayComparisonExportService;
        _comparisonAuditService = comparisonAuditService;
        _comparisonRecordRepository = comparisonRecordRepository;
        _comparisonReplayService = comparisonReplayService;
        _replayDiagnosticsRecorder = replayDiagnosticsRecorder;
        _driftReportFormatter = driftReportFormatter;
        _driftReportDocxExport = driftReportDocxExport;
        _logger = logger;
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

        return Ok(new AgentResultCompareResponse { Diff = diff });
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

    [HttpGet("run/compare/end-to-end")]
    [ProducesResponseType(typeof(EndToEndReplayComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareRunsEndToEnd(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await _endToEndReplayComparisonService.BuildAsync(
                leftRunId,
                rightRunId,
                cancellationToken);

            return Ok(new EndToEndReplayComparisonResponse { Report = report });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    [HttpPost("run/compare/end-to-end/summary")]
    [ProducesResponseType(typeof(EndToEndReplayComparisonSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareRunsEndToEndSummary(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        [FromBody] PersistComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new PersistComparisonRequest();

        try
        {
            var report = await _endToEndReplayComparisonService.BuildAsync(
                leftRunId,
                rightRunId,
                cancellationToken);

            var summary = _endToEndReplayComparisonSummaryFormatter.FormatMarkdown(report);

            if (request.Persist)
            {
                var comparisonRecordId = await _comparisonAuditService.RecordEndToEndAsync(
                    report,
                    summary,
                    cancellationToken);
                Response.Headers["X-ArchiForge-ComparisonRecordId"] = comparisonRecordId;
            }

            return Ok(new EndToEndReplayComparisonSummaryResponse
            {
                Format = "markdown",
                Summary = summary
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    [HttpGet("run/compare/end-to-end/export")]
    [ProducesResponseType(typeof(EndToEndReplayComparisonExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRunsEndToEndComparisonMarkdown(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await _endToEndReplayComparisonService.BuildAsync(
                leftRunId,
                rightRunId,
                cancellationToken);

            var markdown = _endToEndReplayComparisonExportService.GenerateMarkdown(report);
            var fileName = $"end_to_end_compare_{leftRunId}_to_{rightRunId}.md";

            return Ok(new EndToEndReplayComparisonExportResponse
            {
                Format = "markdown",
                FileName = fileName,
                Content = markdown
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    [HttpGet("run/compare/end-to-end/export/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadRunsEndToEndComparisonMarkdown(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await _endToEndReplayComparisonService.BuildAsync(
                leftRunId,
                rightRunId,
                cancellationToken);

            var markdown = _endToEndReplayComparisonExportService.GenerateMarkdown(report);
            var bytes = System.Text.Encoding.UTF8.GetBytes(markdown);
            var fileName = $"end_to_end_compare_{leftRunId}_to_{rightRunId}.md";

            return File(bytes, "text/markdown", fileName);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
    }

    [HttpGet("run/compare/end-to-end/export/docx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRunsEndToEndComparisonDocx(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await _endToEndReplayComparisonService.BuildAsync(
                leftRunId,
                rightRunId,
                cancellationToken);

            var bytes = await _endToEndReplayComparisonExportService.GenerateDocxAsync(
                report,
                cancellationToken);

            var fileName = $"end_to_end_compare_{leftRunId}_to_{rightRunId}.docx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
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

        var replay = await _comparisonReplayService.ReplayAsync(
            new AppReplayComparisonRequest
            {
                ComparisonRecordId = comparisonRecordId,
                Format = "markdown",
                ReplayMode = "artifact",
                PersistReplay = false
            },
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
        [FromQuery] string? comparisonType,
        [FromQuery] string? leftRunId,
        [FromQuery] string? rightRunId,
        [FromQuery] string? leftExportRecordId,
        [FromQuery] string? rightExportRecordId,
        [FromQuery] string? label,
        [FromQuery] DateTime? createdFromUtc,
        [FromQuery] DateTime? createdToUtc,
        [FromQuery] string? tag,
        [FromQuery] string[]? tags,
        [FromQuery] string? sortBy = "createdUtc",
        [FromQuery] string? sortDir = "desc",
        [FromQuery] string? cursor = null,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = string.IsNullOrWhiteSpace(comparisonType) ? null : comparisonType.Trim();
        if (normalizedType is not null
            && !string.Equals(normalizedType, "end-to-end-replay", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedType, "export-record-diff", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = $"Unsupported comparisonType '{comparisonType}'. Supported: end-to-end-replay, export-record-diff." });
        }

        if (createdFromUtc is not null && createdToUtc is not null && createdFromUtc > createdToUtc)
        {
            return BadRequest(new { error = "createdFromUtc must be <= createdToUtc." });
        }

        if (skip < 0)
        {
            return BadRequest(new { error = "skip must be >= 0." });
        }

        if (sortDir is not null
            && !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "sortDir must be 'asc' or 'desc'." });
        }

        if (sortBy is not null
            && !string.Equals(sortBy, "createdUtc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sortBy, "type", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sortBy, "label", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sortBy, "leftRunId", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sortBy, "rightRunId", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "sortBy must be one of: createdUtc, type, label, leftRunId, rightRunId." });
        }

        var normalizedTags = (tags ?? [])
            .SelectMany(t => (t ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!string.IsNullOrWhiteSpace(tag))
        {
            normalizedTags.AddRange(tag.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            normalizedTags = normalizedTags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        DateTime? cursorCreatedUtc = null;
        string? cursorId = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            var parts = cursor.Split(':', 2);
            if (parts.Length != 2 || !long.TryParse(parts[0], out var ticks) || string.IsNullOrWhiteSpace(parts[1]))
            {
                return BadRequest(new { error = "cursor must be formatted as '<utcTicks>:<comparisonRecordId>'." });
            }
            cursorCreatedUtc = new DateTime(ticks, DateTimeKind.Utc);
            cursorId = parts[1];
        }

        IReadOnlyList<ArchiForge.Contracts.Metadata.ComparisonRecord> records;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!string.Equals(sortBy, "createdUtc", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "cursor paging currently requires sortBy=createdUtc." });
            }

            records = await _comparisonRecordRepository.SearchByCursorAsync(
                normalizedType,
                leftRunId,
                rightRunId,
                createdFromUtc,
                createdToUtc,
                leftExportRecordId,
                rightExportRecordId,
                label,
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
                leftRunId,
                rightRunId,
                createdFromUtc,
                createdToUtc,
                leftExportRecordId,
                rightExportRecordId,
                label,
                normalizedTags,
                sortBy,
                sortDir,
                skip,
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
            Skip = skip,
            ComparisonType = comparisonType,
            LeftRunId = leftRunId,
            RightRunId = rightRunId,
            LeftExportRecordId = leftExportRecordId,
            RightExportRecordId = rightExportRecordId,
            Label = label,
            CreatedFromUtc = createdFromUtc,
            CreatedToUtc = createdToUtc,
            Tag = tag,
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
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await _comparisonReplayService.ReplayAsync(
                new AppReplayComparisonRequest
                {
                    ComparisonRecordId = comparisonRecordId,
                    Format = request.Format,
                    ReplayMode = request.ReplayMode,
                    Profile = request.Profile,
                    PersistReplay = request.PersistReplay
                },
                cancellationToken);

            sw.Stop();
            _replayDiagnosticsRecorder.Record(new ReplayDiagnosticsEntry
            {
                TimestampUtc = DateTime.UtcNow,
                ComparisonRecordId = comparisonRecordId,
                ComparisonType = result.ComparisonType,
                Format = result.Format,
                ReplayMode = result.ReplayMode,
                PersistReplay = request.PersistReplay,
                DurationMs = sw.ElapsedMilliseconds,
                Success = true,
                VerificationPassed = result.VerificationPassed,
                PersistedReplayRecordId = result.PersistedReplayRecordId,
                MetadataOnly = false
            });
            _logger.LogInformation(
                "Comparison replay completed: ComparisonRecordId={ComparisonRecordId}, ComparisonType={ComparisonType}, Format={Format}, ReplayMode={ReplayMode}, PersistReplay={PersistReplay}, DurationMs={DurationMs}, VerificationPassed={VerificationPassed}",
                comparisonRecordId, result.ComparisonType, result.Format, result.ReplayMode, request.PersistReplay, sw.ElapsedMilliseconds, result.VerificationPassed);

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

            if (string.Equals(result.Format, "markdown", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(result.Content ?? string.Empty);
                return new FileWithRangeResult(Request, bytes, "text/markdown", result.FileName);
            }

            if (string.Equals(result.Format, "html", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(result.Content ?? string.Empty);
                return new FileWithRangeResult(Request, bytes, "text/html", result.FileName);
            }

            if (string.Equals(result.Format, "docx", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = result.BinaryContent ?? Array.Empty<byte>();
                return new FileWithRangeResult(
                    Request,
                    bytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    result.FileName);
            }

            if (string.Equals(result.Format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = result.BinaryContent ?? Array.Empty<byte>();
                return new FileWithRangeResult(Request, bytes, "application/pdf", result.FileName);
            }

            return this.BadRequestProblem(
                $"Unsupported replay result format '{result.Format}'.",
                ProblemTypes.BadRequest);
        }
        catch (InvalidOperationException ex)
        {
            sw.Stop();
            var notFound = ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase);
            RecordReplayFailure(comparisonRecordId, request, sw.ElapsedMilliseconds, ex.Message, metadataOnly: false);
            _logger.LogWarning(
                ex,
                "Comparison replay failed: ComparisonRecordId={ComparisonRecordId}, NotFound={NotFound}, Error={Error}",
                comparisonRecordId,
                notFound,
                ex.Message);
            throw;
        }
    }

    [HttpPost("comparisons/{comparisonRecordId}/drift")]
    [ProducesResponseType(typeof(DriftAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeComparisonDrift(
        [FromRoute] string comparisonRecordId,
        CancellationToken cancellationToken)
    {
        var drift = await _comparisonReplayService.AnalyzeDriftAsync(comparisonRecordId, cancellationToken);
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
        var drift = await _comparisonReplayService.AnalyzeDriftAsync(comparisonRecordId, cancellationToken);
        var normalizedFormat = (format ?? "markdown").Trim().ToLowerInvariant();

        if (normalizedFormat == "markdown")
        {
            var content = _driftReportFormatter.FormatMarkdown(drift, comparisonRecordId);
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return File(bytes, "text/markdown", $"drift-report_{comparisonRecordId}.md");
        }
        if (normalizedFormat == "html")
        {
            var content = _driftReportFormatter.FormatHtml(drift, comparisonRecordId);
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return File(bytes, "text/html", $"drift-report_{comparisonRecordId}.html");
        }
        if (normalizedFormat == "docx")
        {
            var bytes = _driftReportDocxExport.GenerateDocx(drift, comparisonRecordId);
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"drift-report_{comparisonRecordId}.docx");
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
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await _comparisonReplayService.ReplayAsync(
                new AppReplayComparisonRequest
                {
                    ComparisonRecordId = comparisonRecordId,
                    Format = request.Format,
                    ReplayMode = request.ReplayMode,
                    Profile = request.Profile,
                    PersistReplay = request.PersistReplay
                },
                cancellationToken);

            sw.Stop();
            _replayDiagnosticsRecorder.Record(new ReplayDiagnosticsEntry
            {
                TimestampUtc = DateTime.UtcNow,
                ComparisonRecordId = comparisonRecordId,
                ComparisonType = result.ComparisonType,
                Format = result.Format,
                ReplayMode = result.ReplayMode,
                PersistReplay = request.PersistReplay,
                DurationMs = sw.ElapsedMilliseconds,
                Success = true,
                VerificationPassed = result.VerificationPassed,
                PersistedReplayRecordId = result.PersistedReplayRecordId,
                MetadataOnly = true
            });
            _logger.LogInformation(
                "Comparison replay metadata completed: ComparisonRecordId={ComparisonRecordId}, ComparisonType={ComparisonType}, Format={Format}, ReplayMode={ReplayMode}, DurationMs={DurationMs}",
                comparisonRecordId, result.ComparisonType, result.Format, result.ReplayMode, sw.ElapsedMilliseconds);

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
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            sw.Stop();
            RecordReplayFailure(comparisonRecordId, request, sw.ElapsedMilliseconds, ex.Message, metadataOnly: true);
            _logger.LogWarning(ex, "Comparison replay metadata not found: ComparisonRecordId={ComparisonRecordId}", comparisonRecordId);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            sw.Stop();
            RecordReplayFailure(comparisonRecordId, request, sw.ElapsedMilliseconds, ex.Message, metadataOnly: true);
            _logger.LogWarning(ex, "Comparison replay metadata failed: ComparisonRecordId={ComparisonRecordId}, Error={Error}", comparisonRecordId, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
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
            return BadRequest(new { error = "comparisonRecordIds is required." });
        }

        if (request.ComparisonRecordIds.Count > 50)
        {
            return BadRequest(new { error = "comparisonRecordIds max is 50." });
        }

        var format = request.Format ?? "markdown";
        var mode = request.ReplayMode ?? "artifact";

        await using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var id in request.ComparisonRecordIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var result = await _comparisonReplayService.ReplayAsync(
                    new AppReplayComparisonRequest
                    {
                        ComparisonRecordId = id,
                        Format = format,
                        ReplayMode = mode,
                        Profile = request.Profile,
                        PersistReplay = request.PersistReplay
                    },
                    cancellationToken);

                var entryName = result.FileName;
                if (string.IsNullOrWhiteSpace(entryName))
                {
                    entryName = $"comparison_{id}.{result.Format}";
                }

                var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();

                if (string.Equals(result.Format, "markdown", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(result.Format, "html", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(result.Content ?? string.Empty);
                    await entryStream.WriteAsync(bytes, cancellationToken);
                }
                else
                {
                    var bytes = result.BinaryContent ?? Array.Empty<byte>();
                    await entryStream.WriteAsync(bytes, cancellationToken);
                }
            }
        }

        ms.Position = 0;
        return File(ms.ToArray(), "application/zip", "comparison_replays.zip");
    }

    private void RecordReplayFailure(
        string comparisonRecordId,
        ApiReplayComparisonRequest request,
        long durationMs,
        string errorMessage,
        bool metadataOnly)
    {
        _replayDiagnosticsRecorder.Record(new ReplayDiagnosticsEntry
        {
            TimestampUtc = DateTime.UtcNow,
            ComparisonRecordId = comparisonRecordId,
            ComparisonType = string.Empty,
            Format = request.Format ?? "markdown",
            ReplayMode = request.ReplayMode ?? "artifact",
            PersistReplay = request.PersistReplay,
            DurationMs = durationMs,
            Success = false,
            ErrorMessage = errorMessage,
            MetadataOnly = metadataOnly
        });
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

