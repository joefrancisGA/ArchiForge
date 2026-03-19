using System.Text;
using FluentAssertions;

namespace ArchiForge.Cli.Tests;

public sealed class CliSmokeTests
{
    [Fact]
    public async Task RunAsync_WhenNoArgs_Returns1()
    {
        var exit = await Program.RunAsync([]);
        exit.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_ComparisonsWithNoSubcommand_ShowsUsageAndReturns1()
    {
        var sb = new StringBuilder();
        var oldOut = Console.Out;
        try
        {
            Console.SetOut(new StringWriter(sb));
            var exit = await Program.RunAsync(["comparisons"]);
            exit.Should().Be(1);
        }
        finally
        {
            Console.SetOut(oldOut);
        }

        sb.ToString().Should().Contain("Usage: archiforge comparisons");
    }
}

