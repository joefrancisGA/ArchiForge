using ArchLucid.Host.Core.Health;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Persistence.Archival;

using FluentAssertions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="DataArchivalHostHealthCheck" /> mapping from <see cref="DataArchivalHostHealthState" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DataArchivalHostHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_archival_disabled_is_healthy()
    {
        DataArchivalHostHealthState state = new();
        Mock<IOptionsMonitor<DataArchivalOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new DataArchivalOptions { Enabled = false });
        DataArchivalHostHealthCheck sut = new(state, options.Object);

        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_archival_enabled_no_iteration_yet_is_healthy()
    {
        DataArchivalHostHealthState state = new();
        Mock<IOptionsMonitor<DataArchivalOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new DataArchivalOptions { Enabled = true });
        DataArchivalHostHealthCheck sut = new(state, options.Object);

        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_archival_enabled_last_failure_is_degraded()
    {
        DataArchivalHostHealthState state = new();
        state.MarkLastIterationFailed(new InvalidOperationException("archival failed"));
        Mock<IOptionsMonitor<DataArchivalOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new DataArchivalOptions { Enabled = true });
        DataArchivalHostHealthCheck sut = new(state, options.Object);

        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_archival_enabled_last_success_is_healthy()
    {
        DataArchivalHostHealthState state = new();
        state.MarkLastIterationSucceeded();
        Mock<IOptionsMonitor<DataArchivalOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new DataArchivalOptions { Enabled = true });
        DataArchivalHostHealthCheck sut = new(state, options.Object);

        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
