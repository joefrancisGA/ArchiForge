using ArchLucid.Host.Core.Jobs;

using FluentAssertions;

namespace ArchLucid.Jobs.Cli.Tests;

/// <summary>Covers <see cref="Program.RunAsync" /> paths before the web host is built (invalid <c>--job</c> CLI).</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ProgramRunAsyncEarlyExitTests
{
    [Fact]
    public async Task RunAsync_empty_args_returns_configuration_error()
    {
        int exit = await Program.RunAsync([]);

        exit.Should().Be(ArchLucidJobExitCodes.ConfigurationError);
    }

    [Fact]
    public async Task RunAsync_without_job_flag_returns_configuration_error()
    {
        int exit = await Program.RunAsync(["--verbose"]);

        exit.Should().Be(ArchLucidJobExitCodes.ConfigurationError);
    }

    [Fact]
    public async Task RunAsync_job_flag_without_value_returns_configuration_error()
    {
        int exit = await Program.RunAsync(["--job"]);

        exit.Should().Be(ArchLucidJobExitCodes.ConfigurationError);
    }

    [Fact]
    public async Task RunAsync_whitespace_job_name_returns_configuration_error()
    {
        int exit = await Program.RunAsync(["--job", "   "]);

        exit.Should().Be(ArchLucidJobExitCodes.ConfigurationError);
    }
}
