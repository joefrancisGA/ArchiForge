using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Api.ProductLearning;
using ArchLucid.Contracts.Abstractions.ProductLearning;
using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Coordination.ProductLearning;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Advisory;

/// <summary>
/// Scoped read APIs for pilot feedback rollups: dashboard KPIs, improvement opportunities, artifact trends, and triage queue slices.
/// </summary>
/// <remarks>
/// Base route <c>v1/product-learning</c>. Aligns with <see cref="ProductLearningScope"/> from <see cref="IScopeContextProvider"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/product-learning")]
[EnableRateLimiting("fixed")]
public sealed class ProductLearningController(
    IProductLearningDashboardService dashboardService,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    private static readonly JsonSerializerOptions ReportFileJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    /// <summary>KPIs and explanatory notes only (no aggregate arrays).</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ProductLearningDashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary([FromQuery] string? since, CancellationToken cancellationToken)
    {
        if (!ProductLearningQueryParser.TryParseOptionalSince(since, out DateTime? sinceUtc, out string? sinceError))

            return this.BadRequestProblem(sinceError!, ProblemTypes.ValidationFailed);


        ScopeContext scopeContext = scopeProvider.GetCurrentScope();
        ProductLearningScope scope = ToProductLearningScope(scopeContext);

        ProductLearningTriageOptions options = new()
        {
            SinceUtc = sinceUtc
        };

        LearningDashboardSummary full = await dashboardService.GetDashboardSummaryAsync(scope, options, cancellationToken);

        ProductLearningDashboardSummaryResponse body = new()
        {
            GeneratedUtc = full.GeneratedUtc,
            TenantId = full.TenantId,
            WorkspaceId = full.WorkspaceId,
            ProjectId = full.ProjectId,
            TotalSignalsInScope = full.TotalSignalsInScope,
            DistinctRunsTouched = full.DistinctRunsTouched,
            TopAggregateCount = full.TopAggregates.Count,
            ArtifactTrendCount = full.ArtifactTrends.Count,
            ImprovementOpportunityCount = full.Opportunities.Count,
            TriageQueueItemCount = full.TriageQueue.Count,
            SummaryNotes = full.SummaryNotes,
        };

        return Ok(body);
    }

    /// <summary>Top improvement opportunities after deterministic ranking (cap via <c>maxOpportunities</c>).</summary>
    [HttpGet("improvement-opportunities")]
    [ProducesResponseType(typeof(ProductLearningImprovementOpportunitiesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetImprovementOpportunities(
        [FromQuery] string? since,
        [FromQuery] string? maxOpportunities,
        CancellationToken cancellationToken)
    {
        if (!ProductLearningQueryParser.TryParseOptionalSince(since, out DateTime? sinceUtc, out string? sinceError))

            return this.BadRequestProblem(sinceError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseMaxImprovementOpportunities(maxOpportunities, out int maxOpp, out string? maxError))

            return this.BadRequestProblem(maxError!, ProblemTypes.ValidationFailed);


        ScopeContext scopeContext = scopeProvider.GetCurrentScope();
        ProductLearningScope scope = ToProductLearningScope(scopeContext);

        ProductLearningTriageOptions options = new()
        {
            SinceUtc = sinceUtc,
            MaxImprovementOpportunities = maxOpp,
        };

        LearningDashboardSummary full = await dashboardService.GetDashboardSummaryAsync(scope, options, cancellationToken);

        return Ok(new ProductLearningImprovementOpportunitiesResponse
        {
            GeneratedUtc = full.GeneratedUtc,
            Opportunities = full.Opportunities,
        });
    }

    /// <summary>Artifact outcome trend rows for charts (same noise gates as the full dashboard).</summary>
    [HttpGet("artifact-outcome-trends")]
    [ProducesResponseType(typeof(ProductLearningArtifactOutcomeTrendsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetArtifactOutcomeTrends([FromQuery] string? since, CancellationToken cancellationToken)
    {
        if (!ProductLearningQueryParser.TryParseOptionalSince(since, out DateTime? sinceUtc, out string? sinceError))

            return this.BadRequestProblem(sinceError!, ProblemTypes.ValidationFailed);


        ScopeContext scopeContext = scopeProvider.GetCurrentScope();
        ProductLearningScope scope = ToProductLearningScope(scopeContext);

        ProductLearningTriageOptions options = new()
        {
            SinceUtc = sinceUtc
        };

        LearningDashboardSummary full = await dashboardService.GetDashboardSummaryAsync(scope, options, cancellationToken);

        return Ok(new ProductLearningArtifactOutcomeTrendsResponse
        {
            GeneratedUtc = full.GeneratedUtc,
            Trends = full.ArtifactTrends,
        });
    }

    /// <summary>Triage queue slice (merged opportunities + repeated-comment themes), ordered deterministically.</summary>
    [HttpGet("triage-queue")]
    [ProducesResponseType(typeof(ProductLearningTriageQueueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTriageQueue(
        [FromQuery] string? since,
        [FromQuery] string? maxTriageItems,
        CancellationToken cancellationToken)
    {
        if (!ProductLearningQueryParser.TryParseOptionalSince(since, out DateTime? sinceUtc, out string? sinceError))

            return this.BadRequestProblem(sinceError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseMaxTriageQueueItems(maxTriageItems, out int maxTriage, out string? maxError))

            return this.BadRequestProblem(maxError!, ProblemTypes.ValidationFailed);


        ScopeContext scopeContext = scopeProvider.GetCurrentScope();
        ProductLearningScope scope = ToProductLearningScope(scopeContext);

        ProductLearningTriageOptions options = new()
        {
            SinceUtc = sinceUtc,
            MaxTriageQueueItems = maxTriage,
        };

        LearningDashboardSummary full = await dashboardService.GetDashboardSummaryAsync(scope, options, cancellationToken);

        return Ok(new ProductLearningTriageQueueResponse
        {
            GeneratedUtc = full.GeneratedUtc,
            Items = full.TriageQueue,
        });
    }

    /// <summary>
    /// Triage-friendly export: markdown (JSON wrapper) or structured JSON. Omits raw pilot comments.
    /// Uses slightly wider internal caps than UI slices so the report can list more ranked items.
    /// </summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(ProductLearningReportExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProductLearningTriageReportDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTriageReport(
        [FromQuery] string? since,
        [FromQuery] string? format,
        [FromQuery] string? maxReportArtifacts,
        [FromQuery] string? maxReportImprovements,
        [FromQuery] string? maxReportTriage,
        CancellationToken cancellationToken)
    {
        if (!ProductLearningQueryParser.TryParseOptionalSince(since, out DateTime? sinceUtc, out string? sinceError))

            return this.BadRequestProblem(sinceError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseReportFormat(format, out string formatNorm, out string? formatError))

            return this.BadRequestProblem(formatError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseMaxReportArtifacts(maxReportArtifacts, out int maxArt, out string? artError))

            return this.BadRequestProblem(artError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseMaxReportImprovements(maxReportImprovements, out int maxImp, out string? impError))

            return this.BadRequestProblem(impError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseMaxReportTriagePreview(maxReportTriage, out int maxTr, out string? trError))

            return this.BadRequestProblem(trError!, ProblemTypes.ValidationFailed);


        ScopeContext scopeContext = scopeProvider.GetCurrentScope();
        ProductLearningScope scope = ToProductLearningScope(scopeContext);

        LearningDashboardSummary full =
            await dashboardService.GetDashboardSummaryAsync(scope, ReportDashboardOptions(sinceUtc), cancellationToken);

        ProductLearningTriageReportLimits limits = new()
        {
            MaxArtifactRows = maxArt,
            MaxImprovements = maxImp,
            MaxTriagePreview = maxTr,
            MaxProblemAreaLines = 8,
            MaxSummaryChars = 240,
        };

        ProductLearningTriageReportDocument document =
            ProductLearningTriageReportBuilder.Build(full, limits, sinceUtc);

        if (formatNorm == "json")

            return Ok(document);


        string markdown = ProductLearningTriageReportMarkdownFormatter.Format(document);

        return Ok(
            new ProductLearningReportExportResponse
            {
                Format = "markdown",
                FileName = "product-learning-triage-report.md",
                Content = markdown,
            });
    }

    /// <summary>Same body as <see cref="GetTriageReport"/> as a downloadable file (<c>.md</c> or <c>.json</c>).</summary>
    [HttpGet("report/file")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadTriageReport(
        [FromQuery] string? since,
        [FromQuery] string? format,
        [FromQuery] string? maxReportArtifacts,
        [FromQuery] string? maxReportImprovements,
        [FromQuery] string? maxReportTriage,
        CancellationToken cancellationToken)
    {
        if (!ProductLearningQueryParser.TryParseOptionalSince(since, out DateTime? sinceUtc, out string? sinceError))

            return this.BadRequestProblem(sinceError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseReportFormat(format, out string formatNorm, out string? formatError))

            return this.BadRequestProblem(formatError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseMaxReportArtifacts(maxReportArtifacts, out int maxArt, out string? artError))

            return this.BadRequestProblem(artError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseMaxReportImprovements(maxReportImprovements, out int maxImp, out string? impError))

            return this.BadRequestProblem(impError!, ProblemTypes.ValidationFailed);


        if (!ProductLearningQueryParser.TryParseMaxReportTriagePreview(maxReportTriage, out int maxTr, out string? trError))

            return this.BadRequestProblem(trError!, ProblemTypes.ValidationFailed);


        ScopeContext scopeContext = scopeProvider.GetCurrentScope();
        ProductLearningScope scope = ToProductLearningScope(scopeContext);

        LearningDashboardSummary full =
            await dashboardService.GetDashboardSummaryAsync(scope, ReportDashboardOptions(sinceUtc), cancellationToken);

        ProductLearningTriageReportLimits limits = new()
        {
            MaxArtifactRows = maxArt,
            MaxImprovements = maxImp,
            MaxTriagePreview = maxTr,
            MaxProblemAreaLines = 8,
            MaxSummaryChars = 240,
        };

        ProductLearningTriageReportDocument document =
            ProductLearningTriageReportBuilder.Build(full, limits, sinceUtc);

        if (formatNorm == "json")
        {
            string json = JsonSerializer.Serialize(document, ReportFileJsonOptions);
            return ApiFileResults.RangeText(Request, json, "application/json", "product-learning-triage-report.json");
        }

        string markdown = ProductLearningTriageReportMarkdownFormatter.Format(document);

        return ApiFileResults.RangeText(Request, markdown, "text/markdown", "product-learning-triage-report.md");
    }

    /// <summary>Wider caps than UI list endpoints so exports include a fuller ranked set (still bounded).</summary>
    private static ProductLearningTriageOptions ReportDashboardOptions(DateTime? sinceUtc) =>
        new()
        {
            SinceUtc = sinceUtc,
            MaxImprovementOpportunities = 50,
            MaxTriageQueueItems = 40,
            MaxArtifactTrends = 100,
            MaxFeedbackRollups = 200,
        };

    private static ProductLearningScope ToProductLearningScope(ScopeContext scopeContext)
    {
        if (scopeContext is null)

            throw new ArgumentNullException(nameof(scopeContext));


        return new ProductLearningScope
        {
            TenantId = scopeContext.TenantId,
            WorkspaceId = scopeContext.WorkspaceId,
            ProjectId = scopeContext.ProjectId,
        };
    }
}
