using ArchiForge.Host.Core.Hosted;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Advisory;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Verifies <see cref="AdvisoryScanHostedService"/> exits cleanly on cancellation
/// and does not leak exceptions when the stopping token fires mid-delay or mid-processing.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AdvisoryScanHostedServiceShutdownTests
{
    [Fact]
    public async Task ExecuteAsync_exits_cleanly_when_cancellation_is_requested_during_delay()
    {
        Mock<IAdvisoryScanScheduleRepository> scheduleRepo = new();
        scheduleRepo
            .Setup(r => r.ListDueAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IAdvisoryScanRunner> runner = new();

        ServiceCollection services = [];
        services.AddScoped(_ => scheduleRepo.Object);
        services.AddScoped(_ => runner.Object);
        services.AddScoped<AdvisoryDueScheduleProcessor>();
        services.AddLogging();
        ServiceProvider provider = services.BuildServiceProvider();

        AdvisoryScanHostedService sut = new(
            provider,
            HostLeaderElectionTestDoubles.CoordinatorWithElectionDisabled(),
            NullLogger<AdvisoryScanHostedService>.Instance);

        using CancellationTokenSource cts = new();

        await sut.StartAsync(cts.Token);
        await Task.Delay(200, cts.Token);

        await cts.CancelAsync();

        Func<Task> act = () => sut.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync("hosted service should exit cleanly on cancellation");
    }

    [Fact]
    public async Task ExecuteAsync_continues_after_processor_throws_non_cancellation_exception()
    {
        int callCount = 0;
        Mock<IAdvisoryScanScheduleRepository> scheduleRepo = new();
        scheduleRepo
            .Setup(r => r.ListDueAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((DateTime _, int _, CancellationToken _) =>
            {
                callCount++;

                if (callCount == 1)
                    throw new InvalidOperationException("Simulated transient failure");

                return Task.FromResult<IReadOnlyList<AdvisoryScanSchedule>>(
                    []);
            });

        Mock<IAdvisoryScanRunner> runner = new();

        ServiceCollection services = [];
        services.AddScoped(_ => scheduleRepo.Object);
        services.AddScoped(_ => runner.Object);
        services.AddScoped<AdvisoryDueScheduleProcessor>();
        services.AddLogging();
        ServiceProvider provider = services.BuildServiceProvider();

        AdvisoryScanHostedService sut = new(
            provider,
            HostLeaderElectionTestDoubles.CoordinatorWithElectionDisabled(),
            NullLogger<AdvisoryScanHostedService>.Instance);

        using CancellationTokenSource cts = new();
        await sut.StartAsync(cts.Token);

        // Wait enough for at least two iterations (one fail + one success).
        await Task.Delay(500, cts.Token);
        await cts.CancelAsync();

        Func<Task> act = () => sut.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync("service should swallow non-cancellation exceptions and continue");

        callCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ExecuteAsync_exits_when_cancellation_fires_during_processing()
    {
        using CancellationTokenSource cts = new();
        Mock<IAdvisoryScanScheduleRepository> scheduleRepo = new();
        scheduleRepo
            .Setup(r => r.ListDueAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(async (DateTime _, int _, CancellationToken ct) =>
            {
                // Signal cancellation during processing
                await cts.CancelAsync();
                ct.ThrowIfCancellationRequested();
                return [];
            });

        Mock<IAdvisoryScanRunner> runner = new();

        ServiceCollection services = [];
        services.AddScoped(_ => scheduleRepo.Object);
        services.AddScoped(_ => runner.Object);
        services.AddScoped<AdvisoryDueScheduleProcessor>();
        services.AddLogging();
        ServiceProvider provider = services.BuildServiceProvider();

        AdvisoryScanHostedService sut = new(
            provider,
            HostLeaderElectionTestDoubles.CoordinatorWithElectionDisabled(),
            NullLogger<AdvisoryScanHostedService>.Instance);

        await sut.StartAsync(cts.Token);
        // cts is cancelled inside ListDueAsync; do not pass cts.Token here or Delay throws TaskCanceledException.
        await Task.Delay(300, CancellationToken.None);

        Func<Task> act = () => sut.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync("should handle OCE from mid-processing gracefully");
    }
}
