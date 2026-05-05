using System.Text.Json;

using ArchLucid.Api.Attributes;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Api.Services;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Decisioning.Alerts.Delivery;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Integrations;

/// <summary>
///     Tests connectivity for persisted alert-routing webhook subscriptions.
///     Uses the same synthetic CloudEvents ping payload as <c>POST /v1/webhooks/dry-run</c> but resolves the destination
///     from the stored subscription, enforcing tenant RLS before firing.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/integrations/webhooks")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class WebhookConnectionsController(
    IScopeContextProvider scopeProvider,
    IAlertRoutingSubscriptionRepository subscriptionRepository,
    IOutboundWebhookDryRunService probe,
    IAuditService auditService) : ControllerBase
{
    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IOutboundWebhookDryRunService _probe =
        probe ?? throw new ArgumentNullException(nameof(probe));

    private readonly IAlertRoutingSubscriptionRepository _subscriptionRepository =
        subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    /// <summary>
    ///     Dispatches a synthetic ping event to the destination URL of the named webhook subscription and returns the
    ///     HTTP outcome. Only webhook channel types are supported (Email subscriptions are rejected with 400).
    /// </summary>
    [HttpPost("{routingSubscriptionId:guid}/test")]
    [ProducesResponseType(typeof(OutboundWebhookDryRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestAsync(
        Guid routingSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        AlertRoutingSubscription? subscription =
            await _subscriptionRepository.GetByIdAsync(routingSubscriptionId, cancellationToken);

        if (subscription is null)
            return this.NotFoundProblem(
                $"Routing subscription '{routingSubscriptionId}' was not found.",
                ProblemTypes.ResourceNotFound);

        ScopeContext scope = _scopeProvider.GetCurrentScope();

        if (!MatchesScope(subscription, scope))
            return this.NotFoundProblem(
                $"Routing subscription '{routingSubscriptionId}' was not found in the current scope.",
                ProblemTypes.ResourceNotFound);

        if (!IsWebhookChannelType(subscription.ChannelType))
            return this.BadRequestProblem(
                $"Subscription '{routingSubscriptionId}' uses channel type '{subscription.ChannelType}' which does not support a ping test. Only webhook channel types are supported.",
                ProblemTypes.ValidationFailed);

        if (!Uri.TryCreate(subscription.Destination, UriKind.Absolute, out Uri? destinationUri))
            return this.BadRequestProblem(
                $"Subscription '{routingSubscriptionId}' destination is not a valid absolute URI.",
                ProblemTypes.ValidationFailed);

        OutboundWebhookDryRunResult outcome = await _probe.ProbeAsync(destinationUri, null, cancellationToken);

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.AlertRoutingWebhookPingExecuted,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                DataJson = JsonSerializer.Serialize(new
                {
                    routingSubscriptionId,
                    channelType = subscription.ChannelType,
                    transportSucceeded = outcome.TransportSucceeded,
                    statusCode = outcome.StatusCode,
                    reasonPhrase = outcome.ReasonPhrase,
                    error = outcome.Error
                })
            },
            cancellationToken);

        OutboundWebhookDryRunResponse response = new()
        {
            TransportSucceeded = outcome.TransportSucceeded,
            StatusCode = outcome.StatusCode,
            ReasonPhrase = outcome.ReasonPhrase,
            ResponseBodyPreview = outcome.ResponseBodyPreview,
            ResponseBodyTruncated = outcome.ResponseBodyTruncated,
            Error = outcome.Error
        };

        return Ok(response);
    }

    private static bool IsWebhookChannelType(string channelType)
    {
        return channelType is AlertRoutingChannelType.TeamsWebhook
            or AlertRoutingChannelType.SlackWebhook
            or AlertRoutingChannelType.OnCallWebhook;
    }

    private static bool MatchesScope(AlertRoutingSubscription subscription, ScopeContext scope)
    {
        return subscription.TenantId == scope.TenantId &&
               subscription.WorkspaceId == scope.WorkspaceId &&
               subscription.ProjectId == scope.ProjectId;
    }
}
