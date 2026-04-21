using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Suite", "Core")]
public sealed class PilotUpCommandTests
{
    [Fact]
    public void FindDockerComposeDirectory_FromRepoRoot_ReturnsDirectoryContainingCompose()
    {
        string? dir = PilotUpCommand.FindDockerComposeDirectory();

        dir.Should().NotBeNull();
        File.Exists(Path.Combine(dir, "docker-compose.yml")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "docker-compose.demo.yml")).Should().BeTrue();
    }

    [Fact]
    public async Task PilotSubcommand_WithoutUp_PrintsExpected()
    {
        StringWriter outWriter = new();
        StringWriter errWriter = new();
        TextWriter prevOut = Console.Out;
        TextWriter prevErr = Console.Error;
        Console.SetOut(outWriter);
        Console.SetError(errWriter);
        try
        {
            int exit = await Program.RunAsync(["pilot"]);

            exit.Should().Be(CliExitCode.UsageError);
            (outWriter.ToString() + errWriter.ToString()).Should().Contain("archlucid pilot up");
        }
        finally
        {
            Console.SetOut(prevOut);
            Console.SetError(prevErr);
        }
    }
}
