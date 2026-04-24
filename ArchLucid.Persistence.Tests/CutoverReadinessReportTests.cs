using ArchLucid.Persistence.Coordination.Backfill;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Unit tests for <see cref="CutoverReadinessReport" /> and <see cref="CutoverSliceReadiness" />
///     model logic (computed properties, aggregations, edge cases).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CutoverReadinessReportTests
{
    // ── CutoverSliceReadiness ──────────────────────────────────────

    [Fact]
    public void Slice_AllHeadersHaveChildren_IsReady()
    {
        CutoverSliceReadiness slice = new()
        {
            SliceName = "ContextSnapshot.CanonicalObjects", TotalHeaderRows = 100, HeadersWithRelationalRows = 100
        };

        slice.IsReady.Should().BeTrue();
        slice.HeadersMissingRelationalRows.Should().Be(0);
    }

    [Fact]
    public void Slice_SomeHeadersMissing_IsNotReady()
    {
        CutoverSliceReadiness slice = new()
        {
            SliceName = "GoldenManifest.Assumptions", TotalHeaderRows = 50, HeadersWithRelationalRows = 42
        };

        slice.IsReady.Should().BeFalse();
        slice.HeadersMissingRelationalRows.Should().Be(8);
    }

    [Fact]
    public void Slice_ZeroHeaders_IsReady()
    {
        CutoverSliceReadiness slice = new()
        {
            SliceName = "FindingsSnapshot.Findings", TotalHeaderRows = 0, HeadersWithRelationalRows = 0
        };

        slice.IsReady.Should().BeTrue();
        slice.HeadersMissingRelationalRows.Should().Be(0);
    }

    [Fact]
    public void Slice_AllHeadersMissing_ReportsCorrectCount()
    {
        CutoverSliceReadiness slice = new()
        {
            SliceName = "ArtifactBundle.Artifacts", TotalHeaderRows = 25, HeadersWithRelationalRows = 0
        };

        slice.IsReady.Should().BeFalse();
        slice.HeadersMissingRelationalRows.Should().Be(25);
    }

    // ── CutoverReadinessReport ─────────────────────────────────────

    [Fact]
    public void Report_AllSlicesReady_IsFullyReady()
    {
        CutoverReadinessReport report = new()
        {
            Slices =
            [
                CreateSlice("ContextSnapshot.CanonicalObjects", 10, 10),
                CreateSlice("ContextSnapshot.Warnings", 10, 10),
                CreateSlice("GraphSnapshot.Nodes", 5, 5)
            ]
        };

        report.IsFullyReady.Should().BeTrue();
        report.SlicesNotReady.Should().BeEmpty();
    }

    [Fact]
    public void Report_OneSliceNotReady_IsNotFullyReady()
    {
        CutoverReadinessReport report = new()
        {
            Slices =
            [
                CreateSlice("ContextSnapshot.CanonicalObjects", 10, 10),
                CreateSlice("ContextSnapshot.Warnings", 10, 7),
                CreateSlice("GraphSnapshot.Nodes", 5, 5)
            ]
        };

        report.IsFullyReady.Should().BeFalse();
        report.SlicesNotReady.Should().HaveCount(1);
        report.SlicesNotReady[0].SliceName.Should().Be("ContextSnapshot.Warnings");
    }

    [Fact]
    public void Report_EmptySlices_IsFullyReady()
    {
        CutoverReadinessReport report = new() { Slices = [] };

        report.IsFullyReady.Should().BeTrue();
        report.SlicesNotReady.Should().BeEmpty();
        report.TotalHeaderRows.Should().Be(0);
    }

    [Fact]
    public void Report_TotalHeaderRows_DeduplicatesByEntityType()
    {
        CutoverReadinessReport report = new()
        {
            Slices =
            [
                CreateSlice("ContextSnapshot.CanonicalObjects", 10, 10),
                CreateSlice("ContextSnapshot.Warnings", 10, 10),
                CreateSlice("ContextSnapshot.Errors", 10, 10),
                CreateSlice("ContextSnapshot.SourceHashes", 10, 10),
                CreateSlice("GraphSnapshot.Nodes", 5, 5),
                CreateSlice("GraphSnapshot.Edges", 5, 5)
            ]
        };

        // 10 (ContextSnapshot, deduplicated) + 5 (GraphSnapshot, deduplicated) = 15
        report.TotalHeaderRows.Should().Be(15);
    }

    [Fact]
    public void Report_MultipleSlicesNotReady_ListsAll()
    {
        CutoverReadinessReport report = new()
        {
            Slices =
            [
                CreateSlice("ContextSnapshot.CanonicalObjects", 10, 10),
                CreateSlice("GraphSnapshot.Nodes", 5, 3),
                CreateSlice("FindingsSnapshot.Findings", 20, 18),
                CreateSlice("ArtifactBundle.Artifacts", 8, 8)
            ]
        };

        report.IsFullyReady.Should().BeFalse();
        report.SlicesNotReady.Should().HaveCount(2);

        List<string> notReadyNames = report.SlicesNotReady.Select(static s => s.SliceName).ToList();
        notReadyNames.Should().Contain("GraphSnapshot.Nodes");
        notReadyNames.Should().Contain("FindingsSnapshot.Findings");
    }

    [Fact]
    public void Report_AllZeroHeaders_IsFullyReady()
    {
        CutoverReadinessReport report = new()
        {
            Slices =
            [
                CreateSlice("ContextSnapshot.CanonicalObjects", 0, 0),
                CreateSlice("GraphSnapshot.Nodes", 0, 0),
                CreateSlice("FindingsSnapshot.Findings", 0, 0),
                CreateSlice("GoldenManifest.Assumptions", 0, 0),
                CreateSlice("ArtifactBundle.Artifacts", 0, 0)
            ]
        };

        report.IsFullyReady.Should().BeTrue();
        report.TotalHeaderRows.Should().Be(0);
    }

    // ── Representative full-coverage scenario ──────────────────────

    [Fact]
    public void Report_FullPipeline_RealisticScenario()
    {
        CutoverReadinessReport report = new()
        {
            Slices =
            [
                CreateSlice("ContextSnapshot.CanonicalObjects", 200, 200),
                CreateSlice("ContextSnapshot.Warnings", 200, 200),
                CreateSlice("ContextSnapshot.Errors", 200, 200),
                CreateSlice("ContextSnapshot.SourceHashes", 200, 195),
                CreateSlice("GraphSnapshot.Nodes", 200, 200),
                CreateSlice("GraphSnapshot.Edges", 200, 200),
                CreateSlice("GraphSnapshot.Warnings", 200, 200),
                CreateSlice("GraphSnapshot.EdgeProperties", 200, 180),
                CreateSlice("FindingsSnapshot.Findings", 200, 200),
                CreateSlice("GoldenManifest.Assumptions", 150, 150),
                CreateSlice("GoldenManifest.Warnings", 150, 150),
                CreateSlice("GoldenManifest.Decisions", 150, 150),
                CreateSlice("GoldenManifest.Provenance", 150, 148),
                CreateSlice("ArtifactBundle.Artifacts", 100, 100)
            ]
        };

        report.IsFullyReady.Should().BeFalse();
        report.SlicesNotReady.Should().HaveCount(3);

        // ContextSnapshot.SourceHashes: 5 missing
        CutoverSliceReadiness hashSlice =
            report.SlicesNotReady.First(static s => s.SliceName == "ContextSnapshot.SourceHashes");
        hashSlice.HeadersMissingRelationalRows.Should().Be(5);

        // GraphSnapshot.EdgeProperties: 20 missing
        CutoverSliceReadiness edgeSlice =
            report.SlicesNotReady.First(static s => s.SliceName == "GraphSnapshot.EdgeProperties");
        edgeSlice.HeadersMissingRelationalRows.Should().Be(20);

        // GoldenManifest.Provenance: 2 missing
        CutoverSliceReadiness provSlice =
            report.SlicesNotReady.First(static s => s.SliceName == "GoldenManifest.Provenance");
        provSlice.HeadersMissingRelationalRows.Should().Be(2);

        // Total headers deduplicated: 200 (Context) + 200 (Graph) + 200 (Findings) + 150 (Golden) + 100 (Artifact)
        report.TotalHeaderRows.Should().Be(850);
    }

    private static CutoverSliceReadiness CreateSlice(string name, int total, int withRelational)
    {
        return new CutoverSliceReadiness
        {
            SliceName = name, TotalHeaderRows = total, HeadersWithRelationalRows = withRelational
        };
    }
}
