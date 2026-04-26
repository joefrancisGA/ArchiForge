using ArchLucid.Host.Core.Hosted;
using ArchLucid.Persistence.Orchestration;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>Shutdown and loop behavior tests for <see cref="AuthorityPipelineWorkHostedService" />.</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AuthorityPipelineWorkHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_exits_cleanly_when_stopped_during_delay()
    {
        Mock<IAuthorityPipelineWorkProcessor> processor = new();
        processor
            .Setup(p => p.ProcessPendingBatchAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AuthorityPipelineWorkHostedService sut = new(
            processor.Object,
            NullLogger<AuthorityPipelineWorkHostedService>.Instance,
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
        Mock<IAuthorityPipelineWorkProcessor> processor = new();
        processor
            .Setup(p => p.ProcessPendingBatchAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                callCount++;

                return callCount == 1 ? throw new InvalidOperationException("simulated failure") : Task.CompletedTask;
            });

        AuthorityPipelineWorkHostedService sut = new(
            processor.Object,
            NullLogger<AuthorityPipelineWorkHostedService>.Instance,
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
