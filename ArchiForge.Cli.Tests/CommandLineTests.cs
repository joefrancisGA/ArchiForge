using FluentAssertions;

namespace ArchiForge.Cli.Tests;

[Trait("Suite", "Core")]
public sealed class CommandLineTests
{
    [Fact]
    public async Task NoArgs_Returns1_AndPrintsUsage()
    {
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut, out TextWriter prevErr);
        try
        {
            int exitCode = await Program.RunAsync([]);

            exitCode.Should().Be(1);
            string output = outWriter + errWriter.ToString();
            output.Should().Contain("Please provide a command");
            output.Should().Contain("Available commands");
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    [Fact]
    public async Task UnknownCommand_Returns1_AndPrintsUnknown()
    {
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut, out TextWriter prevErr);
        try
        {
            int exitCode = await Program.RunAsync(["invalid"]);

            exitCode.Should().Be(1);
            string output = outWriter + errWriter.ToString();
            output.Should().Contain("Unknown command");
            output.Should().Contain("invalid");
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    [Fact]
    public async Task Health_WhenApiUnreachable_Returns1()
    {
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut, out TextWriter prevErr);
        try
        {
            int exitCode = await Program.RunAsync(["health"]);

            exitCode.Should().Be(1);
            string output = outWriter + errWriter.ToString();
            (output.Contains("FAIL") || output.Contains("Cannot connect") || output.Contains("Cannot reach"))
                .Should().BeTrue("output should contain 'FAIL', 'Cannot connect', or 'Cannot reach'");
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    [Fact]
    public async Task New_WithProjectName_Returns0_AndCreatesFiles()
    {
        using TempDirectory temp = new();
        string prevCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(temp.Path);

            RedirectConsole(out _, out _, out TextWriter prevOut, out TextWriter prevErr);
            try
            {
                int exitCode = await Program.RunAsync(["new", "TestProject"]);

                exitCode.Should().Be(0);
                string projectDir = Path.Combine(temp.Path, "TestProject");
                string archiforgeJson = Path.Combine(projectDir, "archiforge.json");
                string briefMd = Path.Combine(projectDir, "inputs", "brief.md");

                File.Exists(archiforgeJson).Should().BeTrue("archiforge.json should be created");
                File.Exists(briefMd).Should().BeTrue("inputs/brief.md should be created");
            }
            finally
            {
                RestoreConsole(prevOut, prevErr);
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(prevCwd);
        }
    }

    private static void RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut, out TextWriter prevErr)
    {
        outWriter = new StringWriter();
        errWriter = new StringWriter();
        prevOut = Console.Out;
        prevErr = Console.Error;
        Console.SetOut(outWriter);
        Console.SetError(errWriter);
    }

    private static void RestoreConsole(TextWriter prevOut, TextWriter prevErr)
    {
        Console.SetOut(prevOut);
        Console.SetError(prevErr);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ArchiForge.Cli.Tests." + Guid.NewGuid().ToString("N")[..8]);

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
