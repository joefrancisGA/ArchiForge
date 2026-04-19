using ArchLucid.Application.Billing;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Integration;

using ArchLucid.Persistence;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Billing;

public sealed class MarketplaceWebhookIntegrationEventPublisherTests
{
    [Fact]
    public async Task TryPublishAsync_uses_direct_publish_when_transactional_outbox_disabled()
    {
        Mock<IIntegrationEventOutboxRepository> outbox = new();
        Mock<IIntegrationEventPublisher> publisher = new();
        IntegrationEventsOptions options = new() { TransactionalOutboxEnabled = false };
        ILogger logger = NullLoggerFactory.Instance.CreateLogger(nameof(MarketplaceWebhookIntegrationEventPublisherTests));

        string? capturedEventType = null;

        publisher
            .Setup(p =>
                p.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
            .Callback(
                (string eventType, ReadOnlyMemory<byte> _, string? _, CancellationToken _) =>
                    capturedEventType = eventType)
            .Returns(Task.CompletedTask);

        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid projectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        MarketplaceWebhookReceivedIntegrationPayload payload = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            ProviderDedupeKey = "k1",
            Action = "Subscribe",
            SubscriptionId = "sub-1",
            BillingProvider = "AzureMarketplace",
        };

        await MarketplaceWebhookIntegrationEventPublisher.TryPublishAsync(
            outbox.Object,
            publisher.Object,
            options,
            logger,
            payload,
            CancellationToken.None);

        capturedEventType.Should().Be(IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1);

        outbox.Verify(
            o =>
                o.EnqueueAsync(
                    It.IsAny<Guid?>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
