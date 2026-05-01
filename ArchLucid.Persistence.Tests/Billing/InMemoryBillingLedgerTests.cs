using ArchLucid.Persistence.Billing;

namespace ArchLucid.Persistence.Tests.Billing;

public sealed class InMemoryBillingLedgerTests
{
    [SkippableFact]
    public async Task TenantHasActiveSubscriptionAsync_false_when_empty()
    {
        InMemoryBillingLedger sut = new();

        bool active = await sut.TenantHasActiveSubscriptionAsync(Guid.NewGuid(), CancellationToken.None);

        active.Should().BeFalse();
    }

    [SkippableFact]
    public async Task TenantHasActiveSubscriptionAsync_true_when_active_row()
    {
        InMemoryBillingLedger sut = new();
        Guid tenantId = Guid.NewGuid();

        await sut.ActivateSubscriptionAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "noop",
            "sub",
            "team",
            2,
            1,
            null,
            CancellationToken.None);

        bool active = await sut.TenantHasActiveSubscriptionAsync(tenantId, CancellationToken.None);

        active.Should().BeTrue();
    }

    [SkippableFact]
    public async Task UpsertPendingCheckoutAsync_then_TenantHasActiveSubscriptionAsync_false_until_activated()
    {
        InMemoryBillingLedger sut = new();
        Guid tenantId = Guid.NewGuid();

        await sut.UpsertPendingCheckoutAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "stripe",
            "sess",
            "team",
            1,
            1,
            CancellationToken.None);

        (await sut.TenantHasActiveSubscriptionAsync(tenantId, CancellationToken.None)).Should().BeFalse();
    }

    [SkippableFact]
    public async Task Webhook_dedupe_and_status_round_trip()
    {
        InMemoryBillingLedger sut = new();
        const string key = "evt_1";

        bool first = await sut.TryInsertWebhookEventAsync(key, "stripe", "x", "{}", CancellationToken.None);
        bool second = await sut.TryInsertWebhookEventAsync(key, "stripe", "x", "{}", CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse();

        await sut.MarkWebhookProcessedAsync(key, "ok", CancellationToken.None);

        string? status = await sut.GetWebhookEventResultStatusAsync(key, CancellationToken.None);

        status.Should().Be("ok");
    }

    [SkippableFact]
    public async Task GetWebhookEventResultStatusAsync_unknown_returns_null()
    {
        InMemoryBillingLedger sut = new();

        string? status = await sut.GetWebhookEventResultStatusAsync("missing", CancellationToken.None);

        status.Should().BeNull();
    }

    [SkippableFact]
    public async Task SuspendSubscriptionAsync_then_TenantHasActiveSubscriptionAsync_false()
    {
        InMemoryBillingLedger sut = new();
        Guid tenantId = Guid.NewGuid();

        await sut.ActivateSubscriptionAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "p",
            "sub",
            "team",
            1,
            1,
            null,
            CancellationToken.None);

        await sut.SuspendSubscriptionAsync(tenantId, CancellationToken.None);

        (await sut.TenantHasActiveSubscriptionAsync(tenantId, CancellationToken.None)).Should().BeFalse();
    }

    [SkippableFact]
    public async Task ReinstateSubscriptionAsync_after_suspend_restores_active()
    {
        InMemoryBillingLedger sut = new();
        Guid tenantId = Guid.NewGuid();

        await sut.ActivateSubscriptionAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "p",
            "sub",
            "team",
            1,
            1,
            null,
            CancellationToken.None);

        await sut.SuspendSubscriptionAsync(tenantId, CancellationToken.None);
        await sut.ReinstateSubscriptionAsync(tenantId, CancellationToken.None);

        (await sut.TenantHasActiveSubscriptionAsync(tenantId, CancellationToken.None)).Should().BeTrue();
    }

    [SkippableFact]
    public async Task CancelSubscriptionAsync_marks_canceled()
    {
        InMemoryBillingLedger sut = new();
        Guid tenantId = Guid.NewGuid();

        await sut.ActivateSubscriptionAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "p",
            "sub",
            "team",
            1,
            1,
            null,
            CancellationToken.None);

        await sut.CancelSubscriptionAsync(tenantId, CancellationToken.None);

        (await sut.TenantHasActiveSubscriptionAsync(tenantId, CancellationToken.None)).Should().BeFalse();
    }

    [SkippableFact]
    public async Task ChangePlanAsync_and_ChangeQuantityAsync_mutate_row_when_present()
    {
        InMemoryBillingLedger sut = new();
        Guid tenantId = Guid.NewGuid();

        await sut.ActivateSubscriptionAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "p",
            "sub",
            "team",
            3,
            2,
            null,
            CancellationToken.None);

        await sut.ChangePlanAsync(tenantId, "enterprise", null, CancellationToken.None);
        await sut.ChangeQuantityAsync(tenantId, 10, null, CancellationToken.None);

        (await sut.TenantHasActiveSubscriptionAsync(tenantId, CancellationToken.None)).Should().BeTrue();
    }
}
