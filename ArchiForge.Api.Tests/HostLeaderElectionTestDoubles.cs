using ArchiForge.Data.Repositories;
using ArchiForge.Host.Core.Configuration;
using ArchiForge.Host.Core.Hosted;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Builds <see cref="HostLeaderElectionCoordinator"/> with leader election disabled for hosted-service unit tests.
/// </summary>
internal static class HostLeaderElectionTestDoubles
{
    public static HostLeaderElectionCoordinator CoordinatorWithElectionDisabled(
        ILogger<HostLeaderElectionCoordinator>? logger = null)
    {
        Mock<IOptionsMonitor<HostLeaderElectionOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new HostLeaderElectionOptions { Enabled = false });

        return new HostLeaderElectionCoordinator(
            options.Object,
            new NoOpHostLeaderLeaseRepository(),
            HostInstanceIdentifier.ForTests("unit-test-host"),
            logger ?? NullLogger<HostLeaderElectionCoordinator>.Instance);
    }
}
