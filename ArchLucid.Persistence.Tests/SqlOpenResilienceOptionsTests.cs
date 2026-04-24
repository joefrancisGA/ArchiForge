using ArchLucid.Persistence.Connections;

using FluentAssertions;

using Polly;

namespace ArchLucid.Persistence.Tests;

[Trait("Suite", "Core")]
public sealed class SqlOpenResilienceOptionsTests
{
    [Fact]
    public void Normalize_clamps_max_retries_and_base_delay()
    {
        SqlOpenResilienceOptions options = new() { MaxRetryAttempts = -5, BaseDelayMilliseconds = -1 };
        options.Normalize();

        options.MaxRetryAttempts.Should().Be(0);
        options.BaseDelayMilliseconds.Should().Be(1);
    }

    [Fact]
    public async Task BuildSqlOpenRetryPipeline_uses_values_matching_bound_options()
    {
        SqlOpenResilienceOptions options = new() { MaxRetryAttempts = 1, BaseDelayMilliseconds = 1 };
        options.Normalize();

        ResiliencePipeline pipeline = SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(
            null,
            options.MaxRetryAttempts,
            TimeSpan.FromMilliseconds(options.BaseDelayMilliseconds));

        int tries = 0;
        await pipeline.ExecuteAsync(
            async _ =>
            {
                tries++;

                if (tries == 1)
                {
                    throw new TimeoutException("transient");
                }

                await Task.CompletedTask;
            },
            CancellationToken.None);

        tries.Should().Be(2);
    }
}
