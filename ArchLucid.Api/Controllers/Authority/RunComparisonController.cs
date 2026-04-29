using ArchLucid.Api.Attributes;
using ArchLucid.Api.Http;
using ArchLucid.Api.Mapping;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Analysis;
using ArchLucid.Application.Diffs;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Authority;

/// <summary>Run-to-run comparison endpoints (agents, end-to-end replay compare).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class RunComparisonController(
    IRunDetailQueryService runDetailQueryService,
    IAgentResultDiffService agentResultDiffService,
    IAgentResultDiffSummaryFormatter agentResultDiffSummaryFormatter,
    IEndToEndReplayComparisonService endToEndReplayComparisonService,
    IEndToEndReplayComparisonSummaryFormatter endToEndReplayComparisonSummaryFormatter,
    IEndToEndReplayComparisonExportService endToEndReplayComparisonExportService,
    IComparisonAuditService comparisonAuditService,
    IValidator<RunPairQuery> runPairQueryValidator)
    : ControllerBase
{
    [HttpGet("run/compare/agents")]
    [ProducesResponseType(typeof(AgentResultCompareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareAgentResults(
        [FromQuery] RunPairQuery query,
        CancellationToken cancellationToken)
    {
        (IActionResult? error, ArchitectureRunDetail? leftDetail, ArchitectureRunDetail? rightDetail) =
            await LoadValidatedRunPairAsync(query, cancellationToken);
        if (error is not null)
            return error;

        AgentResultDiffResult diff = agentResultDiffService.Compare(
            query.LeftRunId,
            leftDetail!.Results,
            query.RightRunId,
            rightDetail!.Results);
        return Ok(ComparisonResponseMapper.ToAgentResultCompareResponse(diff));
    }

    [HttpGet("run/compare/agents/summary")]
    [ProducesResponseType(typeof(AgentResultCompareSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareAgentResultsSummary(
        [FromQuery] RunPairQuery query,
        CancellationToken cancellationToken)
    {
        (IActionResult? error, ArchitectureRunDetail? leftDetail, ArchitectureRunDetail? rightDetail) =
            await LoadValidatedRunPairAsync(query, cancellationToken);
        if (error is not null)
            return error;

        AgentResultDiffResult diff = agentResultDiffService.Compare(
            query.LeftRunId,
            leftDetail!.Results,
            query.RightRunId,
            rightDetail!.Results);
        string summary = agentResultDiffSummaryFormatter.FormatMarkdown(diff);
        return Ok(ComparisonResponseMapper.ToAgentResultCompareSummaryResponse(summary, diff));
    }

    [HttpGet("run/compare/end-to-end")]
    [ProducesResponseType(typeof(EndToEndReplayComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareRunsEndToEnd(
        [FromQuery] RunPairQuery query,
        CancellationToken cancellationToken)
    {
        (IActionResult? error, EndToEndReplayComparisonReport? report) =
            await BuildEndToEndReportAsync(query, cancellationToken);
        return error ?? Ok(ComparisonResponseMapper.ToEndToEndResponse(report!));
    }

    [HttpPost("run/compare/end-to-end/summary")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(EndToEndReplayComparisonSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareRunsEndToEndSummary(
        [FromQuery] RunPairQuery query,
        [FromBody] PersistComparisonRequest? request,
        CancellationToken cancellationToken)
    {
        (IActionResult? error, EndToEndReplayComparisonReport? report) =
            await BuildEndToEndReportAsync(query, cancellationToken);
        if (error is not null)
            return error;

        request ??= new PersistComparisonRequest();
        string summary = endToEndReplayComparisonSummaryFormatter.FormatMarkdown(report!);

        if (!request.Persist)
            return Ok(ComparisonResponseMapper.ToEndToEndSummaryResponse(summary));

        string comparisonRecordId =
            await comparisonAuditService.RecordEndToEndAsync(report!, summary, cancellationToken);
        Response.Headers[ArchLucidHttpHeaders.ComparisonRecordId] = comparisonRecordId;

        return Ok(ComparisonResponseMapper.ToEndToEndSummaryResponse(summary));
    }

    [HttpGet("run/compare/end-to-end/export")]
    [ProducesResponseType(typeof(EndToEndReplayComparisonExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRunsEndToEndComparisonMarkdown(
        [FromQuery] RunPairQuery query,
        CancellationToken cancellationToken)
    {
        (IActionResult? error, EndToEndReplayComparisonReport? report) =
            await BuildEndToEndReportAsync(query, cancellationToken);
        if (error is not null)
            return error;
        string markdown = endToEndReplayComparisonExportService.GenerateMarkdown(report!);
        string fileName = $"end_to_end_compare_{query.LeftRunId}_to_{query.RightRunId}.md";
        return Ok(ComparisonResponseMapper.ToEndToEndExportResponse(fileName, markdown));
    }

    [HttpGet("run/compare/end-to-end/export/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadRunsEndToEndComparisonMarkdown(
        [FromQuery] RunPairQuery query,
        CancellationToken cancellationToken)
    {
        (IActionResult? error, EndToEndReplayComparisonReport? report) =
            await BuildEndToEndReportAsync(query, cancellationToken);
        if (error is not null)
            return error;
        string markdown = endToEndReplayComparisonExportService.GenerateMarkdown(report!);
        string fileName = $"end_to_end_compare_{query.LeftRunId}_to_{query.RightRunId}.md";
        return ApiFileResults.RangeText(Request, markdown, "text/markdown", fileName);
    }

    [HttpGet("run/compare/end-to-end/export/docx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRunsEndToEndComparisonDocx(
        [FromQuery] RunPairQuery query,
        CancellationToken cancellationToken)
    {
        (IActionResult? error, EndToEndReplayComparisonReport? report) =
            await BuildEndToEndReportAsync(query, cancellationToken);
        if (error is not null)
            return error;
        byte[] bytes = await endToEndReplayComparisonExportService.GenerateDocxAsync(report!, cancellationToken);
        string fileName = $"end_to_end_compare_{query.LeftRunId}_to_{query.RightRunId}.docx";
        return ApiFileResults.RangeBytes(
            Request,
            bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }

    /// <summary>
    ///     Validates the query and builds the end-to-end comparison report.
    ///     Returns a non-null error result when validation fails.
    /// </summary>
    private async Task<(IActionResult? Error, EndToEndReplayComparisonReport? Report)> BuildEndToEndReportAsync(
        RunPairQuery query,
        CancellationToken cancellationToken)
    {
        IActionResult? error = await ValidateRunPairQueryAsync(query, cancellationToken);
        if (error is not null)
            return (error, null);

        EndToEndReplayComparisonReport report =
            await endToEndReplayComparisonService.BuildAsync(query.LeftRunId, query.RightRunId, cancellationToken);
        return (null, report);
    }

    private async Task<IActionResult?> ValidateRunPairQueryAsync(RunPairQuery query,
        CancellationToken cancellationToken)
    {
        ValidationResult? validation = await runPairQueryValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return this.BadRequestProblem(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)),
                ProblemTypes.ValidationFailed);


        return null;
    }

    /// <summary>
    ///     Validates the query, loads both runs through <see cref="IRunDetailQueryService" />, and returns 404 when either run
    ///     is missing.
    /// </summary>
    private async Task<(IActionResult? Error, ArchitectureRunDetail? Left, ArchitectureRunDetail? Right)>
        LoadValidatedRunPairAsync(
            RunPairQuery query,
            CancellationToken cancellationToken)
    {
        IActionResult? queryError = await ValidateRunPairQueryAsync(query, cancellationToken);
        if (queryError is not null)
            return (queryError, null, null);

        ArchitectureRunDetail? left = await runDetailQueryService.GetRunDetailAsync(query.LeftRunId, cancellationToken);
        if (left is null)
            return (this.NotFoundProblem($"Run '{query.LeftRunId}' was not found.", ProblemTypes.RunNotFound), null,
                null);

        ArchitectureRunDetail? right =
            await runDetailQueryService.GetRunDetailAsync(query.RightRunId, cancellationToken);
        if (right is null)
            return (this.NotFoundProblem($"Run '{query.RightRunId}' was not found.", ProblemTypes.RunNotFound), null,
                null);

        return (null, left, right);
    }
}
