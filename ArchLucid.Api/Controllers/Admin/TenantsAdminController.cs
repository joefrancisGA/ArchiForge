using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>Tenant registry provisioning (admin-only).</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.AdminAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/tenants")]
public sealed class TenantsAdminController(ITenantRepository tenantRepository, ITenantProvisioningService provisioning)
    : ControllerBase
{
    private readonly ITenantProvisioningService _provisioning =
        provisioning ?? throw new ArgumentNullException(nameof(provisioning));

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    /// <summary>Lists registered tenants (global admin metadata; not RLS-scoped).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TenantRecord> rows = await _tenantRepository.ListAsync(cancellationToken);

        return Ok(rows);
    }

    /// <summary>Creates a tenant + default workspace identifiers (idempotent by derived slug).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantProvisioningResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProvisionAsync(
        [FromBody] TenantProvisionAdminRequest? body,
        CancellationToken cancellationToken = default)
    {
        if (body is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        try
        {
            TenantProvisioningResult result = await _provisioning.ProvisionAsync(
                new TenantProvisioningRequest { Name = body.Name, AdminEmail = body.AdminEmail, Tier = body.Tier },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
    }
}
