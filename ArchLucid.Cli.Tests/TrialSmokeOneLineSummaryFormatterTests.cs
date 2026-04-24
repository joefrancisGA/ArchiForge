using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class TrialSmokeOneLineSummaryFormatterTests
{
    private const string BaseUrl = "https://staging.archlucid.com";

    [Fact]
    public void Format_AllPassed_EmitsPassWithCorrelationAndTenantAndWelcomeRun()
    {
        TrialSmokeReport report = new()
        {
            AllPassed = true,
            TenantId = "tenant-1",
            TrialWelcomeRunId = "welcome-1",
            RegistrationCorrelationId = "corr-1",
            Steps = [new TrialSmokeStepResult { Name = "register", Passed = true, Detail = "ok" }],
        };

        string line = TrialSmokeOneLineSummaryFormatter.Format(report, BaseUrl);

        line.Should().Be(
            "PASS host=https://staging.archlucid.com correlation=corr-1 tenant=tenant-1 welcomeRun=welcome-1 failed=<none>");
    }

    [Fact]
    public void Format_FirstStepFailed_EmitsFailWithFailedStepName()
    {
        TrialSmokeReport report = new()
        {
            AllPassed = false,
            RegistrationCorrelationId = "corr-7",
            Steps =
            [
                new TrialSmokeStepResult { Name = "register", Passed = false, Detail = "boom", FailureHint = "look here" },
            ],
        };

        string line = TrialSmokeOneLineSummaryFormatter.Format(report, BaseUrl);

        line.Should().StartWith("FAIL ");
        line.Should().Contain("correlation=corr-7");
        line.Should().Contain("failed=register");
        line.Should().Contain("tenant=<none>");
        line.Should().Contain("welcomeRun=<none>");
    }

    [Fact]
    public void Format_MissingCorrelationId_EmitsAngleBracketNoneToken()
    {
        TrialSmokeReport report = new()
        {
            AllPassed = false,
            Steps = [new TrialSmokeStepResult { Name = "register", Passed = false, Detail = "x" }],
        };

        string line = TrialSmokeOneLineSummaryFormatter.Format(report, BaseUrl);

        line.Should().Contain("correlation=<none>");
    }

    [Fact]
    public void Format_NullReport_Throws()
    {
        Action act = () => TrialSmokeOneLineSummaryFormatter.Format(null!, BaseUrl);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Format_NullBaseUrl_Throws()
    {
        Action act = () => TrialSmokeOneLineSummaryFormatter.Format(new TrialSmokeReport(), null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
