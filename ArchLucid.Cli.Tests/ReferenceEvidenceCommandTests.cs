using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

public sealed class ReferenceEvidenceCommandTests
{
    [Fact]
    public async Task Usage_error_when_neither_run_nor_tenant()
    {
        int exit = await ReferenceEvidenceCommand.RunAsync([], CancellationToken.None);

        exit.Should().Be(CliExitCode.UsageError);
    }

    [Fact]
    public async Task Usage_error_when_both_run_and_tenant()
    {
        int exit = await ReferenceEvidenceCommand.RunAsync(
            [
                "--run",
                "6e8c4a10-2b1f-4c9a-9d3e-10b2a4f0c501",
                "--tenant",
                "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
            ],
            CancellationToken.None);

        exit.Should().Be(CliExitCode.UsageError);
    }
}
