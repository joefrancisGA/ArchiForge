using ArchLucid.Api.Attributes;
using ArchLucid.Api.Models.Tenancy;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Billing;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Tenancy;

/// <summary>Non-authoritative spend guidance for Standard+ tenants (404 when below Standard; endpoint hidden by tier).</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenant/cost-estimate")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class TenantCostEstimateController(
    ITenantCostEstimateService estimateService,
    IScopeContextProvider scopeProvider) : ControllerBase
{
    private readonly ITenantCostEstimateService _estimateService =
        estimateService ?? throw new ArgumentNullException(nameof(estimateService));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    /// <summary>Returns a configured monthly band for the caller's tenant.</summary>
    [HttpGet]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(TenantCostEstimateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();

        TenantCostEstimate? estimate =
            await _estimateService.TryGetEstimateAsync(scope.TenantId, cancellationToken);

        return estimate is null ? this.NotFoundProblem("No cost estimate is configured for this tenant.", type: ProblemTypes.ResourceNotFound) : Ok(TenantCostEstimateResponse.FromDomain(estimate));
    }
}
