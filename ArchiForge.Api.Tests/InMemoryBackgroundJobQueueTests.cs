using ArchiForge.Api.Jobs;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class InMemoryBackgroundJobQueueTests
{
    private static BackgroundJobFile OkFile() => new("out.bin", "application/octet-stream", []);

    [Fact]
    public async Task Enqueue_WhenPendingChannelIsFull_ThrowsInvalidOperationException()
    {
        TaskCompletionSource<bool> barrier = new();
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger = new();
        InMemoryBackgroundJobQueue queue = new(logger.Object);
        await queue.StartAsync(CancellationToken.None);

        _ = queue.Enqueue("block", "plain", async ct =>
        {
            await barrier.Task.WaitAsync(ct);
            return OkFile();
        });

        for (int i = 0; i < 500; i++)
        {
            _ = queue.Enqueue($"p{i}", "plain", async ct =>
            {
                await barrier.Task.WaitAsync(ct);
                return OkFile();
            });
        }

        Action overflow = () => _ = queue.Enqueue("overflow", "plain", async ct =>
        {
            await barrier.Task.WaitAsync(ct);
            return OkFile();
        });

        overflow.Should().Throw<InvalidOperationException>()
            .WithMessage("*capacity*");

        barrier.SetResult(true);

        await Task.Delay(300, CancellationToken.None);
        await queue.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWorkThrows_MarksJobFailedWithErrorMessage()
    {
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger = new();
        InMemoryBackgroundJobQueue queue = new(logger.Object);
        await queue.StartAsync(CancellationToken.None);

        string jobId = queue.Enqueue("bad", "plain", _ => throw new InvalidOperationException("work failed"));

        await WaitForTerminalStateAsync(queue, jobId, TimeSpan.FromSeconds(5));

        BackgroundJobInfo? info = queue.GetInfo(jobId);
        info.Should().NotBeNull();
        info!.State.Should().Be(BackgroundJobState.Failed);
        info.Error.Should().Contain("work failed");

        await queue.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task EvictOldTerminalJobs_AfterMoreThan200Succeeded_OldestJobRemoved()
    {
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger = new();
        InMemoryBackgroundJobQueue queue = new(logger.Object);
        await queue.StartAsync(CancellationToken.None);

        List<string> ids = [];
        for (int i = 0; i < 201; i++)
        {
            string id = queue.Enqueue($"ok{i}", "plain", async ct =>
            {
                await Task.Delay(15, ct);
                return OkFile();
            });
            ids.Add(id);
        }

        await WaitForTerminalStateAsync(queue, ids[^1], TimeSpan.FromSeconds(120));

        queue.GetInfo(ids[0]).Should().BeNull("oldest terminal job should be evicted after 201 completions");

        await queue.StopAsync(CancellationToken.None);
    }

    private static async Task WaitForTerminalStateAsync(
        InMemoryBackgroundJobQueue queue,
        string jobId,
        TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            BackgroundJobInfo? info = queue.GetInfo(jobId);
            if (info is { State: BackgroundJobState.Succeeded or BackgroundJobState.Failed })
                return;

            await Task.Delay(20);
        }

        throw new TimeoutException($"Job {jobId} did not reach a terminal state within {timeout}.");
    }
}
