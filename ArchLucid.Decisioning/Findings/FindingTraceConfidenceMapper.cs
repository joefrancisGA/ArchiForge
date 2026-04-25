using ArchLucid.Contracts.Explanation;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Builds <see cref="FindingTraceConfidenceDto" /> rows from a findings snapshot (no I/O).
/// </summary>
public static class FindingTraceConfidenceMapper
{
    public static List<FindingTraceConfidenceDto> FromSnapshot(FindingsSnapshot? snapshot)
    {
        if (snapshot?.Findings is not { Count: > 0 } list)
            return [];

        List<FindingTraceConfidenceDto> rows = new(list.Count);

        foreach (Finding f in list)
        {
            TraceCompletenessScore score = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(f);
            FindingExplainabilityEvidence evidence = FindingExplainabilityNarrativeBuilder.BuildEvidence(f);

            rows.Add(
                new FindingTraceConfidenceDto
                {
                    FindingId = f.FindingId,
                    TraceCompletenessRatio = score.CompletenessRatio,
                    TraceConfidenceLabel = TraceConfidenceLabels.FromCompletenessRatio(score.CompletenessRatio),
                    RuleId = evidence.RuleId,
                    EvidenceRefCount = evidence.EvidenceRefs.Count
                });
        }

        return rows;
    }
}
