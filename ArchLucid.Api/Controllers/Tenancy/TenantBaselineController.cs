using System.Text.Json;

using ArchLucid.Api.Models.Tenancy;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Tenancy;

/// <summary>Deferred ROI baseline (manual prep hours, people per review) for the tenant in <see cref="IScopeContextProvider" /> scope.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenant/baseline")]
public sealed class TenantBaselineController(
    ITenantRepository tenantRepository,
    IScopeContextProvider scopeProvider,
    IAuditService auditService) : ControllerBase
{
    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    [HttpGet]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(TenantBaselineGetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();
        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(scope.TenantId, cancellationToken);

        if (tenant is null)
            return this.NotFoundProblem("Tenant not found.", ProblemTypes.ResourceNotFound);

        return Ok(
            new TenantBaselineGetResponse
            {
                ManualPrepHoursPerReview = tenant.BaselineManualPrepHoursPerReview,
                PeoplePerReview = tenant.BaselinePeoplePerReview,
                CapturedUtc = tenant.BaselineManualPrepCapturedUtc
            });
    }

    [HttpPut]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(TenantBaselineGetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutAsync(
        [FromBody] TenantBaselinePutRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);
        }

        if (body.ManualPrepHoursPerReview is <= 0m or > 10_000m)
        {
            return this.BadRequestProblem(
                "Manual preparation hours per review must be between 0 and 10,000 (exclusive of zero) when set.",
                ProblemTypes.ValidationFailed);
        }

        if (body.PeoplePerReview is <= 0 or > 10_000)
        {
            return this.BadRequestProblem(
                "People involved per review must be between 1 and 10,000 when set.",
                ProblemTypes.ValidationFailed);
        }

        ScopeContext scope = _scopeProvider.GetCurrentScope();
        TenantRecord? existing = await _tenantRepository.GetByIdAsync(scope.TenantId, cancellationToken);

        if (existing is null)
            return this.NotFoundProblem("Tenant not found.", ProblemTypes.ResourceNotFound);

        if (body.ManualPrepHoursPerReview is null && body.PeoplePerReview is null)
        {
            return Ok(
                new TenantBaselineGetResponse
                {
                    ManualPrepHoursPerReview = existing.BaselineManualPrepHoursPerReview,
                    PeoplePerReview = existing.BaselinePeoplePerReview,
                    CapturedUtc = existing.BaselineManualPrepCapturedUtc
                });
        }

        decimal? prep = body.ManualPrepHoursPerReview ?? existing.BaselineManualPrepHoursPerReview;
        int? people = body.PeoplePerReview ?? existing.BaselinePeoplePerReview;

        if (prep is <= 0m or > 10_000m)
        {
            return this.BadRequestProblem(
                "Manual preparation hours per review must be between 0 and 10,000 (exclusive of zero).",
                ProblemTypes.ValidationFailed);
        }

        if (people is <= 0 or > 10_000)
        {
            return this.BadRequestProblem(
                "People involved per review must be between 1 and 10,000.",
                ProblemTypes.ValidationFailed);
        }

        bool firstCapture = existing.BaselineManualPrepCapturedUtc is null;
        DateTimeOffset captured = DateTimeOffset.UtcNow;
        await _tenantRepository.UpdateBaselineAsync(scope.TenantId, prep, people, captured, cancellationToken);
        ArchLucidInstrumentation.RecordBaselineManualPrepCaptured();
        string actor = User.Identity?.Name ?? "operator";
        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = firstCapture
                    ? AuditEventTypes.TrialBaselineManualPrepCaptured
                    : AuditEventTypes.TrialBaselineManualPrepUpdated,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        manualPrepHoursPerReview = prep,
                        peoplePerReview = people,
                        capturedUtc = captured
                    })
            },
            cancellationToken);

        TenantRecord? readBack = await _tenantRepository.GetByIdAsync(scope.TenantId, cancellationToken);

        return Ok(
            new TenantBaselineGetResponse
            {
                ManualPrepHoursPerReview = readBack?.BaselineManualPrepHoursPerReview,
                PeoplePerReview = readBack?.BaselinePeoplePerReview,
                CapturedUtc = readBack?.BaselineManualPrepCapturedUtc
            });
    }
}
