using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ComplianceReportRepositoryRootResolverTests
{
    [Fact]
    public void TryResolve_finds_repo_from_tests_output_by_walking_up()
    {
        bool ok = ComplianceReportRepositoryRootResolver.TryResolve(
            null,
            AppContext.BaseDirectory,
            out string? root);

        ok.Should().BeTrue();
        root.Should().NotBeNullOrWhiteSpace();
        File.Exists(Path.Combine(root!, ComplianceReportRepositoryRootResolver.Soc2TemplateRelativePath)).Should().BeTrue();
    }

    [Fact]
    public void TryResolve_explicit_wrong_path_returns_false()
    {
        string temp = Path.Combine(Path.GetTempPath(), "ArchLucid.Cli.Tests.cr." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        try
        {
            bool ok = ComplianceReportRepositoryRootResolver.TryResolve(temp, AppContext.BaseDirectory, out string? _);

            ok.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(temp);
        }
    }
}
