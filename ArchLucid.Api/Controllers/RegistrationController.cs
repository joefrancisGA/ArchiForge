using System.Text.Json;

using ArchLucid.Api.Models.Tenancy;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>Anonymous tenant self-registration (Free tier).</summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("registration")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/register")]
public sealed class RegistrationController(
    ITenantProvisioningService provisioning,
    IAuditService audit,
    ITrialTenantBootstrapService trialBootstrap) : ControllerBase
{
    private readonly ITenantProvisioningService _provisioning =
        provisioning ?? throw new ArgumentNullException(nameof(provisioning));

    private readonly IAuditService _audit = audit ?? throw new ArgumentNullException(nameof(audit));

    private readonly ITrialTenantBootstrapService _trialBootstrap =
        trialBootstrap ?? throw new ArgumentNullException(nameof(trialBootstrap));

    /// <summary>Creates a Free-tier tenant and default workspace (idempotent by organization slug).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantProvisioningResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TenantProvisioningResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] TenantRegistrationRequest? body,
        CancellationToken cancellationToken = default)
    {
        if (body is null)
        {
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);
        }

        try
        {
            TenantProvisioningResult result = await _provisioning.ProvisionAsync(
                new TenantProvisioningRequest
                {
                    Name = body.OrganizationName.Trim(),
                    AdminEmail = body.AdminEmail.Trim(),
                    Tier = TenantTier.Free,
                    AuditActorOverride = body.AdminEmail.Trim(),
                },
                cancellationToken);

            if (result.WasAlreadyProvisioned)
            {
                return this.ConflictProblem(
                    "An organization with this name is already registered.",
                    ProblemTypes.Conflict);
            }

            string actor = body.AdminEmail.Trim();

            await _audit.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TenantSelfRegistered,
                    ActorUserId = actor,
                    ActorUserName = string.IsNullOrWhiteSpace(body.AdminDisplayName) ? actor : body.AdminDisplayName.Trim(),
                    TenantId = result.TenantId,
                    WorkspaceId = result.DefaultWorkspaceId,
                    ProjectId = result.DefaultProjectId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            organizationName = body.OrganizationName.Trim(),
                            adminEmail = body.AdminEmail.Trim(),
                        }),
                },
                cancellationToken);

            await _trialBootstrap.TryBootstrapAfterSelfRegistrationAsync(result, actor, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, result);
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
