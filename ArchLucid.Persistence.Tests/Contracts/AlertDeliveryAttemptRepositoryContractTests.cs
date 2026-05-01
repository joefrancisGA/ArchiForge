using ArchLucid.Decisioning.Alerts.Delivery;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IAlertDeliveryAttemptRepository" />.
/// </summary>
public abstract class AlertDeliveryAttemptRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IAlertDeliveryAttemptRepository CreateRepository();

    [SkippableFact]
    public async Task Create_then_ListByAlert_returns_row_newest_first()
    {
        SkipIfSqlServerUnavailable();
        IAlertDeliveryAttemptRepository repo = CreateRepository();
        Guid alertId = Guid.NewGuid();
        Guid subscriptionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        DateTime older = DateTime.UtcNow.AddMinutes(-5);
        DateTime newer = DateTime.UtcNow;

        AlertDeliveryAttempt first = NewAttempt(alertId, subscriptionId, tenantId, older);
        AlertDeliveryAttempt second = NewAttempt(alertId, subscriptionId, tenantId, newer);

        await repo.CreateAsync(first, CancellationToken.None);
        await repo.CreateAsync(second, CancellationToken.None);

        IReadOnlyList<AlertDeliveryAttempt> list = await repo.ListByAlertAsync(alertId, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].AlertDeliveryAttemptId.Should().Be(second.AlertDeliveryAttemptId);
        list[1].AlertDeliveryAttemptId.Should().Be(first.AlertDeliveryAttemptId);
    }

    [SkippableFact]
    public async Task Update_is_reflected_in_ListByAlert()
    {
        SkipIfSqlServerUnavailable();
        IAlertDeliveryAttemptRepository repo = CreateRepository();
        Guid alertId = Guid.NewGuid();
        Guid subscriptionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();

        AlertDeliveryAttempt attempt = NewAttempt(alertId, subscriptionId, tenantId, DateTime.UtcNow);
        await repo.CreateAsync(attempt, CancellationToken.None);

        attempt.Status = AlertDeliveryAttemptStatus.Succeeded;
        await repo.UpdateAsync(attempt, CancellationToken.None);

        IReadOnlyList<AlertDeliveryAttempt> list = await repo.ListByAlertAsync(alertId, CancellationToken.None);

        list.Should().ContainSingle();
        list[0].Status.Should().Be(AlertDeliveryAttemptStatus.Succeeded);
    }

    [SkippableFact]
    public async Task ListBySubscription_respects_take_and_orders_newest_first()
    {
        SkipIfSqlServerUnavailable();
        IAlertDeliveryAttemptRepository repo = CreateRepository();
        Guid alertId = Guid.NewGuid();
        Guid subscriptionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();

        AlertDeliveryAttempt a = NewAttempt(alertId, subscriptionId, tenantId, DateTime.UtcNow.AddMinutes(-2));
        AlertDeliveryAttempt b = NewAttempt(alertId, subscriptionId, tenantId, DateTime.UtcNow.AddMinutes(-1));
        AlertDeliveryAttempt c = NewAttempt(alertId, subscriptionId, tenantId, DateTime.UtcNow);

        await repo.CreateAsync(a, CancellationToken.None);
        await repo.CreateAsync(b, CancellationToken.None);
        await repo.CreateAsync(c, CancellationToken.None);

        IReadOnlyList<AlertDeliveryAttempt> list =
            await repo.ListBySubscriptionAsync(subscriptionId, 2, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].AlertDeliveryAttemptId.Should().Be(c.AlertDeliveryAttemptId);
        list[1].AlertDeliveryAttemptId.Should().Be(b.AlertDeliveryAttemptId);
    }

    private static AlertDeliveryAttempt NewAttempt(
        Guid alertId,
        Guid routingSubscriptionId,
        Guid tenantId,
        DateTime attemptedUtc)
    {
        return new AlertDeliveryAttempt
        {
            AlertDeliveryAttemptId = Guid.NewGuid(),
            AlertId = alertId,
            RoutingSubscriptionId = routingSubscriptionId,
            TenantId = tenantId,
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            AttemptedUtc = attemptedUtc,
            Status = AlertDeliveryAttemptStatus.Started,
            ChannelType = "test",
            Destination = "https://example.test/hook",
            RetryCount = 0
        };
    }
}
