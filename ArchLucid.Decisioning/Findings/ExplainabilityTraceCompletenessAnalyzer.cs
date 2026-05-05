using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Scores how thoroughly each finding's <see cref="ExplainabilityTrace" /> is populated, and aggregates by engine
///     type.
/// </summary>
public static class ExplainabilityTraceCompletenessAnalyzer
{
    /// <summary>Analyzes a single finding's trace; treats null <see cref="Finding.Trace" /> as empty.</summary>
    public static TraceCompletenessScore AnalyzeFinding(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        // Trace is typed non-null on Finding, but object initializers, tests (Trace = null!), and some payloads leave it null at runtime.
        ExplainabilityTrace? trace = finding.Trace;

        bool hasGraph = ListHasMeaningfulContent(trace?.GraphNodeIdsExamined);
        bool hasRules = ListHasMeaningfulContent(trace?.RulesApplied);
        bool hasDecisions = ListHasMeaningfulContent(trace?.DecisionsTaken);
        bool hasAlt = ListHasMeaningfulContent(trace?.AlternativePathsConsidered);
        bool hasNotes = ListHasMeaningfulContent(trace?.Notes);
        bool hasCitations = ListHasMeaningfulContent(trace?.Citations);

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

        if (hasCitations)
            populated++;

        List<string> missing = [];

        if (!hasGraph)
            missing.Add("Graph nodes examined");

        if (!hasRules)
            missing.Add("Rules applied");

        if (!hasDecisions)
            missing.Add("Decisions taken");

        if (!hasAlt)
            missing.Add("Alternative paths considered");

        if (!hasNotes)
            missing.Add("Notes");

        if (!hasCitations)
            missing.Add("Citations");

        return new TraceCompletenessScore
        {
            FindingId = finding.FindingId,
            EngineType = finding.EngineType,
            HasGraphNodeIds = hasGraph,
            HasRulesApplied = hasRules,
            HasDecisionsTaken = hasDecisions,
            HasAlternativePaths = hasAlt,
            HasNotes = hasNotes,
            HasCitations = hasCitations,
            PopulatedFieldCount = populated,
            CompletenessRatio = populated / 6.0,
            MissingTraceFields = missing,
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
                    NotesPopulatedCount = list.Count(x => x.HasNotes),
                    CitationsPopulatedCount = list.Count(x => x.HasCitations)
                };
            })
            .ToList();

        return new TraceCompletenessSummary { TotalFindings = findings.Count, OverallCompletenessRatio = overall, ByEngine = byEngine };
    }

    private static bool ListHasMeaningfulContent(IReadOnlyList<string>? list)
    {
        if (list is null || list.Count == 0)
            return false;

        return list.Any(s => !string.IsNullOrWhiteSpace(s));
    }
}
