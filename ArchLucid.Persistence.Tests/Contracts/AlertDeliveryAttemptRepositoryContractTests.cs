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

    /// <summary>
    ///     SQL-backed implementations must insert <c>AlertRules</c>, <c>AlertRecords</c>, and
    ///     <c>AlertRoutingSubscriptions</c> so <c>dbo.AlertDeliveryAttempts</c> FKs succeed.
    /// </summary>
    protected virtual Task EnsureDeliveryAttemptParentsExistAsync(
        Guid alertId,
        Guid routingSubscriptionId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    protected abstract IAlertDeliveryAttemptRepository CreateRepository(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId);

    [SkippableFact]
    public async Task Create_then_ListByAlert_returns_row_newest_first()
    {
        SkipIfSqlServerUnavailable();
        Guid alertId = Guid.NewGuid();
        Guid subscriptionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        DateTime newer = DateTime.UtcNow;
        DateTime older = newer.AddMinutes(-5);

        await EnsureDeliveryAttemptParentsExistAsync(
            alertId,
            subscriptionId,
            tenantId,
            workspaceId,
            projectId,
            CancellationToken.None);

        IAlertDeliveryAttemptRepository repo = CreateRepository(tenantId, workspaceId, projectId);

        AlertDeliveryAttempt first = NewAttempt(alertId, subscriptionId, tenantId, workspaceId, projectId, older);
        AlertDeliveryAttempt second = NewAttempt(alertId, subscriptionId, tenantId, workspaceId, projectId, newer);

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
        Guid alertId = Guid.NewGuid();
        Guid subscriptionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        await EnsureDeliveryAttemptParentsExistAsync(
            alertId,
            subscriptionId,
            tenantId,
            workspaceId,
            projectId,
            CancellationToken.None);

        IAlertDeliveryAttemptRepository repo = CreateRepository(tenantId, workspaceId, projectId);

        AlertDeliveryAttempt attempt =
            NewAttempt(alertId, subscriptionId, tenantId, workspaceId, projectId, DateTime.UtcNow);
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
        Guid alertId = Guid.NewGuid();
        Guid subscriptionId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        await EnsureDeliveryAttemptParentsExistAsync(
            alertId,
            subscriptionId,
            tenantId,
            workspaceId,
            projectId,
            CancellationToken.None);

        IAlertDeliveryAttemptRepository repo = CreateRepository(tenantId, workspaceId, projectId);

        DateTime newestUtc = DateTime.UtcNow;
        AlertDeliveryAttempt c =
            NewAttempt(alertId, subscriptionId, tenantId, workspaceId, projectId, newestUtc);
        AlertDeliveryAttempt b =
            NewAttempt(alertId, subscriptionId, tenantId, workspaceId, projectId, newestUtc.AddMinutes(-1));
        AlertDeliveryAttempt a =
            NewAttempt(alertId, subscriptionId, tenantId, workspaceId, projectId, newestUtc.AddMinutes(-2));

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
        Guid workspaceId,
        Guid projectId,
        DateTime attemptedUtc)
    {
        return new AlertDeliveryAttempt
        {
            AlertDeliveryAttemptId = Guid.NewGuid(),
            AlertId = alertId,
            RoutingSubscriptionId = routingSubscriptionId,
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            AttemptedUtc = attemptedUtc,
            Status = AlertDeliveryAttemptStatus.Started,
            ChannelType = "test",
            Destination = "https://example.test/hook",
            RetryCount = 0
        };
    }
}
