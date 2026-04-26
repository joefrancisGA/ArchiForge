using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Scores how thoroughly each finding's <see cref="ExplainabilityTrace" /> is populated, and aggregates by engine
///     type.
/// </summary>
public static class ExplainabilityTraceCompletenessAnalyzer
{
    private static readonly ExplainabilityTrace s_emptyTrace = new();

    /// <summary>Analyzes a single finding's trace; treats null <see cref="Finding.Trace" /> as empty.</summary>
    public static TraceCompletenessScore AnalyzeFinding(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        ExplainabilityTrace trace = finding.Trace ?? s_emptyTrace;

        bool hasGraph = ListHasMeaningfulContent(trace.GraphNodeIdsExamined);
        bool hasRules = ListHasMeaningfulContent(trace.RulesApplied);
        bool hasDecisions = ListHasMeaningfulContent(trace.DecisionsTaken);
        bool hasAlt = ListHasMeaningfulContent(trace.AlternativePathsConsidered);
        bool hasNotes = ListHasMeaningfulContent(trace.Notes);

        int populated = 0;

        if (hasGraph)

            populated++;


        if (hasRules)

            populated++;


        if (hasDecisions)

            populated++;


        if (hasAlt)
            populated++;

        if (hasNotes)
            populated++;

        return new TraceCompletenessScore
        {
            FindingId = finding.FindingId,
            EngineType = finding.EngineType,
            HasGraphNodeIds = hasGraph,
            HasRulesApplied = hasRules,
            HasDecisionsTaken = hasDecisions,
            HasAlternativePaths = hasAlt,
            HasNotes = hasNotes,
            PopulatedFieldCount = populated,
            CompletenessRatio = populated / 5.0
        };
    }

    /// <summary>Aggregates scores for all findings in the snapshot, grouped by <see cref="Finding.EngineType" />.</summary>
    public static TraceCompletenessSummary AnalyzeSnapshot(FindingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        List<Finding> findings = snapshot.Findings;

        if (findings.Count == 0)
            return new TraceCompletenessSummary { TotalFindings = 0, OverallCompletenessRatio = 0.0, ByEngine = [] };

        List<TraceCompletenessScore> scores = findings.Select(AnalyzeFinding).ToList();

        double overall = scores.Average(s => s.CompletenessRatio);

        List<EngineTraceCompleteness> byEngine = scores
            .GroupBy(s => s.EngineType, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                List<TraceCompletenessScore> list = g.ToList();

                return new EngineTraceCompleteness
                {
                    EngineType = g.Key,
                    FindingCount = list.Count,
                    CompletenessRatio = list.Average(x => x.CompletenessRatio),
                    GraphNodeIdsPopulatedCount = list.Count(x => x.HasGraphNodeIds),
                    RulesAppliedPopulatedCount = list.Count(x => x.HasRulesApplied),
                    DecisionsTakenPopulatedCount = list.Count(x => x.HasDecisionsTaken),
                    AlternativePathsPopulatedCount = list.Count(x => x.HasAlternativePaths),
                    NotesPopulatedCount = list.Count(x => x.HasNotes)
                };
            })
            .ToList();

        return new TraceCompletenessSummary
        {
            TotalFindings = findings.Count,
            OverallCompletenessRatio = overall,
            ByEngine = byEngine
        };
    }

    private static bool ListHasMeaningfulContent(IReadOnlyList<string>? list)
    {
        if (list is null || list.Count == 0)
            return false;


        return list.Any(s => !string.IsNullOrWhiteSpace(s));
    }
}
