using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ComplianceReportOptionsTests
{
    [Fact]
    public void Parse_defaults_without_live_audit()
    {
        ComplianceReportOptions? opts = ComplianceReportOptions.Parse([], out string? error);

        error.Should().BeNull();
        opts!.WithLiveAudit.Should().BeFalse();
        opts.OutPath.Should().BeNull();
        opts.RepoRoot.Should().BeNull();
    }

    [Fact]
    public void Parse_accepts_out_repo_and_with_live_audit()
    {
        ComplianceReportOptions? opts = ComplianceReportOptions.Parse(
            ["--out", "r.md", "--repo", "C:\\repo", "--with-live-audit"],
            out string? error);

        error.Should().BeNull();
        opts!.OutPath.Should().Be("r.md");
        opts.RepoRoot.Should().Be("C:\\repo");
        opts.WithLiveAudit.Should().BeTrue();
    }

    [Fact]
    public void Parse_rejects_unknown_flag()
    {
        ComplianceReportOptions? opts = ComplianceReportOptions.Parse(["--bogus"], out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--bogus");
    }

    [Fact]
    public void Parse_rejects_missing_out_value()
    {
        ComplianceReportOptions? opts = ComplianceReportOptions.Parse(["--out"], out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--out");
    }
}
