using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Integration")]
[Trait("Suite", "CLI")]
public sealed class ComplianceReportCommandTests
{
    [Fact]
    public async Task Compliance_report_writes_file_from_repository_template()
    {
        string root = ResolveRepositoryRootFromTests();
        string tempFile = Path.Combine(Path.GetTempPath(), "ArchLucid.Cli.cr." + Guid.NewGuid().ToString("N") + ".md");

        string prev = Environment.CurrentDirectory;
        TextWriter prevOut = Console.Out;

        try
        {
            Environment.CurrentDirectory = root;
            Console.SetOut(TextWriter.Null);

            int exit = await Program.RunAsync(["compliance-report", "--out", tempFile]);

            exit.Should().Be(CliExitCode.Success);
            string body = await File.ReadAllTextAsync(tempFile);
            body.Should().Contain("SOC 2 — Owner self-assessment");
            body.Should().Contain("ISO/IEC 27001:2022");
            body.Should().Contain("CLI-generated control mapping");
        }
        finally
        {
            Environment.CurrentDirectory = prev;
            Console.SetOut(prevOut);

            try
            {
                File.Delete(tempFile);
            }
            catch
            {
                // ignore
            }
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

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
