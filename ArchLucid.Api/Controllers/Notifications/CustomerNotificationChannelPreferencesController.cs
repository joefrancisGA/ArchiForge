using System.Text.Json;

using ArchLucid.Api.Attributes;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Contracts.Notifications;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Data.Repositories;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Notifications;

/// <summary>
///     Customer notification channel toggles for the current tenant (governance promotion Logic Apps fan-out).
/// </summary>
/// <remarks>
///     Does not return email addresses or webhook secrets — operators configure connectors / Key Vault. When no SQL row
///     exists, GET returns conservative defaults with
///     <see cref="TenantNotificationChannelPreferencesResponse.IsConfigured" /> false.
/// </remarks>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/notifications")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class CustomerNotificationChannelPreferencesController(
    IScopeContextProvider scopeProvider,
    ITenantNotificationChannelPreferencesRepository preferencesRepository,
    IAuditService auditService) : ControllerBase
{
    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ITenantNotificationChannelPreferencesRepository _preferencesRepository =
        preferencesRepository ?? throw new ArgumentNullException(nameof(preferencesRepository));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    /// <summary>Gets channel toggles for <c>tenantId</c> from the caller’s scope (Logic Apps HTTP action).</summary>
    [HttpGet("customer-channel-preferences")]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(TenantNotificationChannelPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCustomerChannelPreferences(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();

        TenantNotificationChannelPreferencesResponse? row =
            await _preferencesRepository.GetByTenantAsync(scope.TenantId, cancellationToken);

        return Ok(row ?? TenantNotificationChannelPreferencesResponse.Unconfigured(scope.TenantId));
    }

    /// <summary>Replaces channel toggles for the caller’s tenant (Execute+).</summary>
    [HttpPut("customer-channel-preferences")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(TenantNotificationChannelPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutCustomerChannelPreferences(
        [FromBody] TenantNotificationChannelPreferencesUpsertRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return this.BadRequestProblem(
                "Request body is required.",
                ProblemTypes.ValidationFailed);
        }

        ScopeContext scope = _scopeProvider.GetCurrentScope();

        TenantNotificationChannelPreferencesResponse? saved = await _preferencesRepository.UpsertAsync(
            scope.TenantId,
            body.EmailCustomerNotificationsEnabled,
            body.TeamsCustomerNotificationsEnabled,
            body.OutboundWebhookCustomerNotificationsEnabled,
            cancellationToken);

        if (saved is null)
        {
            return this.NotFoundProblem(
                "Tenant was not found for the current scope.",
                ProblemTypes.ResourceNotFound);
        }

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TenantNotificationChannelPreferencesUpdated,
                ActorUserId = User.Identity?.Name ?? "operator",
                ActorUserName = User.Identity?.Name ?? "operator",
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        email = saved.EmailCustomerNotificationsEnabled,
                        teams = saved.TeamsCustomerNotificationsEnabled,
                        outboundWebhook = saved.OutboundWebhookCustomerNotificationsEnabled
                    })
            },
            cancellationToken);

        return Ok(saved);
    }
}
