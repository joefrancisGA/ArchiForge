using ArchLucid.AgentRuntime;
using ArchLucid.Core.Resilience;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Polly;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Circuit Breaking Agent Completion Client.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CircuitBreakingAgentCompletionClientTests
{
    [Fact]
    public async Task Success_delegates_to_inner()
    {
        Mock<IAgentCompletionClient> inner = new();
        inner.SetupGet(c => c.Descriptor).Returns(LlmProviderDescriptor.ForOffline("mock", "mock"));
        inner.Setup(c => c.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");

        CircuitBreakerOptions options = new() { FailureThreshold = 5, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new("test-gate", options);

        CircuitBreakingAgentCompletionClient sut = new(
            inner.Object,
            gate,
            ResiliencePipeline.Empty,
            NullLogger<CircuitBreakingAgentCompletionClient>.Instance);

        string result = await sut.CompleteJsonAsync("s", "u", CancellationToken.None);

        result.Should().Be("{}");
    }

    [Fact]
    public async Task Inner_failure_opens_circuit_after_threshold()
    {
        Mock<IAgentCompletionClient> inner = new();
        inner.SetupGet(c => c.Descriptor).Returns(LlmProviderDescriptor.ForOffline("mock", "mock"));
        inner.Setup(c => c.CompleteJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("429"));

        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        TestClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerGate gate = new("test-gate", options, clock.ToFunc());

        CircuitBreakingAgentCompletionClient sut = new(
            inner.Object,
            gate,
            ResiliencePipeline.Empty,
            NullLogger<CircuitBreakingAgentCompletionClient>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.CompleteJsonAsync("s", "u", CancellationToken.None));

        clock.Advance(TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            sut.CompleteJsonAsync("s", "u", CancellationToken.None));
    }

    [Fact]
    public async Task Retry_TransientFailure_SucceedsBeforeCBTrip()
    {
        int callCount = 0;
        Mock<IAgentCompletionClient> inner = new();
        inner.SetupGet(c => c.Descriptor).Returns(LlmProviderDescriptor.ForOffline("mock", "mock"));
        inner.Setup(c => c.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                int n = Interlocked.Increment(ref callCount);

                if (n < 3)
                {
                    return Task.FromException<string>(new HttpRequestException("transient"));
                }

                return Task.FromResult("{}");
            });

        CircuitBreakerOptions options = new() { FailureThreshold = 5, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new("retry-ok-gate", options);
        ResiliencePipeline retry = LlmCallResilienceDefaults.BuildLlmRetryPipeline(
            maxRetryAttempts: 4,
            baseDelay: TimeSpan.FromMilliseconds(1),
            maxDelay: TimeSpan.FromMilliseconds(50));

        CircuitBreakingAgentCompletionClient sut = new(
            inner.Object,
            gate,
            retry,
            NullLogger<CircuitBreakingAgentCompletionClient>.Instance);

        string result = await sut.CompleteJsonAsync("s", "u", CancellationToken.None);

        result.Should().Be("{}");
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task Retry_ExhaustedThenCBTrips()
    {
        Mock<IAgentCompletionClient> inner = new();
        inner.SetupGet(c => c.Descriptor).Returns(LlmProviderDescriptor.ForOffline("mock", "mock"));
        inner.Setup(c => c.CompleteJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("always fail"));

        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        TestClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerGate gate = new("retry-exhaust-gate", options, clock.ToFunc());
        ResiliencePipeline retry = LlmCallResilienceDefaults.BuildLlmRetryPipeline(
            maxRetryAttempts: 3,
            baseDelay: TimeSpan.FromMilliseconds(1));

        CircuitBreakingAgentCompletionClient sut = new(
            inner.Object,
            gate,
            retry,
            NullLogger<CircuitBreakingAgentCompletionClient>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.CompleteJsonAsync("s", "u", CancellationToken.None));

        clock.Advance(TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            sut.CompleteJsonAsync("s", "u", CancellationToken.None));

        inner.Verify(
            c => c.CompleteJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    [Fact]
    public async Task Retry_UserCancellation_DoesNotTripCircuit_UserBearerCancelled()
    {
        int calls = 0;
        Mock<IAgentCompletionClient> inner = new();
        inner.SetupGet(c => c.Descriptor).Returns(LlmProviderDescriptor.ForOffline("mock", "mock"));
        inner.Setup(c => c.CompleteJsonAsync("s", "u", It.IsAny<CancellationToken>()))
            .Returns((string _, string __, CancellationToken ct) =>
            {
                Interlocked.Increment(ref calls);

                if (ct.IsCancellationRequested)
                {
                    return Task.FromException<string>(new OperationCanceledException(ct));
                }

                return Task.FromResult("{}");
            });

        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new("cancel-gate", options);
        ResiliencePipeline retry = LlmCallResilienceDefaults.BuildLlmRetryPipeline(
            maxRetryAttempts: 3,
            baseDelay: TimeSpan.FromMilliseconds(1));

        CircuitBreakingAgentCompletionClient sut = new(
            inner.Object,
            gate,
            retry,
            NullLogger<CircuitBreakingAgentCompletionClient>.Instance);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.CompleteJsonAsync("s", "u", cts.Token));

        string ok = await sut.CompleteJsonAsync("s", "u", CancellationToken.None);

        ok.Should().Be("{}");
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
