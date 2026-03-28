using ArchiForge.Core.Resilience;
using ArchiForge.Retrieval.Embedding;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchiForge.Retrieval.Tests;

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
        CircuitBreakerGate gate = new(options, clock.ToFunc());

        CircuitBreakingOpenAiEmbeddingClient sut = new(
            inner.Object,
            gate,
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
        CircuitBreakerGate gate = new(options, clock.ToFunc());

        CircuitBreakingOpenAiEmbeddingClient sut = new(
            inner.Object,
            gate,
            NullLogger<CircuitBreakingOpenAiEmbeddingClient>.Instance);

        Func<Task> act = () => sut.EmbedAsync("x", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        clock.Advance(TimeSpan.FromSeconds(1));
        Func<Task> act2 = () => sut.EmbedAsync("y", CancellationToken.None);

        await act2.Should().ThrowAsync<CircuitBreakerOpenException>();
    }
}
