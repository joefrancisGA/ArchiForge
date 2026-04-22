using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Core.Tests.Hosted;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TenantHealthScoringHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_when_scoring_disabled_does_not_call_repository()
    {
        Mock<ITenantCustomerSuccessRepository> repo = new(MockBehavior.Strict);
        ServiceCollection sc = [];
        sc.AddSingleton(repo.Object);
        ServiceProvider sp = sc.BuildServiceProvider();

        HostLeaderElectionCoordinator coordinator = CreateCoordinatorWithElectionDisabled();

        Mock<IOptionsMonitor<TenantHealthScoringOptions>> scoringOpts = new();
        scoringOpts.Setup(o => o.CurrentValue).Returns(new TenantHealthScoringOptions { Enabled = false });

        TenantHealthScoringHostedService sut = new(
            sp,
            coordinator,
            scoringOpts.Object,
            NullLogger<TenantHealthScoringHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await Task.Delay(300);

        await sut.StopAsync(CancellationToken.None);

        repo.Verify(
            r => r.RefreshAllTenantHealthScoresAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_when_scoring_enabled_invokes_refresh_once()
    {
        CancellationTokenSource hostCts = new();

        Mock<ITenantCustomerSuccessRepository> repo = new();
        repo.Setup(r => r.RefreshAllTenantHealthScoresAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => hostCts.Cancel());

        ServiceCollection sc = [];
        sc.AddSingleton(repo.Object);
        ServiceProvider sp = sc.BuildServiceProvider();

        HostLeaderElectionCoordinator coordinator = CreateCoordinatorWithElectionDisabled();

        Mock<IOptionsMonitor<TenantHealthScoringOptions>> scoringOpts = new();
        scoringOpts.Setup(o => o.CurrentValue).Returns(new TenantHealthScoringOptions { Enabled = true, IntervalHours = 24 });

        TenantHealthScoringHostedService sut = new(
            sp,
            coordinator,
            scoringOpts.Object,
            NullLogger<TenantHealthScoringHostedService>.Instance);

        await sut.StartAsync(hostCts.Token);

        await Task.Delay(500);

        await sut.StopAsync(CancellationToken.None);

        repo.Verify(
            r => r.RefreshAllTenantHealthScoresAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    private static HostLeaderElectionCoordinator CreateCoordinatorWithElectionDisabled()
    {
        Mock<IOptionsMonitor<HostLeaderElectionOptions>> hostOpts = new();
        hostOpts.Setup(o => o.CurrentValue).Returns(new HostLeaderElectionOptions { Enabled = false });

        Mock<IHostLeaderLeaseRepository> leases = new();
        HostInstanceIdentifier instance = HostInstanceIdentifier.ForTests("test-instance");

        return new HostLeaderElectionCoordinator(
            hostOpts.Object,
            leases.Object,
            instance,
            NullLogger<HostLeaderElectionCoordinator>.Instance);
    }
}
