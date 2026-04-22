using System.Net;

using ArchLucid.Persistence.Connections;
using ArchLucid.TestSupport;

using FluentAssertions;

using Polly;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Fault;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
/// Simmy scenarios where a single outer retry pipeline must tolerate <b>multiple</b> transient failure shapes
/// (SQL + HTTP), matching mixed dependency behavior during incidents without separate suites per subsystem.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class CombinedFailureChaosTests
{
    [Fact]
    public async Task Combined_sql_then_http_transient_faults_outer_retry_eventually_succeeds()
    {
        int innerSuccesses = 0;
        int chaosWave = 0;

        ChaosFaultStrategyOptions chaosOptions = new()
        {
            InjectionRate = 1.0,
            EnabledGenerator = _ => new ValueTask<bool>(Interlocked.Increment(ref chaosWave) <= 2),
            FaultGenerator = _ =>
            {
                int w = chaosWave;

                if (w == 1)
                {
                    return new ValueTask<Exception?>(SqlExceptionTestFactory.Create(40613));
                }

                if (w == 2)
                {
                    return new ValueTask<Exception?>(
                        new HttpRequestException("429", null, HttpStatusCode.TooManyRequests));
                }

                return new ValueTask<Exception?>((Exception?)null);
            }
        };

        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 5,
                    Delay = TimeSpan.FromMilliseconds(1),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(
                        ex => SqlTransientDetector.IsTransient(ex)
                              || LlmCallResilienceDefaults.ShouldRetryLlmException(ex)),
                })
            .AddChaosFault(chaosOptions)
            .Build();

        await pipeline.ExecuteAsync(
            async _ =>
            {
                Interlocked.Increment(ref innerSuccesses);
                await Task.CompletedTask;
            },
            CancellationToken.None);

        innerSuccesses.Should().Be(1);
        chaosWave.Should().Be(3);
    }
}
