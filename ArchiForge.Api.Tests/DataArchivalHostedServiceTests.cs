using ArchiForge.Api.Hosted;
using ArchiForge.Persistence.Archival;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Shutdown and loop behavior for <see cref="DataArchivalHostedService"/>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DataArchivalHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_exits_cleanly_when_cancellation_during_delay_after_iteration()
    {
        Mock<IOptionsMonitor<DataArchivalOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(
            new DataArchivalOptions { Enabled = false, IntervalHours = 1 });

        ServiceProvider root = new ServiceCollection().BuildServiceProvider();
        DataArchivalHostedService sut = new(
            root.GetRequiredService<IServiceScopeFactory>(),
            options.Object,
            NullLogger<DataArchivalHostedService>.Instance,
            new DataArchivalHostHealthState());

        using CancellationTokenSource cts = new();
        await sut.StartAsync(cts.Token);
        await Task.Delay(80, CancellationToken.None);
        await cts.CancelAsync();

        Func<Task> act = () => sut.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_marks_health_failed_when_coordinator_throws()
    {
        Mock<IDataArchivalCoordinator> coordinator = new();
        coordinator
            .Setup(c => c.RunOnceAsync(It.IsAny<DataArchivalOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("archival failed"));

        ServiceCollection services = new();
        services.AddScoped(_ => coordinator.Object);
        ServiceProvider provider = services.BuildServiceProvider();

        Mock<IServiceScopeFactory> scopeFactory = new();
        scopeFactory.Setup(f => f.CreateScope()).Returns(() => provider.CreateScope());

        Mock<IOptionsMonitor<DataArchivalOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(
            new DataArchivalOptions { Enabled = true, IntervalHours = 1 });

        DataArchivalHostHealthState health = new();
        DataArchivalHostedService sut = new(
            scopeFactory.Object,
            options.Object,
            NullLogger<DataArchivalHostedService>.Instance,
            health);

        using CancellationTokenSource cts = new();
        await sut.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        await cts.CancelAsync();
        await sut.StopAsync(CancellationToken.None);

        health.HasAttempted.Should().BeTrue();
    }
}
