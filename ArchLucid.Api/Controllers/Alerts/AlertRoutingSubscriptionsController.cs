using System.Text.Json;

using ArchLucid.Core.Authorization;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Alerts.Delivery;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Manages <see cref="AlertRoutingSubscription"/> rows for the caller’s scope and exposes recent <see cref="AlertDeliveryAttempt"/> history per subscription.
/// </summary>
/// <remarks>
/// Create/toggle require <see cref="ArchLucidPolicies.ExecuteAuthority"/>; list/attempts require read authority.
/// New subscriptions are stamped with ids and scope from <see cref="IScopeContextProvider"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
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
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AlertRoutingSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] AlertRoutingSubscription? subscription,
        CancellationToken ct = default)
    {
        if (subscription is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();

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
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<AlertRoutingSubscription> result = await subscriptionRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(result);
    }

    /// <summary>Toggles <see cref="AlertRoutingSubscription.IsEnabled"/> when the subscription belongs to the current scope.</summary>
    [HttpPost("{routingSubscriptionId:guid}/toggle")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(AlertRoutingSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Toggle(
        Guid routingSubscriptionId,
        CancellationToken ct = default)
    {
        AlertRoutingSubscription? subscription = await subscriptionRepository.GetByIdAsync(routingSubscriptionId, ct);
        if (subscription is null)
            return this.NotFoundProblem($"Routing subscription '{routingSubscriptionId}' was not found.", ProblemTypes.ResourceNotFound);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return this.NotFoundProblem($"Routing subscription '{routingSubscriptionId}' was not found in the current scope.", ProblemTypes.ResourceNotFound);

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
    /// <param name="routingSubscriptionId"></param>
    /// <param name="take">Max rows (passed to repository).</param>
    /// <param name="ct"></param>
    [HttpGet("{routingSubscriptionId:guid}/attempts")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDeliveryAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttempts(
        Guid routingSubscriptionId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        AlertRoutingSubscription? subscription = await subscriptionRepository.GetByIdAsync(routingSubscriptionId, ct);
        if (subscription is null)
            return this.NotFoundProblem($"Routing subscription '{routingSubscriptionId}' was not found.", ProblemTypes.ResourceNotFound);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return this.NotFoundProblem($"Routing subscription '{routingSubscriptionId}' was not found in the current scope.", ProblemTypes.ResourceNotFound);

        IReadOnlyList<AlertDeliveryAttempt> attempts = await attemptRepository.ListBySubscriptionAsync(routingSubscriptionId, Math.Clamp(take, 1, 200), ct);
        return Ok(attempts);
    }

    private static bool MatchesScope(AlertRoutingSubscription subscription, ScopeContext scope) =>
        subscription.TenantId == scope.TenantId &&
        subscription.WorkspaceId == scope.WorkspaceId &&
        subscription.ProjectId == scope.ProjectId;
}
