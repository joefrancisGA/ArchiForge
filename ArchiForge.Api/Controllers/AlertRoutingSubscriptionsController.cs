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
