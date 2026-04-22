using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Findings;

namespace ArchLucid.Application.GoldenCohort;

/// <summary>Builds the golden-cohort finding category multiset from persisted <see cref="AgentResult"/> rows.</summary>
public static class GoldenCohortFindingCategoryAggregator
{
    /// <summary>Distinct non-empty <see cref="ArchitectureFinding.Category"/> values across all results (ordinal case-sensitive sort).</summary>
    public static SortedSet<string> DistinctCategories(IEnumerable<AgentResult> results)
    {
        if (results is null)
            throw new ArgumentNullException(nameof(results));

        SortedSet<string> set = new(StringComparer.Ordinal);

        foreach (AgentResult result in results)
        {
            if (result?.Findings is null)
                continue;

            foreach (ArchitectureFinding finding in result.Findings)
            {
                if (finding is null)
                    continue;

                if (string.IsNullOrWhiteSpace(finding.Category))
                    continue;

                set.Add(finding.Category.Trim());
            }
        }

        return set;
    }
}
