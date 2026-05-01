using ArchLucid.Contracts.Common;

using FluentAssertions;

using FsCheck.Xunit;

namespace ArchLucid.Application.Tests.Runs;

/// <summary>Lightweight lifecycle enum invariants (complements <see cref="RunLifecycleStatePropertyTests"/>).</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArchitectureRunStatusTransitionPropertyTests
{
    [Property(MaxTest = 50)]
    public void All_enum_values_are_within_documented_range(byte raw)
    {
        if (!Enum.IsDefined(typeof(ArchitectureRunStatus), (ArchitectureRunStatus)raw))
            return;

        ArchitectureRunStatus status = (ArchitectureRunStatus)raw;
        int v = (int)status;

        v.Should().BeInRange(1, 7);
    }

    [SkippableFact]
    public void Terminal_statuses_for_commit_include_Committed_and_Failed()
    {
        ArchitectureRunStatus.Committed.Should().Be((ArchitectureRunStatus)5);
        ArchitectureRunStatus.Failed.Should().Be((ArchitectureRunStatus)6);
    }

    [SkippableFact]
    public void Pre_commit_eligible_statuses_are_ordered_before_Committed()
    {
        ((int)ArchitectureRunStatus.ReadyForCommit).Should().BeLessThan((int)ArchitectureRunStatus.Committed);
        ((int)ArchitectureRunStatus.TasksGenerated).Should().BeLessThan((int)ArchitectureRunStatus.Committed);
    }
}
