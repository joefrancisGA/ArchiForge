using ArchLucid.Host.Core.Hosted;
using ArchLucid.Persistence.Coordination.Retrieval;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Shutdown and loop behavior tests for <see cref="RetrievalIndexingOutboxHostedService"/>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RetrievalIndexingOutboxHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_exits_cleanly_when_stopped_during_delay()
    {
        Mock<IRetrievalIndexingOutboxProcessor> processor = new();
        processor
            .Setup(p => p.ProcessPendingBatchAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        RetrievalIndexingOutboxHostedService sut = new(
            processor.Object,
            NullLogger<RetrievalIndexingOutboxHostedService>.Instance,
            HostLeaderElectionTestDoubles.CoordinatorWithElectionDisabled());

        using CancellationTokenSource cts = new();
        await sut.StartAsync(cts.Token);
        await Task.Delay(150, CancellationToken.None);
        await cts.CancelAsync();

        Func<Task> act = () => sut.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_continues_after_processor_throws()
    {
        int callCount = 0;
        Mock<IRetrievalIndexingOutboxProcessor> processor = new();
        processor
            .Setup(p => p.ProcessPendingBatchAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                callCount++;

                if (callCount == 1)
                
                    throw new InvalidOperationException("simulated failure");
                

                return Task.CompletedTask;
            });

        RetrievalIndexingOutboxHostedService sut = new(
            processor.Object,
            NullLogger<RetrievalIndexingOutboxHostedService>.Instance,
            HostLeaderElectionTestDoubles.CoordinatorWithElectionDisabled());

        using CancellationTokenSource cts = new();
        await sut.StartAsync(cts.Token);
        await Task.Delay(5500, CancellationToken.None);
        await cts.CancelAsync();

        Func<Task> act = () => sut.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();

        callCount.Should().BeGreaterThanOrEqualTo(2);
    }
}
