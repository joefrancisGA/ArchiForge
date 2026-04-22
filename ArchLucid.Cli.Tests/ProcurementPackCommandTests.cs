using ArchLucid.Cli;
using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ProcurementPackCommandTests
{
    [Fact]
    public async Task Procurement_pack_unknown_flag_returns_usage_error()
    {
        StringWriter outWriter = new();
        StringWriter errWriter = new();
        TextWriter prevOut = Console.Out;
        TextWriter prevErr = Console.Error;
        Console.SetOut(outWriter);
        Console.SetError(errWriter);
        string prev = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(ResolveRepositoryRootFromTests());

            int exit = await Program.RunAsync(["procurement-pack", "--not-a-real-flag"]);

            exit.Should().Be(CliExitCode.UsageError);
            errWriter.ToString().Should().Contain("Unexpected argument");
        }
        finally
        {
            Directory.SetCurrentDirectory(prev);
            Console.SetOut(prevOut);
            Console.SetError(prevErr);
        }
    }

    [Fact]
    public async Task Procurement_pack_missing_out_value_returns_usage_error()
    {
        StringWriter errWriter = new();
        TextWriter prevErr = Console.Error;
        Console.SetError(errWriter);
        string prev = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(ResolveRepositoryRootFromTests());

            int exit = await Program.RunAsync(["procurement-pack", "--out"]);

            exit.Should().Be(CliExitCode.UsageError);
            errWriter.ToString().Should().Contain("Missing value for --out");
        }
        finally
        {
            Directory.SetCurrentDirectory(prev);
            Console.SetError(prevErr);
        }
    }

    [Fact]
    public async Task Procurement_pack_dry_run_succeeds_when_python_and_sources_present()
    {
        StringWriter outWriter = new();
        StringWriter errWriter = new();
        TextWriter prevOut = Console.Out;
        TextWriter prevErr = Console.Error;
        Console.SetOut(outWriter);
        Console.SetError(errWriter);
        string prev = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(ResolveRepositoryRootFromTests());

            int exit = await Program.RunAsync(["procurement-pack", "--dry-run"]);

            exit.Should().Be(CliExitCode.Success, because: $"stdout+stderr: {outWriter}{errWriter}");
            outWriter.ToString().Should().Contain("dry-run");
        }
        finally
        {
            Directory.SetCurrentDirectory(prev);
            Console.SetOut(prevOut);
            Console.SetError(prevErr);
        }
    }

    private static string ResolveRepositoryRootFromTests()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        for (int ascent = 0; ascent < 28 && directory is not null; ascent++)
        {
            string marker = Path.Combine(directory.FullName, "docs", "go-to-market", "MARKETPLACE_PUBLICATION.md");

            if (File.Exists(marker))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate repository root from test output directory (expected docs/go-to-market/MARKETPLACE_PUBLICATION.md).");
    }
}
