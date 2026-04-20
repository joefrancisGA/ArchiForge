using System.Text.Json;

using ArchLucid.Api.Attributes;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Value;
using ArchLucid.ArtifactSynthesis.Docx;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Contracts.ValueReports;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Controllers.ValueReports;

/// <summary>Per-tenant stakeholder DOCX value report (ROI_MODEL-aligned metrics).</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
[RequiresCommercialTenantTier(TenantTier.Standard)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/value-report")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status402PaymentRequired)]
public sealed class ValueReportController(
    ValueReportBuilder valueReportBuilder,
    IValueReportRenderer valueReportRenderer,
    IValueReportJobQueue jobQueue,
    IScopeContextProvider scopeProvider,
    IAuditService auditService,
    IOptionsMonitor<ValueReportComputationOptions> optionsMonitor) : ControllerBase
{
    private readonly ValueReportBuilder _valueReportBuilder =
        valueReportBuilder ?? throw new ArgumentNullException(nameof(valueReportBuilder));
    private readonly IValueReportRenderer _valueReportRenderer =
        valueReportRenderer ?? throw new ArgumentNullException(nameof(valueReportRenderer));
    private readonly IValueReportJobQueue _jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    private readonly IOptionsMonitor<ValueReportComputationOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <summary>Generates a DOCX value report for the tenant (sync) or enqueues background generation for large windows.</summary>
    [HttpPost("{tenantId:guid}/generate")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateAsync(
        Guid tenantId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();

        if (tenantId != scope.TenantId)
            return StatusCode(StatusCodes.Status403Forbidden);

        DateTimeOffset end = to ?? DateTimeOffset.UtcNow;
        DateTimeOffset start = from ?? end.AddDays(-30);

        if (end <= start)
            return this.BadRequestProblem("Query parameter 'to' must be after 'from'.", ProblemTypes.ValidationFailed);

        ValueReportComputationOptions opt = _optionsMonitor.CurrentValue;

        if ((end - start).TotalDays > opt.AsyncJobWhenWindowDaysExceeds)
        {
            Guid jobId = _jobQueue.Enqueue(
                new ValueReportJobRequest(scope.TenantId, scope.WorkspaceId, scope.ProjectId, start, end));

            string location = $"{Request.PathBase}/v1.0/value-report/jobs/{jobId:D}/docx";
            Response.Headers.Location = location;

            return Accepted(new { jobId, status = "pending" });
        }

        ValueReportSnapshot snapshot = await _valueReportBuilder.BuildAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            start,
            end,
            cancellationToken);

        byte[] bytes = await _valueReportRenderer.RenderAsync(snapshot, cancellationToken);

        string fileName = $"ArchLucid-value-report-{tenantId:N}-{start:yyyyMMdd}-{end:yyyyMMdd}.docx";

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ValueReportGenerated,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        tenantId,
                        from = start,
                        to = end,
                        byteCount = bytes.Length,
                        asyncJob = false,
                    }),
            },
            cancellationToken);

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }

    /// <summary>Polls async value-report generation started by <see cref="GenerateAsync"/>.</summary>
    [HttpGet("jobs/{jobId:guid}/docx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetJobDocx(Guid jobId)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();
        ValueReportJobPollResult r = _jobQueue.TryPoll(jobId, scope.TenantId);

        if (!r.Found)
            return this.NotFoundProblem("Value report job was not found.", ProblemTypes.ResourceNotFound);

        if (r.Completed && r.DocxBytes is not null)
        {
            return File(
                r.DocxBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                r.FileName ?? "ArchLucid-value-report.docx");
        }

        if (r.ErrorMessage is not null)
        {
            Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
            {
                Type = ProblemTypes.InternalError,
                Title = "Value report generation failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = r.ErrorMessage,
                Instance = Request.Path.Value,
            };

            ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
            ProblemSupportHints.AttachForProblemType(problem);
            ProblemCorrelation.Attach(problem, HttpContext);

            return new ObjectResult(problem)
            {
                StatusCode = problem.Status,
                ContentTypes = { ApplicationProblemMapper.ProblemJsonMediaType },
            };
        }

        Response.Headers.Append("Retry-After", "2");

        return StatusCode(StatusCodes.Status202Accepted);
    }
}
