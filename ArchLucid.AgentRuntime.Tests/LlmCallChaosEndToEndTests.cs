using System.Net;

using ArchLucid.Core.Resilience;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Polly;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Fault;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class LlmCallChaosEndToEndTests
{
    [Fact]
    public async Task EndToEnd_TransientChaos_RecoveryWithoutCBTrip()
    {
        int wave = 0;
        ChaosFaultStrategyOptions chaos = new() { InjectionRate = 1.0 };
        chaos.EnabledGenerator = _ => new ValueTask<bool>(Interlocked.Increment(ref wave) <= 2);
        chaos.FaultGenerator = static _ =>
            new ValueTask<Exception?>(
                new HttpRequestException("429", null, HttpStatusCode.TooManyRequests));

        ResiliencePipeline retryAndChaos = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 4,
                    Delay = TimeSpan.FromMilliseconds(1),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(LlmCallResilienceDefaults.ShouldRetryLlmException),
                })
            .AddChaosFault(chaos)
            .Build();

        RecordingCompletionClient inner = new();
        CircuitBreakerOptions options = new() { FailureThreshold = 2, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new("e2e-ok", options);
        CircuitBreakingAgentCompletionClient sut = new(
            inner,
            gate,
            retryAndChaos,
            NullLogger<CircuitBreakingAgentCompletionClient>.Instance);

        string json = await sut.CompleteJsonAsync("s", "u", CancellationToken.None);

        json.Should().Be("{}");
        inner.SuccessCount.Should().Be(1);
        wave.Should().Be(3);
    }

    [Fact]
    public async Task EndToEnd_PersistentChaos_CBTripsAfterRetryExhaustion()
    {
        ChaosFaultStrategyOptions chaos = new()
        {
            InjectionRate = 1.0,
            EnabledGenerator = static _ => new ValueTask<bool>(true),
            FaultGenerator = static _ => new ValueTask<Exception?>(new HttpRequestException("permanent")),
        };

        ResiliencePipeline retryAndChaos = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(1),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(LlmCallResilienceDefaults.ShouldRetryLlmException),
                })
            .AddChaosFault(chaos)
            .Build();

        RecordingCompletionClient inner = new();
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new("e2e-bad", options);
        CircuitBreakingAgentCompletionClient sut = new(
            inner,
            gate,
            retryAndChaos,
            NullLogger<CircuitBreakingAgentCompletionClient>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.CompleteJsonAsync("s", "u", CancellationToken.None));

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            sut.CompleteJsonAsync("s", "u", CancellationToken.None));
    }

    private sealed class RecordingCompletionClient : IAgentCompletionClient
    {
        public int SuccessCount { get; private set; }

        public LlmProviderDescriptor Descriptor => LlmProviderDescriptor.ForOffline("test", "test");

        public Task<string> CompleteJsonAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            SuccessCount++;
            return Task.FromResult("{}");
        }
    }
}
