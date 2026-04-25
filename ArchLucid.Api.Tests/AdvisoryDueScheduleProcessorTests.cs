using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Persistence;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     <see cref="AdvisoryDueScheduleProcessor" />: sequential runs, failure isolation, cancellation propagation.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AdvisoryDueScheduleProcessorTests
{
    [Fact]
    public async Task ProcessDueAsync_invokes_runner_for_each_due_schedule_in_order()
    {
        AdvisoryScanSchedule a = new() { ScheduleId = Guid.NewGuid() };
        AdvisoryScanSchedule b = new() { ScheduleId = Guid.NewGuid() };

        Mock<IAdvisoryScanScheduleRepository> schedules = new();
        schedules
            .Setup(x => x.ListDueAsync(It.IsAny<DateTime>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdvisoryScanSchedule> { a, b });

        List<Guid> order = [];
        Mock<IAdvisoryScanRunner> runner = new();
        runner
            .Setup(x => x.RunScheduleAsync(It.IsAny<AdvisoryScanSchedule>(), It.IsAny<CancellationToken>()))
            .Callback<AdvisoryScanSchedule, CancellationToken>((s, _) => order.Add(s.ScheduleId))
            .Returns(Task.CompletedTask);

        AdvisoryDueScheduleProcessor sut = new(
            schedules.Object,
            runner.Object,
            NullLogger<AdvisoryDueScheduleProcessor>.Instance);

        await sut.ProcessDueAsync(DateTime.UtcNow, 10, CancellationToken.None);

        order.Should().Equal(a.ScheduleId, b.ScheduleId);
        runner.Verify(x => x.RunScheduleAsync(a, It.IsAny<CancellationToken>()), Times.Once);
        runner.Verify(x => x.RunScheduleAsync(b, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessDueAsync_continues_second_schedule_when_first_runner_throws_non_cancel()
    {
        AdvisoryScanSchedule a = new() { ScheduleId = Guid.NewGuid() };
        AdvisoryScanSchedule b = new() { ScheduleId = Guid.NewGuid() };

        Mock<IAdvisoryScanScheduleRepository> schedules = new();
        schedules
            .Setup(x => x.ListDueAsync(It.IsAny<DateTime>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdvisoryScanSchedule> { a, b });

        Mock<IAdvisoryScanRunner> runner = new();
        runner
            .Setup(x => x.RunScheduleAsync(a, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        runner
            .Setup(x => x.RunScheduleAsync(b, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AdvisoryDueScheduleProcessor sut = new(
            schedules.Object,
            runner.Object,
            NullLogger<AdvisoryDueScheduleProcessor>.Instance);

        await sut.ProcessDueAsync(DateTime.UtcNow, 10, CancellationToken.None);

        runner.Verify(x => x.RunScheduleAsync(b, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessDueAsync_propagates_OperationCanceledException()
    {
        AdvisoryScanSchedule a = new() { ScheduleId = Guid.NewGuid() };

        Mock<IAdvisoryScanScheduleRepository> schedules = new();
        schedules
            .Setup(x => x.ListDueAsync(It.IsAny<DateTime>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdvisoryScanSchedule> { a });

        Mock<IAdvisoryScanRunner> runner = new();
        runner
            .Setup(x => x.RunScheduleAsync(a, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        AdvisoryDueScheduleProcessor sut = new(
            schedules.Object,
            runner.Object,
            NullLogger<AdvisoryDueScheduleProcessor>.Instance);

        Func<Task> act = async () => await sut.ProcessDueAsync(DateTime.UtcNow, 10, CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
