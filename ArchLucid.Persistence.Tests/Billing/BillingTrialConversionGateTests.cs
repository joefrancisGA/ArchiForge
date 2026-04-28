using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Billing;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Persistence.Tests.Billing;

public sealed class BillingTrialConversionGateTests
{
    [Fact]
    public void Ctor_null_options_throws()
    {
        InMemoryBillingLedger ledger = new();

        Action act = () => _ = new BillingTrialConversionGate(null!, ledger);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("options");
    }

    [Fact]
    public void Ctor_null_ledger_throws()
    {
        Mock<IOptionsMonitor<BillingOptions>> opt = new();

        Action act = () => _ = new BillingTrialConversionGate(opt.Object, null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("ledger");
    }

    [Fact]
    public async Task EnsureManualConversionAllowedAsync_noop_provider_does_not_throw()
    {
        Mock<IOptionsMonitor<BillingOptions>> opt = new();
        opt.Setup(o => o.CurrentValue).Returns(new BillingOptions { Provider = "  noop  " });
        BillingTrialConversionGate sut = new(opt.Object, new InMemoryBillingLedger());

        Func<Task> act = async () =>
            await sut.EnsureManualConversionAllowedAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureManualConversionAllowedAsync_whitespace_provider_does_not_throw()
    {
        Mock<IOptionsMonitor<BillingOptions>> opt = new();
        opt.Setup(o => o.CurrentValue).Returns(new BillingOptions { Provider = "   " });
        BillingTrialConversionGate sut = new(opt.Object, new InMemoryBillingLedger());

        Func<Task> act = async () =>
            await sut.EnsureManualConversionAllowedAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureManualConversionAllowedAsync_paid_provider_with_active_subscription_does_not_throw()
    {
        Mock<IOptionsMonitor<BillingOptions>> opt = new();
        opt.Setup(o => o.CurrentValue).Returns(new BillingOptions { Provider = "Stripe" });
        InMemoryBillingLedger ledger = new();
        Guid tenantId = Guid.NewGuid();
        await ledger.ActivateSubscriptionAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "stripe",
            "sub",
            "team",
            1,
            1,
            null,
            CancellationToken.None);

        BillingTrialConversionGate sut = new(opt.Object, ledger);

        Func<Task> act = async () => await sut.EnsureManualConversionAllowedAsync(tenantId, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureManualConversionAllowedAsync_paid_provider_without_subscription_throws()
    {
        Mock<IOptionsMonitor<BillingOptions>> opt = new();
        opt.Setup(o => o.CurrentValue).Returns(new BillingOptions { Provider = "Stripe" });
        BillingTrialConversionGate sut = new(opt.Object, new InMemoryBillingLedger());
        Guid tenantId = Guid.NewGuid();

        Func<Task> act = async () => await sut.EnsureManualConversionAllowedAsync(tenantId, CancellationToken.None);

        await act.Should().ThrowAsync<BillingConversionBlockedException>();
    }
}
