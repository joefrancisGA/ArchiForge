using ArchLucid.Core.Billing;
using ArchLucid.Persistence.Billing;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Billing;

public sealed class NoopBillingProviderTests
{
    [Fact]
    public void Ctor_null_ledger_throws()
    {
        Action act = () => _ = new NoopBillingProvider(null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("ledger");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_writes_pending_row_to_ledger()
    {
        InMemoryBillingLedger ledger = new();
        NoopBillingProvider sut = new(ledger);
        Guid tenantId = Guid.NewGuid();
        BillingCheckoutRequest request = new()
        {
            TenantId = tenantId,
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            TargetTier = BillingCheckoutTier.Team,
            Seats = 2,
            Workspaces = 1,
            ReturnUrl = "https://r",
            CancelUrl = "https://c"
        };

        BillingCheckoutResult result = await sut.CreateCheckoutSessionAsync(request, CancellationToken.None);

        result.CheckoutUrl.Should().Contain("noop-checkout");
        result.ProviderSessionId.Should().StartWith("noop_sess_");
        (await ledger.TenantHasActiveSubscriptionAsync(tenantId, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task HandleWebhookAsync_rejects()
    {
        InMemoryBillingLedger ledger = new();
        NoopBillingProvider sut = new(ledger);
        BillingWebhookInbound inbound = new()
        {
            RawBody = "{}"
        };

        BillingWebhookHandleResult r = await sut.HandleWebhookAsync(inbound, CancellationToken.None);

        r.Succeeded.Should().BeFalse();
        r.ErrorDetail.Should().Contain("Noop");
    }
}
