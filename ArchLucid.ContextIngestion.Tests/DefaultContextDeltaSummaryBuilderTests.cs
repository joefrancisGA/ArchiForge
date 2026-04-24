using ArchLucid.ContextIngestion.Models;
using ArchLucid.ContextIngestion.Summaries;

using FluentAssertions;

namespace ArchLucid.ContextIngestion.Tests;

/// <summary>
///     Contract tests for <see cref="DefaultContextDeltaSummaryBuilder" />.
///     Covers: empty batch, single-type, multi-type deterministic ordering, baseline clauses
///     (first connector with/without previous snapshot), and whitespace/null <c>baseSummary</c> fallback.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class DefaultContextDeltaSummaryBuilderTests
{
    private readonly DefaultContextDeltaSummaryBuilder _sut = new();

    private static CanonicalObject MakeObject(string objectType, string name = "x")
    {
        return new CanonicalObject
        {
            ObjectType = objectType,
            Name = name,
            SourceType = "Test",
            SourceId = "t",
            Properties = new Dictionary<string, string> { ["text"] = name }
        };
    }

    // ── Empty batch ────────────────────────────────────────────────

    [Fact]
    public void BuildSegment_EmptyBatch_ShowsNoneBreakdown()
    {
        NormalizedContextBatch batch = new();

        string line = _sut.BuildSegment("connector-x", "summary", batch, null, false);

        line.Should().Contain("0 produced (none)");
    }

    // ── Single-type batch ──────────────────────────────────────────

    [Fact]
    public void BuildSegment_SingleType_ShowsTypeTimesCount()
    {
        NormalizedContextBatch batch = new()
        {
            CanonicalObjects = [MakeObject("Requirement", "a"), MakeObject("Requirement", "b")]
        };

        string line = _sut.BuildSegment("inline", "req summary", batch, null, false);

        line.Should().Contain("2 produced (Requirement×2)");
    }

    // ── Multi-type batch — deterministic ordering ──────────────────

    [Fact]
    public void BuildSegment_MultipleTypes_OrderedByOrdinalKey()
    {
        NormalizedContextBatch batch = new()
        {
            CanonicalObjects =
            [
                MakeObject("TopologyResource"),
                MakeObject("PolicyControl"),
                MakeObject("PolicyControl"),
                MakeObject("Requirement")
            ]
        };

        string line = _sut.BuildSegment("mixed", "multi", batch, null, false);

        line.Should().Contain("4 produced");

        int policyIdx = line.IndexOf("PolicyControl×2", StringComparison.Ordinal);
        int reqIdx = line.IndexOf("Requirement×1", StringComparison.Ordinal);
        int topoIdx = line.IndexOf("TopologyResource×1", StringComparison.Ordinal);

        policyIdx.Should().BeGreaterThanOrEqualTo(0);
        reqIdx.Should().BeGreaterThanOrEqualTo(0);
        topoIdx.Should().BeGreaterThanOrEqualTo(0);

        policyIdx.Should().BeLessThan(reqIdx, "Ordinal: PolicyControl < Requirement");
        reqIdx.Should().BeLessThan(topoIdx, "Ordinal: Requirement < TopologyResource");
    }

    // ── Baseline clause: first connector, no previous snapshot ─────

    [Fact]
    public void BuildSegment_FirstConnector_NoPrevious_ShowsNoPriorSnapshotBaseline()
    {
        NormalizedContextBatch batch = new();

        string line = _sut.BuildSegment("static", "desc", batch, null, true);

        line.Should().Contain("[baseline: no prior project snapshot]");
    }

    // ── Baseline clause: first connector, with previous snapshot ───

    [Fact]
    public void BuildSegment_FirstConnector_WithPrevious_ShowsPriorObjectCount()
    {
        ContextSnapshot previous = new()
        {
            CanonicalObjects =
            [
                MakeObject("Requirement", "old1"),
                MakeObject("Requirement", "old2"),
                MakeObject("TopologyResource", "old3")
            ]
        };

        NormalizedContextBatch batch = new() { CanonicalObjects = [MakeObject("Requirement")] };

        string line = _sut.BuildSegment("inline-requirements", "Initial inline", batch, previous, true);

        line.Should().Contain("Initial inline");
        line.Should().Contain("prior snapshot had 3 canonical object(s)");
        line.Should().Contain("Requirement×1");
    }

    // ── Baseline clause: not first connector ───────────────────────

    [Fact]
    public void BuildSegment_NotFirstConnector_OmitsBaseline()
    {
        NormalizedContextBatch batch = new();

        string line = _sut.BuildSegment("policy-reference", "Updated policy", batch, null, false);

        line.Should().NotContain("baseline");
        line.Should().Contain("0 produced");
    }

    // ── baseSummary whitespace fallback ─────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void BuildSegment_WhitespaceOrNullBaseSummary_FallsBackToConnectorType(string? baseSummary)
    {
        NormalizedContextBatch batch = new() { CanonicalObjects = [MakeObject("Requirement")] };

        string line = _sut.BuildSegment("my-connector", baseSummary!, batch, null, false);

        line.Should().StartWith("my-connector");
    }

    // ── baseSummary with leading/trailing whitespace is trimmed ─────

    [Fact]
    public void BuildSegment_BaseSummaryWithWhitespace_IsTrimmed()
    {
        NormalizedContextBatch batch = new() { CanonicalObjects = [MakeObject("Requirement")] };

        string line = _sut.BuildSegment("conn", "  padded summary  ", batch, null, false);

        line.Should().StartWith("padded summary");
    }
}
