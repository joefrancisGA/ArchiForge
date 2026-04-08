using ArchLucid.AgentRuntime;
using ArchLucid.Core.Resilience;
using ArchLucid.Retrieval.Embedding;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Polly;

namespace ArchLucid.Retrieval.Tests;

/// <summary>
/// Tests for Circuit Breaking Open Ai Embedding Client.
/// </summary>

[Trait("Category", "Unit")]
public sealed class CircuitBreakingOpenAiEmbeddingClientTests
{
    [Fact]
    public async Task Success_resets_gate_via_RecordSuccess()
    {
        Mock<IOpenAiEmbeddingClient> inner = new();
        float[] vector = [0.1f, 0.2f];
        inner.Setup(c => c.EmbedAsync("x", It.IsAny<CancellationToken>())).ReturnsAsync(vector);

        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerGate gate = new("test-gate", options, clock.ToFunc());

        CircuitBreakingOpenAiEmbeddingClient sut = new(
            inner.Object,
            gate,
            ResiliencePipeline.Empty,
            NullLogger<CircuitBreakingOpenAiEmbeddingClient>.Instance);

        float[] result = await sut.EmbedAsync("x", CancellationToken.None);

        result.Should().Equal(vector);
        inner.Verify(c => c.EmbedAsync("x", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Inner_failure_records_gate_failure()
    {
        Mock<IOpenAiEmbeddingClient> inner = new();
        inner.Setup(c => c.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API error"));

        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerGate gate = new("test-gate", options, clock.ToFunc());

        CircuitBreakingOpenAiEmbeddingClient sut = new(
            inner.Object,
            gate,
            ResiliencePipeline.Empty,
            NullLogger<CircuitBreakingOpenAiEmbeddingClient>.Instance);

        Func<Task> act = () => sut.EmbedAsync("x", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        clock.Advance(TimeSpan.FromSeconds(1));
        Func<Task> act2 = () => sut.EmbedAsync("y", CancellationToken.None);

        await act2.Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    [Fact]
    public async Task EmbedAsync_Retry_TransientThenSuccess()
    {
        int calls = 0;
        Mock<IOpenAiEmbeddingClient> inner = new();
        float[] vector = [0.3f];
        inner.Setup(c => c.EmbedAsync("x", It.IsAny<CancellationToken>()))
            .Returns(
                (string _, CancellationToken _) =>
                {
                    int n = Interlocked.Increment(ref calls);

                    if (n < 3)
                    {
                        return Task.FromException<float[]>(new HttpRequestException("429"));
                    }

                    return Task.FromResult(vector);
                });

        CircuitBreakerOptions options = new() { FailureThreshold = 5, DurationOfBreakSeconds = 60 };
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerGate gate = new("embed-retry", options, clock.ToFunc());
        ResiliencePipeline retry = LlmCallResilienceDefaults.BuildLlmRetryPipeline(
            maxRetryAttempts: 4,
            baseDelay: TimeSpan.FromMilliseconds(1));

        CircuitBreakingOpenAiEmbeddingClient sut = new(
            inner.Object,
            gate,
            retry,
            NullLogger<CircuitBreakingOpenAiEmbeddingClient>.Instance);

        float[] result = await sut.EmbedAsync("x", CancellationToken.None);

        result.Should().Equal(vector);
        calls.Should().Be(3);
    }

    [Fact]
    public async Task EmbedManyAsync_Retry_TransientThenSuccess()
    {
        int calls = 0;
        Mock<IOpenAiEmbeddingClient> inner = new();
        IReadOnlyList<float[]> batch = [new[] { 0.1f }];
        inner.Setup(c => c.EmbedManyAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(
                (IReadOnlyList<string> _, CancellationToken _) =>
                {
                    int n = Interlocked.Increment(ref calls);

                    if (n < 2)
                    {
                        return Task.FromException<IReadOnlyList<float[]>>(new HttpRequestException("503"));
                    }

                    return Task.FromResult(batch);
                });

        CircuitBreakerOptions options = new() { FailureThreshold = 5, DurationOfBreakSeconds = 60 };
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerGate gate = new("embed-batch-retry", options, clock.ToFunc());
        ResiliencePipeline retry = LlmCallResilienceDefaults.BuildLlmRetryPipeline(
            maxRetryAttempts: 3,
            baseDelay: TimeSpan.FromMilliseconds(1));

        CircuitBreakingOpenAiEmbeddingClient sut = new(
            inner.Object,
            gate,
            retry,
            NullLogger<CircuitBreakingOpenAiEmbeddingClient>.Instance);

        IReadOnlyList<float[]> result = await sut.EmbedManyAsync(["a"], CancellationToken.None);

        result.Should().Equal(batch);
        calls.Should().Be(2);
    }

    private sealed class MutableUtcClock
    {
        private DateTimeOffset _now;

        public MutableUtcClock(DateTimeOffset start) => _now = start;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);

        public Func<DateTimeOffset> ToFunc() => () => _now;
    }
}
