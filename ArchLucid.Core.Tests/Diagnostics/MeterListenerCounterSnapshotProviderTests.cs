using System.Diagnostics;

using ArchLucid.Core.Diagnostics;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Diagnostics;

/// <summary>
///     Unit tests for <see cref="MeterListenerCounterSnapshotProvider" /> — the host-singleton listener that
///     powers the <c>/why-archlucid</c> proof page's process-life counter snapshot.
/// </summary>
[Trait("Suite", "Core")]
public sealed class MeterListenerCounterSnapshotProviderTests
{
    [Fact]
    public void Snapshot_initial_state_is_zero_and_empty()
    {
        using MeterListenerCounterSnapshotProvider sut = new();

        InstrumentationCounterSnapshot snapshot = sut.GetSnapshot();

        snapshot.RunsCreatedTotal.Should().Be(0);
        snapshot.FindingsProducedBySeverity.Should().BeEmpty();
        snapshot.OperatorTaskSuccessByTask.Should().BeEmpty();
    }

    [Fact]
    public void Snapshot_accumulates_runs_created_increments()
    {
        // Touch the static counter before starting the listener so InstrumentPublished fires on Start().
        _ = ArchLucidInstrumentation.RunsCreatedTotal;

        using MeterListenerCounterSnapshotProvider sut = new();

        ArchLucidInstrumentation.RunsCreatedTotal.Add(1);
        ArchLucidInstrumentation.RunsCreatedTotal.Add(4);

        InstrumentationCounterSnapshot snapshot = sut.GetSnapshot();

        snapshot.RunsCreatedTotal.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void Snapshot_groups_findings_by_severity_tag()
    {
        _ = ArchLucidInstrumentation.FindingsProducedTotal;

        using MeterListenerCounterSnapshotProvider sut = new();

        TagList critical = new() { { "severity", "Critical" } };
        TagList high = new() { { "severity", "High" } };

        ArchLucidInstrumentation.FindingsProducedTotal.Add(2, critical);
        ArchLucidInstrumentation.FindingsProducedTotal.Add(3, high);
        ArchLucidInstrumentation.FindingsProducedTotal.Add(1, critical);

        InstrumentationCounterSnapshot snapshot = sut.GetSnapshot();

        snapshot.FindingsProducedBySeverity.Should().ContainKey("Critical").WhoseValue.Should()
            .BeGreaterThanOrEqualTo(3);
        snapshot.FindingsProducedBySeverity.Should().ContainKey("High").WhoseValue.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Snapshot_returns_isolated_copy_of_findings_dictionary()
    {
        _ = ArchLucidInstrumentation.FindingsProducedTotal;

        using MeterListenerCounterSnapshotProvider sut = new();

        TagList medium = new() { { "severity", "Medium" } };

        ArchLucidInstrumentation.FindingsProducedTotal.Add(1, medium);

        InstrumentationCounterSnapshot first = sut.GetSnapshot();

        ArchLucidInstrumentation.FindingsProducedTotal.Add(1, medium);

        InstrumentationCounterSnapshot second = sut.GetSnapshot();

        // The first snapshot must be unaffected by later increments — verifies dictionary copy semantics.
        first.FindingsProducedBySeverity["Medium"].Should().BeLessThan(second.FindingsProducedBySeverity["Medium"]);
    }

    [Fact]
    public void Findings_with_missing_severity_tag_bucket_into_unknown()
    {
        _ = ArchLucidInstrumentation.FindingsProducedTotal;

        using MeterListenerCounterSnapshotProvider sut = new();

        ArchLucidInstrumentation.FindingsProducedTotal.Add(7);

        InstrumentationCounterSnapshot snapshot = sut.GetSnapshot();

        snapshot.FindingsProducedBySeverity.Should().ContainKey("unknown").WhoseValue.Should()
            .BeGreaterThanOrEqualTo(7);
    }

    [Fact]
    public void Snapshot_groups_operator_task_success_by_task_tag()
    {
        _ = ArchLucidInstrumentation.OperatorTaskSuccessTotal;

        using MeterListenerCounterSnapshotProvider sut = new();

        ArchLucidInstrumentation.RecordOperatorTaskSuccess("first_run_committed");
        ArchLucidInstrumentation.RecordOperatorTaskSuccess("first_session_completed");
        ArchLucidInstrumentation.RecordOperatorTaskSuccess("first_run_committed");

        InstrumentationCounterSnapshot snapshot = sut.GetSnapshot();

        snapshot.OperatorTaskSuccessByTask.Should().ContainKey("first_run_committed").WhoseValue.Should()
            .BeGreaterThanOrEqualTo(2);
        snapshot.OperatorTaskSuccessByTask.Should().ContainKey("first_session_completed").WhoseValue.Should()
            .BeGreaterThanOrEqualTo(1);
    }
}
