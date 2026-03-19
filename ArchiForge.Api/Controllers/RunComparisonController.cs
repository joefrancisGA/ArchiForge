using ArchiForge.Api;
using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Application.Analysis;
using ArchiForge.Application.Diffs;
using ArchiForge.Data.Repositories;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api.Controllers;

/// <summary>Run-to-run comparison endpoints (agents, end-to-end replay compare).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public sealed class RunComparisonController(
    IArchitectureRunRepository runRepository,
    IAgentResultRepository resultRepository,
    IAgentResultDiffService agentResultDiffService,
    IAgentResultDiffSummaryFormatter agentResultDiffSummaryFormatter,
    IEndToEndReplayComparisonService endToEndReplayComparisonService,
    IEndToEndReplayComparisonSummaryFormatter endToEndReplayComparisonSummaryFormatter,
    IEndToEndReplayComparisonExportService endToEndReplayComparisonExportService,
    IComparisonAuditService comparisonAuditService)
    : ControllerBase
{
    [HttpGet("run/compare/agents")]
    [ProducesResponseType(typeof(AgentResultCompareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareAgentResults(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        var leftRun = await runRepository.GetByIdAsync(leftRunId, cancellationToken);
        if (leftRun is null)
            return this.NotFoundProblem($"Run '{leftRunId}' was not found.", ProblemTypes.RunNotFound);

        var rightRun = await runRepository.GetByIdAsync(rightRunId, cancellationToken);
        if (rightRun is null)
            return this.NotFoundProblem($"Run '{rightRunId}' was not found.", ProblemTypes.RunNotFound);

        var leftResults = await resultRepository.GetByRunIdAsync(leftRunId, cancellationToken);
        var rightResults = await resultRepository.GetByRunIdAsync(rightRunId, cancellationToken);
        var diff = agentResultDiffService.Compare(leftRunId, leftResults, rightRunId, rightResults);
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
        var leftRun = await runRepository.GetByIdAsync(leftRunId, cancellationToken);
        if (leftRun is null)
            return this.NotFoundProblem($"Run '{leftRunId}' was not found.", ProblemTypes.RunNotFound);

        var rightRun = await runRepository.GetByIdAsync(rightRunId, cancellationToken);
        if (rightRun is null)
            return this.NotFoundProblem($"Run '{rightRunId}' was not found.", ProblemTypes.RunNotFound);

        var leftResults = await resultRepository.GetByRunIdAsync(leftRunId, cancellationToken);
        var rightResults = await resultRepository.GetByRunIdAsync(rightRunId, cancellationToken);
        var diff = agentResultDiffService.Compare(leftRunId, leftResults, rightRunId, rightResults);
        var summary = agentResultDiffSummaryFormatter.FormatMarkdown(diff);
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
        var report = await endToEndReplayComparisonService.BuildAsync(leftRunId, rightRunId, cancellationToken);
        return Ok(new EndToEndReplayComparisonResponse { Report = report });
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
        var report = await endToEndReplayComparisonService.BuildAsync(leftRunId, rightRunId, cancellationToken);
        var summary = endToEndReplayComparisonSummaryFormatter.FormatMarkdown(report);
        if (request.Persist)
        {
            var comparisonRecordId = await comparisonAuditService.RecordEndToEndAsync(report, summary, cancellationToken);
            Response.Headers["X-ArchiForge-ComparisonRecordId"] = comparisonRecordId;
        }

        return Ok(new EndToEndReplayComparisonSummaryResponse { Format = "markdown", Summary = summary });
    }

    [HttpGet("run/compare/end-to-end/export")]
    [ProducesResponseType(typeof(EndToEndReplayComparisonExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRunsEndToEndComparisonMarkdown(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        var report = await endToEndReplayComparisonService.BuildAsync(leftRunId, rightRunId, cancellationToken);
        var markdown = endToEndReplayComparisonExportService.GenerateMarkdown(report);
        var fileName = $"end_to_end_compare_{leftRunId}_to_{rightRunId}.md";
        return Ok(new EndToEndReplayComparisonExportResponse
        {
            Format = "markdown",
            FileName = fileName,
            Content = markdown
        });
    }

    [HttpGet("run/compare/end-to-end/export/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadRunsEndToEndComparisonMarkdown(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        var report = await endToEndReplayComparisonService.BuildAsync(leftRunId, rightRunId, cancellationToken);
        var markdown = endToEndReplayComparisonExportService.GenerateMarkdown(report);
        var fileName = $"end_to_end_compare_{leftRunId}_to_{rightRunId}.md";
        return ApiFileResults.RangeText(Request, markdown, "text/markdown", fileName);
    }

    [HttpGet("run/compare/end-to-end/export/docx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRunsEndToEndComparisonDocx(
        [FromQuery] string leftRunId,
        [FromQuery] string rightRunId,
        CancellationToken cancellationToken)
    {
        var report = await endToEndReplayComparisonService.BuildAsync(leftRunId, rightRunId, cancellationToken);
        var bytes = await endToEndReplayComparisonExportService.GenerateDocxAsync(report, cancellationToken);
        var fileName = $"end_to_end_compare_{leftRunId}_to_{rightRunId}.docx";
        return ApiFileResults.RangeBytes(
            Request,
            bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }
}
