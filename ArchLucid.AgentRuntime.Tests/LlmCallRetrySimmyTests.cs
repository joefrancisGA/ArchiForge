using System.Net;

using FluentAssertions;

using Polly;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Timeout;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class LlmCallRetrySimmyTests
{
    /// <summary>Outer-loop trials for 50% chaos injection (statistical coverage, not tied to retry attempt count).</summary>
    private const int ChaosResilienceObservationIterations = 12;

    [Fact]
    public async Task ChaosTransient429_RetryRecoverBeforeCBTrip()
    {
        int innerCalls = 0;
        int chaosWave = 0;

        ChaosFaultStrategyOptions chaosOptions = new()
        {
            InjectionRate = 1.0
        };
        chaosOptions.EnabledGenerator = _ => new ValueTask<bool>(Interlocked.Increment(ref chaosWave) <= 2);
        chaosOptions.FaultGenerator = static _ =>
            new ValueTask<Exception?>(
                new HttpRequestException("429", null, HttpStatusCode.TooManyRequests));

        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 4,
                    Delay = TimeSpan.FromMilliseconds(1),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(LlmCallResilienceDefaults.ShouldRetryLlmException),
                })
            .AddChaosFault(chaosOptions)
            .Build();

        await pipeline.ExecuteAsync(
            async _ =>
            {
                Interlocked.Increment(ref innerCalls);
                await Task.CompletedTask;
            },
            CancellationToken.None);

        innerCalls.Should().Be(1);
        chaosWave.Should().Be(3);
    }

    [Fact]
    public async Task ChaosPersistentFault_RetryExhaustedPropagates()
    {
        ChaosFaultStrategyOptions chaosOptions = new()
        {
            InjectionRate = 1.0,
            EnabledGenerator = static _ => new ValueTask<bool>(true),
            FaultGenerator = static _ => new ValueTask<Exception?>(new HttpRequestException("down")),
        };

        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(1),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(LlmCallResilienceDefaults.ShouldRetryLlmException),
                })
            .AddChaosFault(chaosOptions)
            .Build();

        Func<Task> act = () => pipeline.ExecuteAsync(
                async _ => await Task.CompletedTask,
                CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task InnerTimeout_slowDelegate_RetryThenTimeoutRejected()
    {
        // Retry wraps an inner timeout pipeline (same nesting shape as LLM call resilience).
        //
        // Simmy chaos latency here was dropped: on some Linux CI hosts, chaos latency + nested AddPipeline
        // occasionally completed without TimeoutRejectedException (no exception at all). A delegate-bound
        // delay longer than the inner timeout keeps the behavior deterministic while still proving
        // TimeoutRejectedException propagates after retries are exhausted.
        TimeSpan innerBudget = TimeSpan.FromMilliseconds(50);
        TimeSpan workDuration = TimeSpan.FromMilliseconds(500);

        ResiliencePipeline innerTimeout = new ResiliencePipelineBuilder()
            .AddTimeout(innerBudget)
            .Build();

        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(1),
                    ShouldHandle = new PredicateBuilder().Handle<TimeoutRejectedException>(),
                })
            .AddPipeline(innerTimeout)
            .Build();

        Func<Task> act = () => pipeline.ExecuteAsync(
                async ct =>
                {
                    await Task.Delay(workDuration, ct);
                },
                CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task ChaosIntermittent_FiftyPercent_EventualSuccessWithinBudget()
    {
        ChaosFaultStrategyOptions chaosOptions = new()
        {
            InjectionRate = 0.5,
            EnabledGenerator = static _ => new ValueTask<bool>(true),
            FaultGenerator = static _ => new ValueTask<Exception?>(new HttpRequestException("maybe")),
        };

        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 6,
                    Delay = TimeSpan.FromMilliseconds(1),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(LlmCallResilienceDefaults.ShouldRetryLlmException),
                })
            .AddChaosFault(chaosOptions)
            .Build();

        int successes = 0;

        for (int i = 0; i < ChaosResilienceObservationIterations; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(static async _ => await Task.CompletedTask, CancellationToken.None);
                successes++;
            }
            catch (HttpRequestException)
            {
            }
        }

        successes.Should().BeGreaterThanOrEqualTo(3);
    }
}
