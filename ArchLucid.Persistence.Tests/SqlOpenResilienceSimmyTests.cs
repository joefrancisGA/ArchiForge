using ArchLucid.Persistence.Connections;
using ArchLucid.TestSupport;

using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Ensures <see cref="SqlOpenResilienceDefaults" /> retry pipeline composes with Simmy SQL faults the same way as
///     <see cref="ResilientSqlConnectionFactory" /> (retry outside chaos).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class SqlOpenResilienceSimmyTests
{
    [Fact]
    public async Task Sql_open_retry_pipeline_outer_with_inner_chaos_transient_sql()
    {
        ResiliencePipeline sqlRetry = SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(
            null,
            4,
            TimeSpan.FromMilliseconds(1));

        int chaosWave = 0;
        int innerCalls = 0;

        ChaosFaultStrategyOptions chaosOptions = new()
        {
            InjectionRate = 1.0,
            EnabledGenerator = _ => new ValueTask<bool>(Interlocked.Increment(ref chaosWave) <= 2),
            FaultGenerator = static _ => new ValueTask<Exception?>(SqlExceptionTestFactory.Create(40613))
        };

        ResiliencePipeline combined = new ResiliencePipelineBuilder()
            .AddPipeline(sqlRetry)
            .AddChaosFault(chaosOptions)
            .Build();

        await combined.ExecuteAsync(
            async _ =>
            {
                Interlocked.Increment(ref innerCalls);
                await Task.CompletedTask;
            },
            CancellationToken.None);

        innerCalls.Should().Be(1);
        chaosWave.Should().Be(3);
    }
}
