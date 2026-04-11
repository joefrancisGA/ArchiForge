using System.Text.Json;

using ArchLucid.Api.Auth.Models;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Api.Mapping;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Analysis;
using ArchLucid.Application.Jobs;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Serialization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ApiConsultingDocxProfileRecommendationRequest =
    ArchLucid.Api.Models.ConsultingDocxProfileRecommendationRequest;
using AppConsultingDocxExportProfileSelector =
    ArchLucid.Application.Analysis.IConsultingDocxExportProfileSelector;
using AppConsultingDocxProfileRecommendationRequest =
    ArchLucid.Application.Analysis.ConsultingDocxProfileRecommendationRequest;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Builds and exports consolidated analysis reports for a committed run (markdown, DOCX, consulting templates, async jobs).
/// </summary>
/// <remarks>
/// Uses <see cref="IArchitectureAnalysisService"/> for report assembly and <see cref="IRunDetailQueryService"/> for run context.
/// Base route <c>v1/architecture</c> with <see cref="ArchLucidPolicies.ExecuteAuthority"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
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
    IAuditService auditService,
    ILogger<AnalysisReportsController> logger)
    : ControllerBase
{
    /// <summary>
    /// Builds a structured <see cref="ArchLucid.Application.Analysis.ArchitectureAnalysisReport"/> for <paramref name="runId"/> using optional section flags in the body.
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

            Guid? auditRunId = Guid.TryParse(runId, out Guid parsedRunId) ? parsedRunId : null;

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.ArchitectureAnalysisReportGenerated,
                    RunId = auditRunId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            runId,
                            manifestVersion = report.Manifest?.Metadata.ManifestVersion,
                            warningCount = report.Warnings.Count,
                            request.IncludeEvidence,
                            request.IncludeExecutionTraces,
                            request.IncludeManifest,
                            request.IncludeDiagram,
                            request.IncludeSummary,
                            request.IncludeDeterminismCheck,
                        },
                        AuditJsonSerializationOptions.Instance),
                },
                cancellationToken);

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
    [ProducesResponseType(typeof(AsyncJobResponse), StatusCodes.Status202Accepted)]
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

        AnalysisReportDocxWorkUnit workUnit = new(
            AnalysisReportDocxJobPayload.FromAnalysisRequest(request),
            FileName: $"analysis-report-{runId}.docx",
            ContentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        string jobId = await jobs.EnqueueAsync(workUnit, cancellationToken: cancellationToken);

        return Accepted(new AsyncJobResponse { JobId = jobId });
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
    [Authorize(Policy = ArchLucidPolicies.CanExportConsultingDocx)]
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
    [Authorize(Policy = ArchLucidPolicies.CanExportConsultingDocx)]
    [ProducesResponseType(typeof(AsyncJobResponse), StatusCodes.Status202Accepted)]
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

        ConsultingDocxWorkUnit workUnit = new(
            ConsultingDocxJobPayloadMapper.ToPayload(runId, request),
            FileName: $"analysis-report-consulting-{runId}.docx",
            ContentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        string jobId = await jobs.EnqueueAsync(workUnit, cancellationToken: cancellationToken);

        return Accepted(new AsyncJobResponse { JobId = jobId });
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

    // â”€â”€ Private helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

