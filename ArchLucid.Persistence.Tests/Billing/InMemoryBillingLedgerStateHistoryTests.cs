using ArchLucid.Core.Billing;
using ArchLucid.Persistence.Billing;

namespace ArchLucid.Persistence.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class InMemoryBillingLedgerStateHistoryTests
{
    [Fact]
    public async Task Checkout_then_activate_produces_two_history_rows_newest_first()
    {
        InMemoryBillingLedger ledger = new();
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        await ledger.UpsertPendingCheckoutAsync(
            tenantId,
            workspaceId,
            projectId,
            "Stripe",
            "sess-1",
            "pro",
            5,
            1,
            CancellationToken.None);

        await ledger.ActivateSubscriptionAsync(
            tenantId,
            workspaceId,
            projectId,
            "Stripe",
            "sub-1",
            "pro",
            5,
            1,
            "{}",
            CancellationToken.None);

        IReadOnlyList<BillingSubscriptionStateHistoryEntry> history =
            await ledger.GetSubscriptionStateHistoryAsync(tenantId, 50, CancellationToken.None);

        history.Should().HaveCount(2);
        history[0].ChangeKind.Should().Be("Activate");
        history[0].NewStatus.Should().Be("Active");
        history[1].ChangeKind.Should().Be("UpsertPending");
        history[1].NewStatus.Should().Be("Pending");
    }
}
