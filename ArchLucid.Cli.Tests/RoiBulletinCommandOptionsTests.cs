using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RoiBulletinCommandOptionsTests
{
    [Fact]
    public void Parse_requires_quarter()
    {
        RoiBulletinCommandOptions? opts = RoiBulletinCommandOptions.Parse([], out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--quarter");
    }

    [Fact]
    public void Parse_defaults_min_tenants_to_5()
    {
        RoiBulletinCommandOptions? opts = RoiBulletinCommandOptions.Parse(["--quarter", "Q1-2026"], out string? error);

        error.Should().BeNull();
        opts!.MinTenants.Should().Be(5);
        opts.Quarter.Should().Be("Q1-2026");
    }

    [Fact]
    public void Parse_overrides_min_tenants_and_out()
    {
        RoiBulletinCommandOptions? opts = RoiBulletinCommandOptions.Parse(
            ["--quarter", "Q2-2026", "--min-tenants", "12", "--out", "b.md"],
            out string? error);

        error.Should().BeNull();
        opts!.MinTenants.Should().Be(12);
        opts.OutPath.Should().Be("b.md");
    }

    [Fact]
    public void Parse_rejects_non_positive_min_tenants()
    {
        RoiBulletinCommandOptions? opts = RoiBulletinCommandOptions.Parse(
            ["--quarter", "Q1-2026", "--min-tenants", "0"],
            out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--min-tenants");
    }

    [Fact]
    public void Parse_rejects_unknown_flag()
    {
        RoiBulletinCommandOptions? opts = RoiBulletinCommandOptions.Parse(["--quarter", "Q1-2026", "--bogus"], out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--bogus");
    }
}
