using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class TrialSmokeCommandOptionsTests
{
    [Fact]
    public void Parse_RequiresOrgAndEmail()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse([], out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--org");
    }

    [Fact]
    public void Parse_RequiresEmail()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(["--org", "Acme"], out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--email");
    }

    [Fact]
    public void Parse_AcceptsMinimumRequiredFields()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            ["--org", "Acme", "--email", "ops@example.com"],
            out string? error);

        error.Should().BeNull();
        opts.Should().NotBeNull();
        opts!.OrganizationName.Should().Be("Acme");
        opts.AdminEmail.Should().Be("ops@example.com");
        opts.AdminDisplayName.Should().Be(TrialSmokeCommandOptions.DefaultDisplayName);
        opts.BaselineReviewCycleHours.Should().BeNull();
        opts.BaselineReviewCycleSource.Should().BeNull();
        opts.SkipPilotRunDeltas.Should().BeFalse();
    }

    [Fact]
    public void Parse_AcceptsBaselineHoursAndSource()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            [
                "--org", "Acme",
                "--email", "ops@example.com",
                "--baseline-hours", "16.5",
                "--baseline-source", "team estimate",
                "--display-name", "Ops User",
                "--api-base-url", "https://staging.archlucid.com",
            ],
            out string? error);

        error.Should().BeNull();
        opts!.BaselineReviewCycleHours.Should().Be(16.5m);
        opts.BaselineReviewCycleSource.Should().Be("team estimate");
        opts.AdminDisplayName.Should().Be("Ops User");
        opts.ApiBaseUrl.Should().Be("https://staging.archlucid.com");
    }

    [Fact]
    public void Parse_RejectsBaselineSourceWithoutHours()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            ["--org", "Acme", "--email", "ops@example.com", "--baseline-source", "team estimate"],
            out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--baseline-source").And.Contain("--baseline-hours");
    }

    [Fact]
    public void Parse_RejectsNonPositiveBaselineHours()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            ["--org", "Acme", "--email", "ops@example.com", "--baseline-hours", "0"],
            out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--baseline-hours");
    }

    [Fact]
    public void Parse_FlagWithoutValueReturnsError()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            ["--org", "Acme", "--email"],
            out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--email");
    }

    [Fact]
    public void Parse_UnknownFlagReturnsError()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            ["--org", "Acme", "--email", "ops@example.com", "--bogus"],
            out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--bogus");
    }

    [Fact]
    public void Parse_SkipPilotRunDeltasFlagIsBoolean()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            ["--org", "Acme", "--email", "ops@example.com", "--skip-pilot-run-deltas"],
            out string? error);

        error.Should().BeNull();
        opts!.SkipPilotRunDeltas.Should().BeTrue();
    }

    [Fact]
    public void Parse_StagingFlag_AutoSetsApiBaseUrlAndOneLineOutput()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            ["--staging", "--org", "Acme", "--email", "ops@example.com"],
            out string? error);

        error.Should().BeNull();
        opts!.TargetStaging.Should().BeTrue();
        opts.OneLineOutput.Should().BeTrue();
        opts.ApiBaseUrl.Should().Be(TrialSmokeCommandOptions.StagingApiBaseUrl);
    }

    [Fact]
    public void Parse_StagingFlag_RejectsConflictingApiBaseUrl()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            [
                "--staging",
                "--api-base-url", "http://localhost:5128",
                "--org", "Acme",
                "--email", "ops@example.com",
            ],
            out string? error);

        opts.Should().BeNull();
        error.Should().Contain("--staging").And.Contain("--api-base-url");
    }

    [Fact]
    public void Parse_StagingFlag_AcceptsRedundantStagingApiBaseUrl()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            [
                "--staging",
                "--api-base-url", TrialSmokeCommandOptions.StagingApiBaseUrl,
                "--org", "Acme",
                "--email", "ops@example.com",
            ],
            out string? error);

        error.Should().BeNull();
        opts!.TargetStaging.Should().BeTrue();
        opts.ApiBaseUrl.Should().Be(TrialSmokeCommandOptions.StagingApiBaseUrl);
    }

    [Fact]
    public void Parse_OneLineFlag_StandsAloneWithoutStaging()
    {
        TrialSmokeCommandOptions? opts = TrialSmokeCommandOptions.Parse(
            ["--one-line", "--org", "Acme", "--email", "ops@example.com"],
            out string? error);

        error.Should().BeNull();
        opts!.OneLineOutput.Should().BeTrue();
        opts.TargetStaging.Should().BeFalse();
        opts.ApiBaseUrl.Should().BeNull();
    }
}
