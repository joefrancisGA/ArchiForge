using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Hosted;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class HostLeaderElectionCoordinatorTests
{
    [Fact]
    public async Task RunLeaderWorkAsync_when_disabled_does_not_call_lease_repository()
    {
        Mock<IHostLeaderLeaseRepository> lease = new();
        Mock<IOptionsMonitor<HostLeaderElectionOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new HostLeaderElectionOptions { Enabled = false });

        HostLeaderElectionCoordinator sut = new(
            options.Object,
            lease.Object,
            HostInstanceIdentifier.ForTests("instance-a"),
            NullLogger<HostLeaderElectionCoordinator>.Instance);

        using CancellationTokenSource app = new();
        app.CancelAfter(150);

        await sut.RunLeaderWorkAsync(
            "test-lease",
            async ct =>
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(30, ct);
                }
            },
            app.Token);

        lease.Verify(
            l => l.TryAcquireOrRenewAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunLeaderWorkAsync_when_renewal_fails_cancels_leader_work()
    {
        int callSequence = 0;
        Mock<IHostLeaderLeaseRepository> lease = new();
        lease
            .Setup(l => l.TryAcquireOrRenewAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callSequence++;

                // Initial acquire, then one successful renew, then renewal failure (lost lease).
                return callSequence <= 2;
            });

        Mock<IOptionsMonitor<HostLeaderElectionOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new HostLeaderElectionOptions
        {
            Enabled = true,
            LeaseDurationSeconds = 60,
            RenewIntervalSeconds = 1,
            FollowerPollMilliseconds = 50
        });

        HostLeaderElectionCoordinator sut = new(
            options.Object,
            lease.Object,
            HostInstanceIdentifier.ForTests("leader-a"),
            NullLogger<HostLeaderElectionCoordinator>.Instance);

        int loopTicks = 0;
        using CancellationTokenSource app = new(TimeSpan.FromSeconds(12));

        await sut.RunLeaderWorkAsync(
            "test-lease",
            async ct =>
            {
                while (!ct.IsCancellationRequested)
                {
                    loopTicks++;
                    await Task.Delay(50, ct);
                }
            },
            app.Token);

        loopTicks.Should().BeGreaterThan(0);
        callSequence.Should().BeGreaterThanOrEqualTo(3);
    }
}
