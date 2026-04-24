using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
public sealed class CliResilienceOptionsTests
{
    [Fact]
    public void FromCliConfig_applies_http_resilience_section()
    {
        ArchLucidProjectScaffolder.ArchLucidCliConfig config = new()
        {
            HttpResilience = new ArchLucidProjectScaffolder.CliHttpResilienceConfig
            {
                MaxRetryAttempts = 1, InitialDelaySeconds = 2
            }
        };

        CliResilienceOptions options = CliResilienceOptions.FromCliConfig(config);

        options.MaxRetryAttempts.Should().Be(1);
        options.InitialDelaySeconds.Should().Be(2);
    }

    [Fact]
    public void Normalize_clamps_extreme_values()
    {
        CliResilienceOptions options = new() { MaxRetryAttempts = 100, InitialDelaySeconds = 10_000 };
        options.Normalize();

        options.MaxRetryAttempts.Should().Be(10);
        options.InitialDelaySeconds.Should().Be(300);
    }
}
