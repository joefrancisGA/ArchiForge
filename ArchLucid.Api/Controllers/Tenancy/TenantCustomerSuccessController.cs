using ArchLucid.Api.Attributes;
using ArchLucid.Api.Models.CustomerSuccess;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Tenancy;

/// <summary>Customer health scores and PMF feedback for the active tenant scope.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenant/customer-success")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class TenantCustomerSuccessController(
    ITenantCustomerSuccessRepository customerSuccessRepository,
    IScopeContextProvider scopeProvider) : ControllerBase
{
    private readonly ITenantCustomerSuccessRepository _customerSuccessRepository =
        customerSuccessRepository ?? throw new ArgumentNullException(nameof(customerSuccessRepository));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    /// <summary>Returns the latest materialized health score row when the worker has populated it.</summary>
    [HttpGet("health-score")]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(TenantHealthScoreResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthScoreAsync(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();

        TenantHealthScoreRecord? row = await _customerSuccessRepository.GetHealthScoreAsync(
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            return Ok(
                new TenantHealthScoreResponse { IsCalculated = false });
        }

        return Ok(
            new TenantHealthScoreResponse
            {
                IsCalculated = true,
                EngagementScore = row.EngagementScore,
                BreadthScore = row.BreadthScore,
                QualityScore = row.QualityScore,
                GovernanceScore = row.GovernanceScore,
                SupportScore = row.SupportScore,
                CompositeScore = row.CompositeScore,
                UpdatedUtc = row.UpdatedUtc
            });
    }

    /// <summary>Records thumbs feedback for product instrumentation.</summary>
    [HttpPost("product-feedback")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PostProductFeedbackAsync(
        [FromBody] ProductFeedbackRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = _scopeProvider.GetCurrentScope();

        ProductFeedbackSubmission submission = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            FindingRef = request.FindingRef,
            RunId = request.RunId,
            Score = request.Score,
            Comment = request.Comment
        };

        await _customerSuccessRepository.InsertProductFeedbackAsync(submission, cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }
}
