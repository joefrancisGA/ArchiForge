using ArchLucid.Contracts.Notifications;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Notifications;

/// <summary>
/// Read-only customer notification channel toggles for the current tenant (governance promotion Logic Apps fan-out).
/// </summary>
/// <remarks>
/// Does not return email addresses or webhook secrets — operators configure connectors / Key Vault. When no SQL row
/// exists, returns conservative defaults with <see cref="TenantNotificationChannelPreferencesResponse.IsConfigured"/> false.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/notifications")]
[EnableRateLimiting("fixed")]
public sealed class CustomerNotificationChannelPreferencesController(
    IScopeContextProvider scopeProvider,
    ITenantNotificationChannelPreferencesRepository preferencesRepository) : ControllerBase
{
    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly ITenantNotificationChannelPreferencesRepository _preferencesRepository =
        preferencesRepository ?? throw new ArgumentNullException(nameof(preferencesRepository));

    /// <summary>Gets channel toggles for <c>tenantId</c> from the caller’s scope (Logic Apps HTTP action).</summary>
    [HttpGet("customer-channel-preferences")]
    [ProducesResponseType(typeof(TenantNotificationChannelPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCustomerChannelPreferences(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();

        TenantNotificationChannelPreferencesResponse? row =
            await _preferencesRepository.GetByTenantAsync(scope.TenantId, cancellationToken);

        if (row is null)
        {
            return Ok(TenantNotificationChannelPreferencesResponse.Unconfigured(scope.TenantId));
        }

        return Ok(row);
    }
}
