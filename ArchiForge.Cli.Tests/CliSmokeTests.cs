using System.Text;

using FluentAssertions;

namespace ArchiForge.Cli.Tests;

public sealed class CliSmokeTests
{
    [Fact]
    public async Task RunAsync_WhenNoArgs_Returns1()
    {
        int exit = await Program.RunAsync([]);
        exit.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_ComparisonsWithNoSubcommand_ShowsUsageAndReturns1()
    {
        StringBuilder sb = new StringBuilder();
        TextWriter oldOut = Console.Out;
        try
        {
            Console.SetOut(new StringWriter(sb));
            int exit = await Program.RunAsync(["comparisons"]);
            exit.Should().Be(1);
        }
        finally
        {
            Console.SetOut(oldOut);
        }

        sb.ToString().Should().Contain("Usage: archiforge comparisons");
    }
}

