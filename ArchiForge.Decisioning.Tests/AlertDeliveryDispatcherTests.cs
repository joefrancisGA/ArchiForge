using ArchiForge.Core.Audit;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Delivery;

using ArchiForge.Persistence.Alerts;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests;

[Trait("Category", "Unit")]
public sealed class AlertDeliveryDispatcherTests
{
    [Fact]
    public async Task DeliverAsync_WhenAlertIsNull_ThrowsArgumentNullException()
    {
        AlertDeliveryDispatcher sut = CreateSut(
            Mock.Of<IEnumerable<IAlertDeliveryChannel>>(),
            Mock.Of<IAlertRoutingSubscriptionRepository>(),
            Mock.Of<IAlertDeliveryAttemptRepository>(),
            Mock.Of<IAuditService>());

        Func<Task> act = async () => await sut.DeliverAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeliverAsync_WhenNoMatchingSubscriptions_DoesNotSend()
    {
        Mock<IAlertRoutingSubscriptionRepository> routing = new();
        routing
            .Setup(x => x.ListEnabledByScopeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IAlertDeliveryChannel> channel = new();
        channel.Setup(x => x.ChannelType).Returns(AlertRoutingChannelType.Email);

        AlertDeliveryDispatcher sut = CreateSut(
            [channel.Object],
            routing.Object,
            Mock.Of<IAlertDeliveryAttemptRepository>(),
            Mock.Of<IAuditService>());

        AlertRecord alert = CreateAlert(AlertSeverity.Critical);

        await sut.DeliverAsync(alert, CancellationToken.None);

        channel.Verify(
            x => x.SendAsync(It.IsAny<AlertDeliveryPayload>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeliverAsync_WhenChannelSucceeds_AuditsSuccessAndUpdatesSubscription()
    {
        AlertRoutingSubscription subscription = new()
        {
            RoutingSubscriptionId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ChannelType = AlertRoutingChannelType.Email,
            Destination = "oncall@example.com",
            MinimumSeverity = AlertSeverity.Info,
        };

        Mock<IAlertRoutingSubscriptionRepository> routing = new();
        routing
            .Setup(x => x.ListEnabledByScopeAsync(subscription.TenantId, subscription.WorkspaceId, subscription.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([subscription]);

        Mock<IAlertDeliveryAttemptRepository> attempts = new();
        attempts.Setup(x => x.CreateAsync(It.IsAny<AlertDeliveryAttempt>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        attempts.Setup(x => x.UpdateAsync(It.IsAny<AlertDeliveryAttempt>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        routing.Setup(x => x.UpdateAsync(It.IsAny<AlertRoutingSubscription>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IAlertDeliveryChannel> channel = new();
        channel.Setup(x => x.ChannelType).Returns(AlertRoutingChannelType.Email);
        channel.Setup(x => x.SendAsync(It.IsAny<AlertDeliveryPayload>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IAuditService> audit = new();
        audit.Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        AlertDeliveryDispatcher sut = CreateSut(
            [channel.Object],
            routing.Object,
            attempts.Object,
            audit.Object);

        AlertRecord alert = CreateAlert(AlertSeverity.Warning, subscription.TenantId, subscription.WorkspaceId, subscription.ProjectId);

        await sut.DeliverAsync(alert, CancellationToken.None);

        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.AlertDeliverySucceeded),
                It.IsAny<CancellationToken>()),
            Times.Once);
        routing.Verify(x => x.UpdateAsync(It.IsAny<AlertRoutingSubscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeliverAsync_WhenChannelFails_AuditsFailureWithoutThrowing()
    {
        AlertRoutingSubscription subscription = new()
        {
            RoutingSubscriptionId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ChannelType = AlertRoutingChannelType.Email,
            Destination = "bad",
            MinimumSeverity = AlertSeverity.Info,
        };

        Mock<IAlertRoutingSubscriptionRepository> routing = new();
        routing
            .Setup(x => x.ListEnabledByScopeAsync(subscription.TenantId, subscription.WorkspaceId, subscription.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([subscription]);

        Mock<IAlertDeliveryAttemptRepository> attempts = new();
        attempts.Setup(x => x.CreateAsync(It.IsAny<AlertDeliveryAttempt>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        attempts.Setup(x => x.UpdateAsync(It.IsAny<AlertDeliveryAttempt>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IAlertDeliveryChannel> channel = new();
        channel.Setup(x => x.ChannelType).Returns(AlertRoutingChannelType.Email);
        channel
            .Setup(x => x.SendAsync(It.IsAny<AlertDeliveryPayload>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("webhook failed"));

        Mock<IAuditService> audit = new();
        audit.Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        AlertDeliveryDispatcher sut = CreateSut(
            [channel.Object],
            routing.Object,
            attempts.Object,
            audit.Object);

        AlertRecord alert = CreateAlert(AlertSeverity.Warning, subscription.TenantId, subscription.WorkspaceId, subscription.ProjectId);

        await sut.Invoking(x => x.DeliverAsync(alert, CancellationToken.None)).Should().NotThrowAsync();

        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.AlertDeliveryFailed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AlertDeliveryDispatcher CreateSut(
        IEnumerable<IAlertDeliveryChannel> channels,
        IAlertRoutingSubscriptionRepository subscriptionRepository,
        IAlertDeliveryAttemptRepository attemptRepository,
        IAuditService auditService) =>
        new(channels, subscriptionRepository, attemptRepository, auditService);

    private static AlertRecord CreateAlert(
        string severity,
        Guid? tenantId = null,
        Guid? workspaceId = null,
        Guid? projectId = null) =>
        new()
        {
            AlertId = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            WorkspaceId = workspaceId ?? Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            Severity = severity,
            Title = "t",
        };
}
