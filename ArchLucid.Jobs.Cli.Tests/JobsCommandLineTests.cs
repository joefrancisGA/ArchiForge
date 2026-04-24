using ArchLucid.Host.Core.Jobs;

using FluentAssertions;

namespace ArchLucid.Jobs.Cli.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class JobsCommandLineTests
{
    [Fact]
    public void TryParseJobName_false_without_job_flag()
    {
        bool ok = JobsCommandLine.TryParseJobName(["--help"], out string? name, out string? err);

        ok.Should().BeFalse();
        name.Should().BeNull();
        err.Should().Contain("Required");
    }

    [Fact]
    public void TryParseJobName_true_with_job_and_name()
    {
        bool ok = JobsCommandLine.TryParseJobName(["--job", ArchLucidJobNames.AdvisoryScan], out string? name,
            out string? err);

        ok.Should().BeTrue();
        name.Should().Be(ArchLucidJobNames.AdvisoryScan);
        err.Should().BeNull();
    }

    [Fact]
    public void TryParseJobName_false_when_args_empty()
    {
        bool ok = JobsCommandLine.TryParseJobName([], out string? name, out string? err);

        ok.Should().BeFalse();
        name.Should().BeNull();
        err.Should().Contain("Required");
    }

    [Fact]
    public void TryParseJobName_false_when_job_flag_without_value()
    {
        bool ok = JobsCommandLine.TryParseJobName(["--job"], out string? name, out string? err);

        ok.Should().BeFalse();
        name.Should().BeNull();
        err.Should().Contain("Expected a job name");
    }

    [Fact]
    public void TryParseJobName_false_when_job_name_whitespace_only()
    {
        bool ok = JobsCommandLine.TryParseJobName(["--job", "   "], out string? name, out string? err);

        ok.Should().BeFalse();
        name.Should().BeEmpty();
        err.Should().Contain("empty");
    }

    [Fact]
    public void TryParseJobName_true_when_job_preceded_by_other_tokens()
    {
        bool ok = JobsCommandLine.TryParseJobName(
            ["verbose", "--job", ArchLucidJobNames.AdvisoryScan],
            out string? name,
            out string? err);

        ok.Should().BeTrue();
        name.Should().Be(ArchLucidJobNames.AdvisoryScan);
        err.Should().BeNull();
    }
}
