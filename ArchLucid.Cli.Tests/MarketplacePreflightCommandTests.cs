using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class MarketplacePreflightCommandTests
{
    [Fact]
    public async Task RunAsync_unknown_argument_returns_usage_error()
    {
        int code = await MarketplacePreflightCommand.RunAsync(["--nope"]);

        code.Should().Be(CliExitCode.UsageError);
    }

    [Fact]
    public async Task RunAsync_repo_flag_without_value_returns_usage_error()
    {
        int code = await MarketplacePreflightCommand.RunAsync(["--repo"]);

        code.Should().Be(CliExitCode.UsageError);
    }
}
