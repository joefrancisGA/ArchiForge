using System.Net.Http;

using ArchiForge.AgentRuntime;
using ArchiForge.Core.Resilience;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class CircuitBreakingAgentCompletionClientTests
{
    [Fact]
    public async Task Success_delegates_to_inner()
    {
        Mock<IAgentCompletionClient> inner = new();
        inner.Setup(c => c.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");

        CircuitBreakerOptions options = new() { FailureThreshold = 5, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new(options);

        CircuitBreakingAgentCompletionClient sut = new(
            inner.Object,
            gate,
            NullLogger<CircuitBreakingAgentCompletionClient>.Instance);

        string result = await sut.CompleteJsonAsync("s", "u", CancellationToken.None);

        result.Should().Be("{}");
    }

    [Fact]
    public async Task Inner_failure_opens_circuit_after_threshold()
    {
        Mock<IAgentCompletionClient> inner = new();
        inner.Setup(c => c.CompleteJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("429"));

        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        TestClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerGate gate = new(options, clock.ToFunc());

        CircuitBreakingAgentCompletionClient sut = new(
            inner.Object,
            gate,
            NullLogger<CircuitBreakingAgentCompletionClient>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.CompleteJsonAsync("s", "u", CancellationToken.None));

        clock.Advance(TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            sut.CompleteJsonAsync("s", "u", CancellationToken.None));
    }

    private sealed class TestClock(DateTimeOffset start)
    {
        private DateTimeOffset _t = start;

        public void Advance(TimeSpan delta)
        {
            _t += delta;
        }

        public Func<DateTimeOffset> ToFunc()
        {
            return () => _t;
        }
    }
}
