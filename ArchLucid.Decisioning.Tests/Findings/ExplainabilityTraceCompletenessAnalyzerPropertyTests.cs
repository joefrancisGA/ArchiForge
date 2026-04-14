using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Models;

using FsCheck;
using FsCheck.Xunit;

namespace ArchLucid.Decisioning.Tests.Findings;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ExplainabilityTraceCompletenessAnalyzerPropertyTests
{
    [Property(MaxTest = 200)]
    public Property AnalyzeFinding_completeness_ratio_matches_populated_field_count()
    {
        return Prop.ForAll(TraceArraysArb(), t =>
        {
            Finding finding = MakeFinding(t.Graph, t.Rules, t.Decisions, t.Alt, t.Notes);
            TraceCompletenessScore score = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(finding);
            double expected = score.PopulatedFieldCount / 5.0;

            return score.CompletenessRatio == expected
                   && score.PopulatedFieldCount >= 0
                   && score.PopulatedFieldCount <= 5;
        });
    }

    [Property(MaxTest = 100)]
    public Property AnalyzeSnapshot_overall_ratio_matches_per_finding_average()
    {
        Arbitrary<List<Finding>> findingsArb = Arb.From(
            Gen.Choose(0, 35).SelectMany(ListOfFindings));

        return Prop.ForAll(findingsArb, findings =>
        {
            FindingsSnapshot snapshot = new() { Findings = findings };
            TraceCompletenessSummary summary = ExplainabilityTraceCompletenessAnalyzer.AnalyzeSnapshot(snapshot);

            if (findings.Count == 0)
            {
                return summary.TotalFindings == 0
                       && summary.OverallCompletenessRatio == 0.0
                       && summary.ByEngine.Count == 0;
            }

            double manualAverage = findings
                .Select(ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding)
                .Average(s => s.CompletenessRatio);

            return Math.Abs(summary.OverallCompletenessRatio - manualAverage) < 1e-9;
        });
    }

    private static Arbitrary<(string[] Graph, string[] Rules, string[] Decisions, string[] Alt, string[] Notes)> TraceArraysArb()
    {
        return Arb.From(
            from graph in Arb.Default.Array<string>().Generator
            from rules in Arb.Default.Array<string>().Generator
            from decisions in Arb.Default.Array<string>().Generator
            from alt in Arb.Default.Array<string>().Generator
            from notes in Arb.Default.Array<string>().Generator
            select (graph, rules, decisions, alt, notes));
    }

    private static Gen<List<Finding>> ListOfFindings(int count)
    {
        if (count <= 0)
        {
            return Gen.Constant(new List<Finding>());
        }

        return
            from head in FindingGen()
            from tail in ListOfFindings(count - 1)
            select tail.Prepend(head).ToList();
    }

    private static Gen<Finding> FindingGen()
    {
        return
            from graph in Arb.Default.Array<string>().Generator
            from rules in Arb.Default.Array<string>().Generator
            from decisions in Arb.Default.Array<string>().Generator
            from alt in Arb.Default.Array<string>().Generator
            from notes in Arb.Default.Array<string>().Generator
            select MakeFinding(graph, rules, decisions, alt, notes);
    }

    private static Finding MakeFinding(
        string[] graph,
        string[] rules,
        string[] decisions,
        string[] alt,
        string[] notes)
    {
        return new Finding
        {
            FindingType = "fixture",
            Category = "fixture",
            EngineType = "FixtureEngine",
            Title = "t",
            Rationale = "r",
            Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = graph.ToList(),
                RulesApplied = rules.ToList(),
                DecisionsTaken = decisions.ToList(),
                AlternativePathsConsidered = alt.ToList(),
                Notes = notes.ToList(),
            },
        };
    }
}
