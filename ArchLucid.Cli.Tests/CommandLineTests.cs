using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>
///     Tests for Command Line.
/// </summary>
[Trait("Suite", "Core")]
public sealed class CommandLineTests
{
    [Fact]
    public async Task NoArgs_Returns1_AndPrintsUsage()
    {
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            int exitCode = await Program.RunAsync([]);

            exitCode.Should().Be(CliExitCode.UsageError);
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
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            int exitCode = await Program.RunAsync(["invalid"]);

            exitCode.Should().Be(CliExitCode.UsageError);
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
    public async Task Health_WhenApiUnreachable_Returns3()
    {
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            int exitCode = await Program.RunAsync(["health"]);

            exitCode.Should().Be(CliExitCode.ApiUnavailable);
            string output = outWriter + errWriter.ToString();
            (output.Contains("FAIL") || output.Contains("Cannot connect") || output.Contains("Cannot reach") ||
             output.Contains("api_unreachable"))
                .Should().BeTrue("output should contain unreachable guidance or JSON error code");
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    [Fact]
    public async Task LeadingGlobalJson_WithNoArgs_WritesJsonUsageToStderr()
    {
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            int exitCode = await Program.RunAsync(["--json"]);

            exitCode.Should().Be(CliExitCode.UsageError);
            string err = errWriter.ToString();
            err.Should().Contain("\"ok\":false");
            err.Should().Contain("\"exitCode\":1");
            err.Should().Contain("\"error\":\"usage\"");
            outWriter.ToString().Should().BeEmpty();
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    [Fact]
    public async Task LeadingGlobalJson_HealthUnreachable_WritesJsonToStderr()
    {
        RedirectConsole(out _, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            int exitCode = await Program.RunAsync(["--json", "health"]);

            exitCode.Should().Be(CliExitCode.ApiUnavailable);
            string err = errWriter.ToString();
            err.Should().Contain("\"ok\":false");
            err.Should().Contain("\"exitCode\":3");
            err.Should().Contain("api_unreachable");
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

                exitCode.Should().Be(CliExitCode.Success);
                string projectDir = Path.Combine(temp.Path, "TestProject");
                string manifestPath = Path.Combine(projectDir, ArchLucidProjectScaffolder.CliManifestFileName);
                string briefMd = Path.Combine(projectDir, "inputs", "brief.md");

                File.Exists(manifestPath).Should().BeTrue("archlucid.json should be created");
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

    private static void RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut,
        out TextWriter prevErr)
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
        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public string Path
        {
            get;
        } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "ArchLucid.Cli.Tests." + Guid.NewGuid().ToString("N")[..8]);

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
