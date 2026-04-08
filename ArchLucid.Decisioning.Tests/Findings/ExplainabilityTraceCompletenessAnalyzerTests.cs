using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Models;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests.Findings;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ExplainabilityTraceCompletenessAnalyzerTests
{
    [Fact]
    public void AnalyzeFinding_fully_populated_trace_has_ratio_one()
    {
        Finding finding = new()
        {
            FindingId = "a",
            EngineType = "test",
            Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = ["n1"],
                RulesApplied = ["r1"],
                DecisionsTaken = ["d1"],
                AlternativePathsConsidered = ["alt"],
                Notes = ["note"],
            },
        };

        TraceCompletenessScore score = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(finding);

        score.PopulatedFieldCount.Should().Be(5);
        score.CompletenessRatio.Should().Be(1.0);
        score.HasGraphNodeIds.Should().BeTrue();
        score.HasRulesApplied.Should().BeTrue();
        score.HasDecisionsTaken.Should().BeTrue();
        score.HasAlternativePaths.Should().BeTrue();
        score.HasNotes.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeFinding_empty_trace_has_ratio_zero()
    {
        Finding finding = new()
        {
            FindingId = "b",
            EngineType = "empty-engine",
            Trace = new ExplainabilityTrace(),
        };

        TraceCompletenessScore score = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(finding);

        score.PopulatedFieldCount.Should().Be(0);
        score.CompletenessRatio.Should().Be(0.0);
    }

    [Fact]
    public void AnalyzeFinding_mixed_trace_three_of_five()
    {
        Finding finding = new()
        {
            FindingId = "c",
            EngineType = "mixed",
            Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = ["x"],
                RulesApplied = ["rule"],
                DecisionsTaken = ["decided"],
            },
        };

        TraceCompletenessScore score = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(finding);

        score.PopulatedFieldCount.Should().Be(3);
        score.CompletenessRatio.Should().BeApproximately(0.6, 0.0001);
    }

    [Fact]
    public void AnalyzeFinding_null_trace_treated_as_empty()
    {
        Finding finding = new()
        {
            FindingId = "d",
            EngineType = "null-trace",
            Trace = null!,
        };

        TraceCompletenessScore score = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(finding);

        score.PopulatedFieldCount.Should().Be(0);
        score.CompletenessRatio.Should().Be(0.0);
    }

    [Fact]
    public void AnalyzeFinding_whitespace_only_lists_do_not_count()
    {
        Finding finding = new()
        {
            FindingId = "e",
            EngineType = "ws",
            Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = ["  ", ""],
                RulesApplied = ["real"],
            },
        };

        TraceCompletenessScore score = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(finding);

        score.HasGraphNodeIds.Should().BeFalse();
        score.HasRulesApplied.Should().BeTrue();
        score.PopulatedFieldCount.Should().Be(1);
    }

    [Fact]
    public void AnalyzeSnapshot_aggregates_by_engine_type()
    {
        FindingsSnapshot snapshot = new()
        {
            Findings =
            [
                new Finding
                {
                    FindingId = "1",
                    EngineType = "alpha",
                    Trace = new ExplainabilityTrace { DecisionsTaken = ["a"] },
                },
                new Finding
                {
                    FindingId = "2",
                    EngineType = "alpha",
                    Trace = new ExplainabilityTrace
                    {
                        GraphNodeIdsExamined = ["n"],
                        RulesApplied = ["r"],
                        DecisionsTaken = ["d"],
                        AlternativePathsConsidered = ["x"],
                        Notes = ["y"],
                    },
                },
                new Finding
                {
                    FindingId = "3",
                    EngineType = "beta",
                    Trace = new ExplainabilityTrace { Notes = ["only-notes"] },
                },
            ],
        };

        TraceCompletenessSummary summary = ExplainabilityTraceCompletenessAnalyzer.AnalyzeSnapshot(snapshot);

        summary.TotalFindings.Should().Be(3);
        summary.ByEngine.Should().HaveCount(2);
        summary.ByEngine[0].EngineType.Should().Be("alpha");
        summary.ByEngine[0].FindingCount.Should().Be(2);
        summary.ByEngine[0].DecisionsTakenPopulatedCount.Should().Be(2);
        summary.ByEngine[1].EngineType.Should().Be("beta");
        summary.ByEngine[1].FindingCount.Should().Be(1);
        summary.ByEngine[1].NotesPopulatedCount.Should().Be(1);
        summary.OverallCompletenessRatio.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AnalyzeSnapshot_zero_findings_returns_empty_by_engine()
    {
        FindingsSnapshot snapshot = new() { Findings = [] };

        TraceCompletenessSummary summary = ExplainabilityTraceCompletenessAnalyzer.AnalyzeSnapshot(snapshot);

        summary.TotalFindings.Should().Be(0);
        summary.OverallCompletenessRatio.Should().Be(0.0);
        summary.ByEngine.Should().BeEmpty();
    }
}
