using ArchLucid.Core.Billing;
using ArchLucid.Core.Integration;

using ArchLucid.Persistence;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Billing;

/// <summary>Publishes <see cref="IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1"/> for downstream Azure Logic Apps / partner automation.</summary>
public static class MarketplaceWebhookIntegrationEventPublisher
{
    public static Task TryPublishAsync(
        IIntegrationEventOutboxRepository outbox,
        IIntegrationEventPublisher publisher,
        IntegrationEventsOptions options,
        ILogger logger,
        MarketplaceWebhookReceivedIntegrationPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(payload);

        object body = new
        {
            schemaVersion = 1,
            tenantId = payload.TenantId,
            workspaceId = payload.WorkspaceId,
            projectId = payload.ProjectId,
            providerDedupeKey = payload.ProviderDedupeKey,
            action = payload.Action,
            subscriptionId = payload.SubscriptionId,
            billingProvider = payload.BillingProvider,
        };

        string messageId = $"{payload.ProviderDedupeKey}:{IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1}";

        return OutboxAwareIntegrationEventPublishing.TryPublishOrEnqueueAsync(
            outbox,
            publisher,
            options,
            logger,
            IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1,
            body,
            messageId,
            runId: null,
            payload.TenantId,
            payload.WorkspaceId,
            payload.ProjectId,
            connection: null,
            transaction: null,
            cancellationToken);
    }
}
