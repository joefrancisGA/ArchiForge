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
        bool ok = JobsCommandLine.TryParseJobName(["--job", ArchLucidJobNames.AdvisoryScan], out string? name, out string? err);

        ok.Should().BeTrue();
        name.Should().Be(ArchLucidJobNames.AdvisoryScan);
        err.Should().BeNull();
    }
}
