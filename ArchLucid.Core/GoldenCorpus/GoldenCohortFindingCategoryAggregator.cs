using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Findings;

namespace ArchLucid.Core.GoldenCorpus;

/// <summary>Builds the golden-cohort finding category multiset from persisted <see cref="AgentResult"/> rows.</summary>
public static class GoldenCohortFindingCategoryAggregator
{
    /// <summary>Distinct non-empty <see cref="ArchitectureFinding.Category"/> values across all results (ordinal case-sensitive sort).</summary>
    public static SortedSet<string> DistinctCategories(IEnumerable<AgentResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        SortedSet<string> set = new(StringComparer.Ordinal);

        foreach (AgentResult result in results)
        {
            foreach (ArchitectureFinding finding in result.Findings.Where(finding => !string.IsNullOrWhiteSpace(finding.Category)))
                set.Add(finding.Category.Trim());
        }

        return set;
    }
}
