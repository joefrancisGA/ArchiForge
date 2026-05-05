using ArchLucid.Application.DataConsistency;
using ArchLucid.Core.Hosting;
using ArchLucid.Core.Integration;
using ArchLucid.Persistence;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.DataConsistency;

public sealed class DataConsistencyReconciliationHostedServiceTests
{
    [SkippableFact]
    public async Task Loop_invokes_reconciliation_and_records_success()
    {
        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(
                IntegrationEventTypes.DataConsistencyCheckCompletedV1,
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationEventsOptions = new();
        integrationEventsOptions
            .Setup(m => m.CurrentValue)
            .Returns(new IntegrationEventsOptions { TransactionalOutboxEnabled = false });

        Mock<ILeaderElectionWorkRunner> runner = new();
        runner
            .Setup(r => r.RunLeaderWorkAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task>, CancellationToken>(static (_, work, ct) => work(ct));

        DataConsistencyReport expected = new(
            DateTime.UtcNow,
            [],
            IsHealthy: true);

        ServiceCollection sc = [];
        sc.AddSingleton<DataConsistencyReconciliationHealthState>();
        sc.AddScoped<IDataConsistencyReconciliationService>(_ => new StubReconciliationService(expected));
        sc.AddScoped(_ => Mock.Of<IIntegrationEventOutboxRepository>());
        sc.AddSingleton(publisher.Object);
        await using ServiceProvider sp = sc.BuildServiceProvider(true);

        IOptionsMonitor<DataConsistencyReconciliationOptions> opts =
            Mock.Of<IOptionsMonitor<DataConsistencyReconciliationOptions>>(m =>
                m.CurrentValue == new DataConsistencyReconciliationOptions { InitialDelaySeconds = 0, ReconciliationIntervalMinutes = 15 });

        DataConsistencyReconciliationHostedService sut = new(
            sp.GetRequiredService<IServiceScopeFactory>(),
            opts,
            runner.Object,
            sp.GetRequiredService<DataConsistencyReconciliationHealthState>(),
            publisher.Object,
            integrationEventsOptions.Object,
            NullLogger<DataConsistencyReconciliationHostedService>.Instance);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);

        await Task.Delay(750, CancellationToken.None);

        DataConsistencyReconciliationHealthState health = sp.GetRequiredService<DataConsistencyReconciliationHealthState>();
        health.TrySnapshot(out bool hasRun, out DataConsistencyReport? report, out string? error);
        hasRun.Should().BeTrue();
        error.Should().BeNull();
        report.Should().NotBeNull();
        report.IsHealthy.Should().BeTrue();

        publisher.Verify(
            p => p.PublishAsync(
                IntegrationEventTypes.DataConsistencyCheckCompletedV1,
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        await sut.StopAsync(CancellationToken.None);
    }

    [SkippableFact]
    public async Task Loop_records_failure_when_reconciliation_throws()
    {
        Mock<IIntegrationEventPublisher> publisher = new();
        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationEventsOptions = new();
        integrationEventsOptions.Setup(m => m.CurrentValue).Returns(new IntegrationEventsOptions());

        Mock<ILeaderElectionWorkRunner> runner = new();
        runner
            .Setup(r => r.RunLeaderWorkAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task>, CancellationToken>(static (_, work, ct) => work(ct));

        ServiceCollection sc = [];
        sc.AddSingleton<DataConsistencyReconciliationHealthState>();
        sc.AddScoped<IDataConsistencyReconciliationService>(_ => new ThrowingReconciliationService());
        sc.AddScoped(_ => Mock.Of<IIntegrationEventOutboxRepository>());
        sc.AddSingleton(publisher.Object);
        await using ServiceProvider sp = sc.BuildServiceProvider(true);

        IOptionsMonitor<DataConsistencyReconciliationOptions> opts =
            Mock.Of<IOptionsMonitor<DataConsistencyReconciliationOptions>>(m =>
                m.CurrentValue == new DataConsistencyReconciliationOptions { InitialDelaySeconds = 0, ReconciliationIntervalMinutes = 15 });

        DataConsistencyReconciliationHostedService sut = new(
            sp.GetRequiredService<IServiceScopeFactory>(),
            opts,
            runner.Object,
            sp.GetRequiredService<DataConsistencyReconciliationHealthState>(),
            publisher.Object,
            integrationEventsOptions.Object,
            NullLogger<DataConsistencyReconciliationHostedService>.Instance);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);

        await Task.Delay(750, CancellationToken.None);

        DataConsistencyReconciliationHealthState health = sp.GetRequiredService<DataConsistencyReconciliationHealthState>();
        health.TrySnapshot(out _, out DataConsistencyReport? report, out string? error);
        report.Should().BeNull();
        error.Should().Contain("boom");

        await sut.StopAsync(CancellationToken.None);
    }

    private sealed class StubReconciliationService(DataConsistencyReport report) : IDataConsistencyReconciliationService
    {
        public Task<DataConsistencyReport> RunReconciliationAsync(CancellationToken cancellationToken) =>
            Task.FromResult(report);
    }

    private sealed class ThrowingReconciliationService : IDataConsistencyReconciliationService
    {
        public Task<DataConsistencyReport> RunReconciliationAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("boom");
    }
}

public sealed class DataConsistencyHealthCheckTests
{
    [SkippableFact]
    public async Task Unhealthy_when_last_report_has_critical()
    {
        DataConsistencyReconciliationHealthState state = new();
        state.RecordSuccess(
            new DataConsistencyReport(
                DateTime.UtcNow,
                [new DataConsistencyFinding("x", DataConsistencyFindingSeverity.Critical, "c", [])],
                IsHealthy: false));

        DataConsistencyHealthCheck sut = new(state);

        HealthCheckResult r = await sut.CheckHealthAsync(new HealthCheckContext());

        r.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [SkippableFact]
    public async Task Unhealthy_when_never_run()
    {
        DataConsistencyReconciliationHealthState state = new();
        DataConsistencyHealthCheck sut = new(state);

        HealthCheckResult r = await sut.CheckHealthAsync(new HealthCheckContext());

        r.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [SkippableFact]
    public async Task Degraded_when_last_report_has_warning()
    {
        DataConsistencyReconciliationHealthState state = new();
        state.RecordSuccess(
            new DataConsistencyReport(
                DateTime.UtcNow,
                [new DataConsistencyFinding("x", DataConsistencyFindingSeverity.Warning, "w", [])],
                IsHealthy: false));

        DataConsistencyHealthCheck sut = new(state);

        HealthCheckResult r = await sut.CheckHealthAsync(new HealthCheckContext());

        r.Status.Should().Be(HealthStatus.Degraded);
    }

    [SkippableFact]
    public async Task Healthy_when_only_info_findings()
    {
        DataConsistencyReconciliationHealthState state = new();
        state.RecordSuccess(
            new DataConsistencyReport(
                DateTime.UtcNow,
                [new DataConsistencyFinding("skip", DataConsistencyFindingSeverity.Info, "ok", [])],
                IsHealthy: true));

        DataConsistencyHealthCheck sut = new(state);

        HealthCheckResult r = await sut.CheckHealthAsync(new HealthCheckContext());

        r.Status.Should().Be(HealthStatus.Healthy);
    }
}
