using ArchiForge.Application.Jobs;
using ArchiForge.Host.Core.Jobs;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Tests for in-memory background job queue (payload executor + channel semantics).
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryBackgroundJobQueueTests
{
    private static BackgroundJobFile OkFile() => new("out.bin", "application/octet-stream", []);

    private static AnalysisReportDocxWorkUnit Work(string label) =>
        new(new AnalysisReportDocxJobPayload { RunId = label }, $"{label}.bin", "application/octet-stream");

    private static (InMemoryBackgroundJobQueue Queue, Mock<IBackgroundJobWorkUnitExecutor> Executor) CreateSystem(
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger,
        Action<Mock<IBackgroundJobWorkUnitExecutor>>? configureExecutor = null)
    {
        Mock<IBackgroundJobWorkUnitExecutor> executor = new();
        configureExecutor?.Invoke(executor);

        ServiceCollection services = new();
        services.AddLogging();
        services.AddScoped<IBackgroundJobWorkUnitExecutor>(_ => executor.Object);
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        InMemoryBackgroundJobQueue queue = new(logger.Object, scopeFactory);

        return (queue, executor);
    }

    [Fact]
    public async Task Enqueue_WhenPendingChannelIsFull_ThrowsInvalidOperationException()
    {
        TaskCompletionSource<bool> barrier = new();
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger = new();

        (InMemoryBackgroundJobQueue queue, _) = CreateSystem(
            logger,
            m => m.Setup(x => x.ExecuteAsync(It.IsAny<BackgroundJobWorkUnit>(), It.IsAny<CancellationToken>()))
                .Returns<BackgroundJobWorkUnit, CancellationToken>(
                    async (_, ct) =>
                    {
                        await barrier.Task.WaitAsync(ct);

                        return OkFile();
                    }));

        await queue.StartAsync(CancellationToken.None);

        _ = await queue.EnqueueAsync(Work("block"));

        await Task.Delay(100, CancellationToken.None);

        for (int i = 0; i < 500; i++)
            _ = await queue.EnqueueAsync(Work($"p{i}"));

        Func<Task> overflow = async () => _ = await queue.EnqueueAsync(Work("overflow"));

        await overflow.Should().ThrowAsync<InvalidOperationException>().WithMessage("*capacity*");

        barrier.SetResult(true);

        await Task.Delay(300, CancellationToken.None);
        await queue.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWorkThrows_MarksJobFailedWithErrorMessage()
    {
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger = new();

        (InMemoryBackgroundJobQueue queue, _) = CreateSystem(
            logger,
            m => m.Setup(x => x.ExecuteAsync(It.IsAny<BackgroundJobWorkUnit>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("work failed")));

        await queue.StartAsync(CancellationToken.None);

        string jobId = await queue.EnqueueAsync(Work("bad"));

        await WaitForTerminalStateAsync(queue, jobId, TimeSpan.FromSeconds(5));

        BackgroundJobInfo? info = await queue.GetInfoAsync(jobId);
        info.Should().NotBeNull();
        info.State.Should().Be(BackgroundJobState.Failed);
        info.Error.Should().Contain("work failed");

        await queue.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task EvictOldTerminalJobs_AfterMoreThan200Succeeded_OldestJobRemoved()
    {
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger = new();

        (InMemoryBackgroundJobQueue queue, _) = CreateSystem(
            logger,
            m => m.Setup(x => x.ExecuteAsync(It.IsAny<BackgroundJobWorkUnit>(), It.IsAny<CancellationToken>()))
                .Returns(
                    async (BackgroundJobWorkUnit _, CancellationToken ct) =>
                    {
                        await Task.Delay(15, ct);

                        return OkFile();
                    }));

        await queue.StartAsync(CancellationToken.None);

        List<string> ids = [];
        for (int i = 0; i < 201; i++)
        {
            string id = await queue.EnqueueAsync(Work($"ok{i}"));
            ids.Add(id);
        }

        await WaitForTerminalStateAsync(queue, ids[^1], TimeSpan.FromSeconds(120));

        (await queue.GetInfoAsync(ids[0])).Should().BeNull("oldest terminal job should be evicted after 201 completions");

        await queue.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Enqueue_WithRetry_RetriesOnFailureThenSucceeds()
    {
        int attempt = 0;
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger = new();

        (InMemoryBackgroundJobQueue queue, _) = CreateSystem(
            logger,
            m => m.Setup(x => x.ExecuteAsync(It.IsAny<BackgroundJobWorkUnit>(), It.IsAny<CancellationToken>()))
                .Returns(
                    () =>
                    {
                        attempt++;

                        return attempt < 3
                            ? Task.FromException<BackgroundJobFile>(new InvalidOperationException($"Transient failure #{attempt}"))
                            : Task.FromResult(OkFile());
                    }));

        await queue.StartAsync(CancellationToken.None);

        string jobId = await queue.EnqueueAsync(Work("retry-ok"), maxRetries: 3);

        await WaitForTerminalStateAsync(queue, jobId, TimeSpan.FromSeconds(30));

        BackgroundJobInfo? info = await queue.GetInfoAsync(jobId);
        info.Should().NotBeNull();
        info.State.Should().Be(BackgroundJobState.Succeeded);
        info.RetryCount.Should().Be(2, "two retries should have occurred before success");

        await queue.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Enqueue_WithRetry_ExhaustsRetriesThenFails()
    {
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger = new();

        (InMemoryBackgroundJobQueue queue, _) = CreateSystem(
            logger,
            m => m.Setup(x => x.ExecuteAsync(It.IsAny<BackgroundJobWorkUnit>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Always fails")));

        await queue.StartAsync(CancellationToken.None);

        string jobId = await queue.EnqueueAsync(Work("retry-fail"), maxRetries: 2);

        await WaitForTerminalStateAsync(queue, jobId, TimeSpan.FromSeconds(30));

        BackgroundJobInfo? info = await queue.GetInfoAsync(jobId);
        info.Should().NotBeNull();
        info.State.Should().Be(BackgroundJobState.Failed);
        info.RetryCount.Should().Be(3, "initial attempt + 2 retries = 3 total attempts");
        info.Error.Should().Contain("Always fails");

        await queue.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Enqueue_WithZeroRetries_FailsImmediately()
    {
        Mock<ILogger<InMemoryBackgroundJobQueue>> logger = new();

        (InMemoryBackgroundJobQueue queue, _) = CreateSystem(
            logger,
            m => m.Setup(x => x.ExecuteAsync(It.IsAny<BackgroundJobWorkUnit>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Immediate fail")));

        await queue.StartAsync(CancellationToken.None);

        string jobId = await queue.EnqueueAsync(Work("no-retry"), maxRetries: 0);

        await WaitForTerminalStateAsync(queue, jobId, TimeSpan.FromSeconds(5));

        BackgroundJobInfo? info = await queue.GetInfoAsync(jobId);
        info.Should().NotBeNull();
        info.State.Should().Be(BackgroundJobState.Failed);
        info.RetryCount.Should().Be(1, "one attempt, no retries");

        await queue.StopAsync(CancellationToken.None);
    }

    private static async Task WaitForTerminalStateAsync(InMemoryBackgroundJobQueue queue, string jobId, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            BackgroundJobInfo? info = await queue.GetInfoAsync(jobId);
            if (info is { State: BackgroundJobState.Succeeded or BackgroundJobState.Failed })
                return;

            await Task.Delay(20);
        }

        throw new TimeoutException($"Job {jobId} did not reach a terminal state within {timeout}.");
    }
}
