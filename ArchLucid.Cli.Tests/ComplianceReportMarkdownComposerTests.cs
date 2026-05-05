using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ComplianceReportMarkdownComposerTests
{
    [Fact]
    public void Compose_includes_iso_row_and_generated_header()
    {
        ComplianceReportAuditLiveSample emptyLive =
            new(true, null, 0, new Dictionary<string, int>(), null, null);

        string md = ComplianceReportMarkdownComposer.Compose(
            "# Template",
            "C:\\repo",
            "2026-05-05T12:00:00Z",
            "TEST-MACHINE",
            "C:\\cwd",
            "| Key | Value |\n|---|---|\n| x | y |",
            "| Severity | Count |\n|---|---|\n| Error | 0 |",
            emptyLive,
            true);

        md.Should().Contain("# Template");
        md.Should().Contain("CLI-generated control mapping");
        md.Should().Contain("A.5.15");
        md.Should().Contain("**Generated:**");
        md.Should().Contain("TEST-MACHINE");
    }
}
