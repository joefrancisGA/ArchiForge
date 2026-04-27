using ArchLucid.Persistence.Connections;

using FluentAssertions;

using Polly;

namespace ArchLucid.Persistence.Tests.Connections;

[Trait("Category", "Unit")]
public sealed class SqlOpenResilienceDefaultsTests
{
    [Fact]
    public async Task BuildSqlOpenRetryPipeline_MaxRetryZero_ReturnsNoOpPipeline_ExecutesOnce()
    {
        ResiliencePipeline pipeline = SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(maxRetryAttempts: 0);

        int count = 0;
        await pipeline.ExecuteAsync(
            async _ =>
            {
                count++;
                await Task.CompletedTask;
            },
            CancellationToken.None);

        count.Should().Be(1);
    }

    [Fact]
    public async Task BuildSqlOpenRetryPipeline_PositiveMax_BuildsAndRunsHappyPath()
    {
        ResiliencePipeline pipeline = SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(maxRetryAttempts: 1);

        int result = await pipeline.ExecuteAsync(
            async _ => await ValueTask.FromResult(42),
            CancellationToken.None);

        result.Should().Be(42);
    }
}
