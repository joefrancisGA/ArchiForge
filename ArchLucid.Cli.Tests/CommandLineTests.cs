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
    public async Task WebhooksTest_without_url_returns_usage_error()
    {
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            int exitCode = await Program.RunAsync(["webhooks", "test"]);

            exitCode.Should().Be(CliExitCode.UsageError);
            string combined = outWriter + errWriter.ToString();
            combined.Should().Contain("webhooks test");
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

    [Fact]
    public async Task New_with_quickstart_before_name_creates_local_evaluation_artifacts()
    {
        using TempDirectory temp = new();
        string prevCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(temp.Path);

            RedirectConsole(out _, out _, out TextWriter prevOut, out TextWriter prevErr);
            try
            {
                int exitCode = await Program.RunAsync(["new", "--quickstart", "QuickProj"]);

                exitCode.Should().Be(CliExitCode.Success);
                string projectDir = Path.Combine(temp.Path, "QuickProj");
                File.Exists(Path.Combine(projectDir, "local", "archlucid-evaluation.sqlite")).Should().BeTrue();
                File.Exists(Path.Combine(projectDir, "local", "archlucid.quickstart.appsettings.json")).Should()
                    .BeTrue();
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

    [Fact]
    public async Task New_with_quickstart_after_name_creates_local_evaluation_artifacts()
    {
        using TempDirectory temp = new();
        string prevCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(temp.Path);

            RedirectConsole(out _, out _, out TextWriter prevOut, out TextWriter prevErr);
            try
            {
                int exitCode = await Program.RunAsync(["new", "QuickProj2", "--quickstart"]);

                exitCode.Should().Be(CliExitCode.Success);
                string projectDir = Path.Combine(temp.Path, "QuickProj2");
                File.Exists(Path.Combine(projectDir, "local", "archlucid-evaluation.sqlite")).Should().BeTrue();
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

    [Fact]
    public async Task New_with_unknown_flag_returns_usage_error()
    {
        using TempDirectory temp = new();
        string prevCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(temp.Path);

            RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut,
                out TextWriter prevErr);
            try
            {
                int exitCode = await Program.RunAsync(["new", "--not-a-real-flag", "P"]);

                exitCode.Should().Be(CliExitCode.UsageError);
                (outWriter + errWriter.ToString()).Should().Contain("Usage").And.Contain("--quickstart");
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
