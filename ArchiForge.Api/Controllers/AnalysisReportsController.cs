using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Jobs;
using ArchiForge.Api.Mapping;
using ArchiForge.Api.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using ArchiForge.Contracts.Architecture;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ApiConsultingDocxProfileRecommendationRequest =
    ArchiForge.Api.Models.ConsultingDocxProfileRecommendationRequest;
using AppConsultingDocxExportProfileSelector =
    ArchiForge.Application.Analysis.IConsultingDocxExportProfileSelector;
using AppConsultingDocxProfileRecommendationRequest =
    ArchiForge.Application.Analysis.ConsultingDocxProfileRecommendationRequest;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Builds and exports consolidated analysis reports for a committed run (markdown, DOCX, consulting templates, async jobs).
/// </summary>
/// <remarks>
/// Uses <see cref="IArchitectureAnalysisService"/> for report assembly and <see cref="IRunDetailQueryService"/> for run context.
/// Base route <c>v1/architecture</c> with <see cref="ArchiForgePolicies.ExecuteAuthority"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AnalysisReportsController(
    IRunDetailQueryService runDetailQueryService,
    IArchitectureAnalysisService architectureAnalysisService,
    IArchitectureAnalysisExportService architectureAnalysisExportService,
    IArchitectureAnalysisDocxExportService docxExportService,
    IArchitectureAnalysisConsultingDocxExportService architectureAnalysisConsultingDocxExportService,
    IConsultingDocxTemplateRecommendationService consultingDocxTemplateRecommendationService,
    AppConsultingDocxExportProfileSelector consultingDocxExportProfileSelector,
    IRunExportAuditService runExportAuditService,
    IBackgroundJobQueue jobs,
    ILogger<AnalysisReportsController> logger)
    : ControllerBase
{
    /// <summary>
    /// Builds a structured <see cref="ArchiForge.Application.Analysis.ArchitectureAnalysisReport"/> for <paramref name="runId"/> using optional section flags in the body.
    /// </summary>
    [HttpPost("run/{runId}/analysis-report")]
    [ProducesResponseType(typeof(ArchitectureAnalysisReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeRun(
        [FromRoute] string runId,
        [FromBody] ArchitectureAnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ArchitectureAnalysisRequest();
        request.RunId = runId;

        RunDetailLookup runDetail = await LoadRunDetailOrNotFoundAsync(runId, cancellationToken);
        if (runDetail.Error is not null) return runDetail.Error;
        request.PreloadedRunDetail = runDetail.Detail;

        try
        {
            ArchitectureAnalysisReport report = await architectureAnalysisService.BuildAsync(request, cancellationToken);
            return Ok(new ArchitectureAnalysisReportResponse { Report = report });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Analysis failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.BadRequest);
        }
    }

    /// <summary>
    /// Returns the same analysis content as <c>analysis-report</c> serialized to markdown in JSON (<see cref="ArchitectureAnalysisExportResponse"/>).
    /// </summary>
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

        RunDetailLookup runDetail = await LoadRunDetailOrNotFoundAsync(runId, cancellationToken);
        if (runDetail.Error is not null) return runDetail.Error;
        request.PreloadedRunDetail = runDetail.Detail;

        try
        {
            ArchitectureAnalysisReport report = await architectureAnalysisService.BuildAsync(request, cancellationToken);
            string markdown = architectureAnalysisExportService.GenerateMarkdown(report);
            return Ok(new ArchitectureAnalysisExportResponse
            {
                RunId = runId,
                Format = "markdown",
                FileName = $"analysis_{runId}.md",
                Content = markdown
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Analysis export failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
    }

    [HttpPost("run/{runId}/analysis-report/export/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAnalysisReportExport(
        [FromRoute] string runId,
        [FromBody] ArchitectureAnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ArchitectureAnalysisRequest();
        request.RunId = runId;

        RunDetailLookup runDetail = await LoadRunDetailOrNotFoundAsync(runId, cancellationToken);
        if (runDetail.Error is not null) return runDetail.Error;
        request.PreloadedRunDetail = runDetail.Detail;

        try
        {
            ArchitectureAnalysisReport report = await architectureAnalysisService.BuildAsync(request, cancellationToken);
            string markdown = architectureAnalysisExportService.GenerateMarkdown(report);
            return ApiFileResults.RangeText(Request, markdown, "text/markdown", $"analysis-report-{runId}.md");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Analysis export file failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
    }

    [HttpPost("run/{runId}/analysis-report/export/docx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAnalysisReportDocx(
        [FromRoute] string runId,
        [FromBody] ArchitectureAnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ArchitectureAnalysisRequest();
        request.RunId = runId;

        RunDetailLookup runDetail = await LoadRunDetailOrNotFoundAsync(runId, cancellationToken);
        if (runDetail.Error is not null) return runDetail.Error;
        request.PreloadedRunDetail = runDetail.Detail;

        try
        {
            byte[] bytes = await docxExportService.GenerateDocxAsync(
                await architectureAnalysisService.BuildAsync(request, cancellationToken),
                cancellationToken);
            return ApiFileResults.RangeBytes(
                Request,
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"analysis-report-{runId}.docx");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "DOCX export failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
    }

    [HttpPost("run/{runId}/analysis-report/export/docx/async")]
    [ProducesResponseType(typeof(AsyncJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAnalysisReportDocxAsync(
        [FromRoute] string runId,
        [FromBody] ArchitectureAnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ArchitectureAnalysisRequest();
        request.RunId = runId;

        RunDetailLookup runDetail = await LoadRunDetailOrNotFoundAsync(runId, cancellationToken);
        if (runDetail.Error is not null) return runDetail.Error;
        request.PreloadedRunDetail = runDetail.Detail;

        string jobId = jobs.Enqueue(
            fileNameHint: $"analysis-report-{runId}.docx",
            contentTypeHint: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            work: async ct =>
            {
                byte[] bytes = await docxExportService.GenerateDocxAsync(
                    await architectureAnalysisService.BuildAsync(request, ct),
                    ct);
                return new BackgroundJobFile(
                    FileName: $"analysis-report-{runId}.docx",
                    ContentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    Bytes: bytes);
            });

        return Ok(new AsyncJobResponse { JobId = jobId });
    }

    [HttpPost("analysis-report/export/docx/consulting/resolve-profile")]
    [ProducesResponseType(typeof(ConsultingDocxResolveProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ResolveConsultingDocxProfile(
        [FromBody] ConsultingDocxResolveProfileRequest? request)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        // TemplateName is currently advisory only; the selector resolves based on the
        // requested profile key and recommendation inputs.
        ResolvedConsultingDocxExportProfile resolved = consultingDocxExportProfileSelector.Resolve(
            request.Profile,
            new Application.Analysis.ConsultingDocxProfileRecommendationRequest());

        return Ok(new ConsultingDocxResolveProfileResponse
        {
            RequestedProfile = request.Profile,
            RequestedTemplateName = request.TemplateName,
            ResolvedProfile = resolved.SelectedProfileName,
            ResolvedProfileDisplayName = resolved.SelectedProfileDisplayName,
            WasAutoSelected = resolved.WasAutoSelected,
            ResolutionReason = resolved.ResolutionReason
        });
    }

    [HttpPost("run/{runId}/analysis-report/export/docx/consulting")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadConsultingDocx(
        [FromRoute] string runId,
        [FromBody] ConsultingDocxExportRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ConsultingDocxExportRequest();

        RunDetailLookup loaded = await LoadRunDetailOrNotFoundAsync(runId, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;

        try
        {
            ArchitectureAnalysisRequest analysisRequest = new()
            {
                RunId = runId,
                PreloadedRunDetail = loaded.Detail,
                IncludeEvidence = request.IncludeEvidence,
                IncludeExecutionTraces = request.IncludeExecutionTraces,
                IncludeManifest = request.IncludeManifest,
                IncludeDiagram = request.IncludeDiagram,
                // Consulting template options are currently configured globally via IOptions;
                // the API request influences the analysis content via the Include* flags.
                IncludeSummary = true,
                IncludeDeterminismCheck = request.IncludeDeterminismCheck,
                DeterminismIterations = request.DeterminismIterations,
                IncludeManifestCompare = request.IncludeManifestCompare,
                CompareManifestVersion = request.CompareManifestVersion,
                IncludeAgentResultCompare = request.IncludeAgentResultCompare,
                CompareRunId = request.CompareRunId
            };

            ArchitectureAnalysisReport report = await architectureAnalysisService.BuildAsync(
                analysisRequest,
                cancellationToken);

            byte[] bytes = await architectureAnalysisConsultingDocxExportService.GenerateDocxAsync(
                report,
                cancellationToken);

            ResolvedConsultingDocxExportProfile resolvedProfile = consultingDocxExportProfileSelector.Resolve(
                request.TemplateProfile,
                ConsultingDocxExportAuditMapper.ToRecommendationRequest(request));

            PersistedAnalysisExportRequest persistedRequest = ConsultingDocxExportAuditMapper.ToPersistedRequest(request);

            const string consultingDocxExportType = "analysis-report-consulting-docx";

            await runExportAuditService.RecordAsync(
                runId,
                consultingDocxExportType,
                format: "docx",
                fileName: $"analysis-report-consulting-{runId}.docx",
                templateProfile: resolvedProfile.SelectedProfileName,
                templateProfileDisplayName: resolvedProfile.SelectedProfileDisplayName,
                wasAutoSelected: resolvedProfile.WasAutoSelected,
                resolutionReason: resolvedProfile.ResolutionReason,
                manifestVersion: loaded.Detail!.Run.CurrentManifestVersion,
                analysisRequest: persistedRequest,
                cancellationToken: cancellationToken);

            return ApiFileResults.RangeBytes(
                Request,
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"analysis-report-consulting-{runId}.docx");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Consulting DOCX export failed for run '{RunId}'.", runId);
            return this.InvalidOperationProblem(ex, ProblemTypes.ExportFailed);
        }
    }

    [HttpPost("run/{runId}/analysis-report/export/docx/consulting/async")]
    [ProducesResponseType(typeof(AsyncJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadConsultingDocxAsync(
        [FromRoute] string runId,
        [FromBody] ConsultingDocxExportRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ConsultingDocxExportRequest();

        RunDetailLookup loaded = await LoadRunDetailOrNotFoundAsync(runId, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;

        string jobId = jobs.Enqueue(
            fileNameHint: $"analysis-report-consulting-{runId}.docx",
            contentTypeHint: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            work: async ct =>
            {
                ArchitectureAnalysisRequest analysisRequest = ConsultingDocxAnalysisRequestFactory.Create(runId, request);
                analysisRequest.PreloadedRunDetail = loaded.Detail;

                ArchitectureAnalysisReport report = await architectureAnalysisService.BuildAsync(
                    analysisRequest,
                    ct);

                byte[] bytes = await architectureAnalysisConsultingDocxExportService.GenerateDocxAsync(
                    report,
                    ct);
                return new BackgroundJobFile(
                    FileName: $"analysis-report-consulting-{runId}.docx",
                    ContentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    Bytes: bytes);
            });

        return Ok(new AsyncJobResponse { JobId = jobId });
    }

    [HttpPost("analysis-report/export/docx/consulting/profiles/recommend")]
    [ProducesResponseType(typeof(ConsultingDocxProfileRecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RecommendConsultingProfiles(
        [FromBody] ApiConsultingDocxProfileRecommendationRequest? request)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ConsultingDocxProfileRecommendation recommendation = consultingDocxTemplateRecommendationService.Recommend(
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

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Loads the canonical run detail for <paramref name="runId"/>.
    /// Returns a non-null <see cref="RunDetailLookup.Error"/> (404 problem) when the run is not found.
    /// </summary>
    private async Task<RunDetailLookup> LoadRunDetailOrNotFoundAsync(string runId, CancellationToken cancellationToken)
    {
        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        return detail is null ? new RunDetailLookup { Error = this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound) } : new RunDetailLookup { Detail = detail };
    }

    private sealed class RunDetailLookup
    {
        public IActionResult? Error { get; init; }
        public ArchitectureRunDetail? Detail { get; init; }
    }
}

