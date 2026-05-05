using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ManifestValidateCommandTests
{
    private const string MinimalValidGoldenManifest =
        """
        {
          "runId": "11111111-1111-1111-1111-111111111111",
          "systemName": "demo",
          "services": [],
          "datastores": [],
          "relationships": [],
          "governance": {},
          "metadata": {
            "manifestVersion": "1",
            "createdUtc": "2026-01-01T00:00:00Z"
          }
        }
        """;

    [Fact]
    public async Task Validate_valid_manifest_exits_0()
    {
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            using TempDirectory temp = new();
            string path = Path.Combine(temp.Path, "gm.json");
            await File.WriteAllTextAsync(path, MinimalValidGoldenManifest);

            int exit =
                await Program.RunAsync(["manifest", "validate", "--file", path]);

            exit.Should().Be(CliExitCode.Success);
            string combined = outWriter + errWriter.ToString();
            combined.Should().Contain("Valid golden manifest:");
            combined.Should().Contain(Path.GetFileName(path));
            errWriter.ToString().Should().BeEmpty();
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    [Fact]
    public async Task Validate_missing_required_property_exits_1_with_actionable_text()
    {
        RedirectConsole(out StringWriter _, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            using TempDirectory temp = new();
            string path = Path.Combine(temp.Path, "bad.json");
            string bad = MinimalValidGoldenManifest.Replace("\"runId\"", "\"_runId_stub\"", StringComparison.Ordinal);

            await File.WriteAllTextAsync(path, bad);

            int exit =
                await Program.RunAsync(["manifest", "validate", "--file", path]);

            exit.Should().Be(CliExitCode.UsageError);
            string err = errWriter.ToString();
            err.Should().Contain("[manifest validate]");
            err.Should().Contain("runId");
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    [Fact]
    public async Task Validate_invalid_json_exits_1_and_mentions_line()
    {
        RedirectConsole(out StringWriter _, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            using TempDirectory temp = new();
            string path = Path.Combine(temp.Path, "bad.json");
            await File.WriteAllTextAsync(
                path,
                """
                {
                  "oops": 
                }
                """);

            int exit =
                await Program.RunAsync(["manifest", "validate", "--file", path]);

            exit.Should().Be(CliExitCode.UsageError);
            string err = errWriter.ToString();
            err.Should().Contain("[manifest validate]");
            err.Should().MatchRegex("line [0-9]+", "parse errors should cite a line number when available");
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    [Fact]
    public async Task Validate_missing_file_argument_exits_1()
    {
        RedirectConsole(out StringWriter _, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            int exit =
                await Program.RunAsync(["manifest", "validate"]);

            exit.Should().Be(CliExitCode.UsageError);
            errWriter.ToString().Should().Contain("Usage:");
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    [Fact]
    public async Task Validate_json_success_emits_ok_json_on_stdout()
    {
        RedirectConsole(out StringWriter outWriter, out StringWriter errWriter, out TextWriter prevOut,
            out TextWriter prevErr);
        try
        {
            using TempDirectory temp = new();
            string path = Path.Combine(temp.Path, "gm.json");
            await File.WriteAllTextAsync(path, MinimalValidGoldenManifest);

            int exit =
                await Program.RunAsync(["--json", "manifest", "validate", "--file", path]);

            exit.Should().Be(CliExitCode.Success);
            string line = outWriter.ToString().Trim();
            line.Should().StartWith("{");
            line.Should().Contain("\"ok\":true");
            errWriter.ToString().Should().BeEmpty();
        }
        finally
        {
            RestoreConsole(prevOut, prevErr);
        }
    }

    private static void RedirectConsole(out StringWriter outWriter, out StringWriter errWriter,
        out TextWriter prevOut, out TextWriter prevErr)
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

        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "ArchLucid.Cli.Tests." + Guid.NewGuid().ToString("N")[..8]);

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
