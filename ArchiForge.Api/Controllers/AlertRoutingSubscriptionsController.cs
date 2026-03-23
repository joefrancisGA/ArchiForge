using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Alerts.Delivery;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Manages <see cref="AlertRoutingSubscription"/> rows for the caller’s scope and exposes recent <see cref="AlertDeliveryAttempt"/> history per subscription.
/// </summary>
/// <remarks>
/// Create/toggle require <see cref="ArchiForgePolicies.ExecuteAuthority"/>; list/attempts require read authority.
/// New subscriptions are stamped with ids and scope from <see cref="IScopeContextProvider"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/alert-routing-subscriptions")]
[EnableRateLimiting("fixed")]
public sealed class AlertRoutingSubscriptionsController(
    IScopeContextProvider scopeProvider,
    IAlertRoutingSubscriptionRepository subscriptionRepository,
    IAlertDeliveryAttemptRepository attemptRepository,
    IAuditService auditService)
    : ControllerBase
{
    /// <summary>Creates a routing subscription bound to the current tenant/workspace/project.</summary>
    [HttpPost]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AlertRoutingSubscription), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertRoutingSubscription>> Create(
        [FromBody] AlertRoutingSubscription subscription,
        CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        subscription.RoutingSubscriptionId = Guid.NewGuid();
        subscription.TenantId = scope.TenantId;
        subscription.WorkspaceId = scope.WorkspaceId;
        subscription.ProjectId = scope.ProjectId;
        subscription.CreatedUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(subscription.MetadataJson))
            subscription.MetadataJson = "{}";

        await subscriptionRepository.CreateAsync(subscription, ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AlertRoutingSubscriptionCreated,
                DataJson = JsonSerializer.Serialize(new
                {
                    subscription.RoutingSubscriptionId,
                    subscription.Name,
                    subscription.ChannelType,
                    subscription.MinimumSeverity,
                }),
            },
            ct);

        return Ok(subscription);
    }

    /// <summary>Lists all routing subscriptions for the current scope (enabled and disabled).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertRoutingSubscription>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertRoutingSubscription>>> List(CancellationToken ct = default)
    {
        var scope = scopeProvider.GetCurrentScope();

        var result = await subscriptionRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(result);
    }

    /// <summary>Toggles <see cref="AlertRoutingSubscription.IsEnabled"/> when the subscription belongs to the current scope.</summary>
    [HttpPost("{routingSubscriptionId:guid}/toggle")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AlertRoutingSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertRoutingSubscription>> Toggle(
        Guid routingSubscriptionId,
        CancellationToken ct = default)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(routingSubscriptionId, ct);
        if (subscription is null)
            return NotFound();

        var scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return NotFound();

        subscription.IsEnabled = !subscription.IsEnabled;
        await subscriptionRepository.UpdateAsync(subscription, ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AlertRoutingSubscriptionToggled,
                DataJson = JsonSerializer.Serialize(new
                {
                    routingSubscriptionId,
                    enabled = subscription.IsEnabled,
                }),
            },
            ct);

        return Ok(subscription);
    }

    /// <summary>Returns recent delivery attempts for a subscription in the current scope.</summary>
    /// <param name="take">Max rows (passed to repository).</param>
    [HttpGet("{routingSubscriptionId:guid}/attempts")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDeliveryAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AlertDeliveryAttempt>>> GetAttempts(
        Guid routingSubscriptionId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(routingSubscriptionId, ct);
        if (subscription is null)
            return NotFound();

        var scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return NotFound();

        var attempts = await attemptRepository.ListBySubscriptionAsync(routingSubscriptionId, take, ct);
        return Ok(attempts);
    }

    private static bool MatchesScope(AlertRoutingSubscription subscription, ScopeContext scope) =>
        subscription.TenantId == scope.TenantId &&
        subscription.WorkspaceId == scope.WorkspaceId &&
        subscription.ProjectId == scope.ProjectId;
}
