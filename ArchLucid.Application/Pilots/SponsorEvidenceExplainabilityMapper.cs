using ArchLucid.Contracts.Pilots;
using ArchLucid.Decisioning.Findings;

namespace ArchLucid.Application.Pilots;

internal static class SponsorEvidenceExplainabilityMapper
{
    public static ExplainabilityTraceCompletenessPack ToContract(TraceCompletenessSummary summary)
    {
        if (summary is null)
            throw new ArgumentNullException(nameof(summary));

        return new ExplainabilityTraceCompletenessPack
        {
            TotalFindings = summary.TotalFindings,
            OverallCompletenessRatio = summary.OverallCompletenessRatio,
            ByEngine = summary.ByEngine
                .Select(e => new ExplainabilityTraceEngineCompletenessPack
                {
                    EngineType = e.EngineType,
                    FindingCount = e.FindingCount,
                    CompletenessRatio = e.CompletenessRatio,
                    GraphNodeIdsPopulatedCount = e.GraphNodeIdsPopulatedCount,
                    RulesAppliedPopulatedCount = e.RulesAppliedPopulatedCount,
                    DecisionsTakenPopulatedCount = e.DecisionsTakenPopulatedCount,
                    AlternativePathsPopulatedCount = e.AlternativePathsPopulatedCount,
                    NotesPopulatedCount = e.NotesPopulatedCount
                })
                .ToList()
        };
    }
}
