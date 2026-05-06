using ArchLucid.Core.Comparison;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Comparison;

/// <summary>
///     <see cref="IComparisonService" /> implementation: keyed merges over decisions, requirement coverage, security
///     controls, topology resources, and optional max monthly cost.
/// </summary>
/// <remarks>
///     Decision keys prefer <see cref="ResolvedArchitectureDecision.DecisionId" /> when set, else <c>Category::Title</c>.
///     Security controls key on <c>ControlId|ControlName</c> when <c>ControlId</c> is non-empty.
///     Requirement names are matched case-insensitively; several string comparisons for options/status use ordinal rules
///     as implemented per section.
/// </remarks>
public sealed class ComparisonService : IComparisonService
{
    /// <inheritdoc />
    public ComparisonResult Compare(ManifestDocument baseM, ManifestDocument targetM)
    {
        ArgumentNullException.ThrowIfNull(baseM);
        ArgumentNullException.ThrowIfNull(targetM);
        ComparisonResult result = new() { BaseRunId = baseM.RunId, TargetRunId = targetM.RunId };

        CompareDecisions(baseM, targetM, result);
        CompareRequirements(baseM, targetM, result);
        CompareSecurity(baseM, targetM, result);
        CompareTopology(baseM, targetM, result);
        CompareCost(baseM, targetM, result);
        BuildSummary(result);
        result.TotalDeltaCount =
            result.DecisionChanges.Count
            + result.RequirementChanges.Count
            + result.SecurityChanges.Count
            + result.TopologyChanges.Count
            + result.CostChanges.Count;

        return result;
    }

    private static string DecisionKey(ResolvedArchitectureDecision d)
    {
        return !string.IsNullOrWhiteSpace(d.DecisionId) ? d.DecisionId : $"{d.Category}::{d.Title}";
    }

    private static void CompareDecisions(ManifestDocument baseM, ManifestDocument targetM, ComparisonResult result)
    {
        Dictionary<string, ResolvedArchitectureDecision> baseMap =
            baseM.Decisions.GroupBy(DecisionKey).ToDictionary(g => g.Key, g => g.First());
        Dictionary<string, ResolvedArchitectureDecision> targetMap =
            targetM.Decisions.GroupBy(DecisionKey).ToDictionary(g => g.Key, g => g.First());

        foreach (string key in baseMap.Keys.Union(targetMap.Keys))
        {
            baseMap.TryGetValue(key, out ResolvedArchitectureDecision? b);
            targetMap.TryGetValue(key, out ResolvedArchitectureDecision? t);

            if (b is null)

                result.DecisionChanges.Add(new DecisionDelta { DecisionKey = key, TargetValue = t!.SelectedOption, ChangeType = "Added" });

            else if (t is null)

                result.DecisionChanges.Add(new DecisionDelta { DecisionKey = key, BaseValue = b.SelectedOption, ChangeType = "Removed" });

            else if (!string.Equals(b.SelectedOption, t.SelectedOption, StringComparison.Ordinal))

                result.DecisionChanges.Add(new DecisionDelta
                {
                    DecisionKey = key, BaseValue = b.SelectedOption, TargetValue = t.SelectedOption, ChangeType = "Modified"
                });
        }
    }

    private static void CompareRequirements(ManifestDocument baseM, ManifestDocument targetM, ComparisonResult result)
    {
        Dictionary<string, RequirementState> baseStates = RequirementStates(baseM.Requirements);
        Dictionary<string, RequirementState> targetStates = RequirementStates(targetM.Requirements);

        foreach (string name in baseStates.Keys.Union(targetStates.Keys, StringComparer.OrdinalIgnoreCase))
        {
            baseStates.TryGetValue(name, out RequirementState? b);
            targetStates.TryGetValue(name, out RequirementState? t);

            if (b is null && t is not null)
            {
                result.RequirementChanges.Add(new RequirementDelta
                {
                    RequirementName = name, ChangeType = t.Bucket == RequirementBucket.Covered ? "Covered" : "Uncovered"
                });
                continue;
            }

            if (b is not null && t is null)
            {
                result.RequirementChanges.Add(new RequirementDelta { RequirementName = name, ChangeType = "Removed" });
                continue;
            }

            if (b is null || t is null)
                continue;

            if (b.Bucket != t.Bucket)
            {
                result.RequirementChanges.Add(new RequirementDelta
                {
                    RequirementName = name, ChangeType = t.Bucket == RequirementBucket.Covered ? "Covered" : "Uncovered"
                });
                continue;
            }

            if (!string.Equals(b.CoverageStatus, t.CoverageStatus, StringComparison.Ordinal) ||
                b.IsMandatory != t.IsMandatory)

                result.RequirementChanges.Add(new RequirementDelta { RequirementName = name, ChangeType = "Changed" });
        }
    }

    private static Dictionary<string, RequirementState> RequirementStates(RequirementsCoverageSection section)
    {
        Dictionary<string, RequirementState> map = new(StringComparer.OrdinalIgnoreCase);

        // First-wins: if a name appears in both lists, the Covered entry takes priority.

        foreach (RequirementCoverageItem x in section.Covered)
            map.TryAdd(x.RequirementName,
                new RequirementState(RequirementBucket.Covered, x.CoverageStatus, x.IsMandatory));

        foreach (RequirementCoverageItem x in section.Uncovered)
            map.TryAdd(x.RequirementName,
                new RequirementState(RequirementBucket.Uncovered, x.CoverageStatus, x.IsMandatory));

        return map;
    }

    private static void CompareSecurity(ManifestDocument baseM, ManifestDocument targetM, ComparisonResult result)
    {
        Dictionary<string, SecurityPostureItem> baseMap =
            baseM.Security.Controls.GroupBy(Key).ToDictionary(g => g.Key, g => g.First());
        Dictionary<string, SecurityPostureItem> targetMap =
            targetM.Security.Controls.GroupBy(Key).ToDictionary(g => g.Key, g => g.First());

        foreach (string key in baseMap.Keys.Union(targetMap.Keys))
        {
            baseMap.TryGetValue(key, out SecurityPostureItem? b);
            targetMap.TryGetValue(key, out SecurityPostureItem? t);

            if (b is null && t is not null)
            {
                result.SecurityChanges.Add(new SecurityDelta { ControlName = t.ControlName, BaseStatus = null, TargetStatus = t.Status });
                continue;
            }

            if (b is not null && t is null)
            {
                result.SecurityChanges.Add(new SecurityDelta { ControlName = b.ControlName, BaseStatus = b.Status, TargetStatus = null });
                continue;
            }

            if (b is null || t is null)
                continue;

            if (!string.Equals(b.Status, t.Status, StringComparison.Ordinal))

                result.SecurityChanges.Add(new SecurityDelta { ControlName = b.ControlName, BaseStatus = b.Status, TargetStatus = t.Status });
        }

        return;

        static string Key(SecurityPostureItem c)
        {
            return string.IsNullOrWhiteSpace(c.ControlId) ? c.ControlName : $"{c.ControlId}|{c.ControlName}";
        }
    }

    private static void CompareTopology(ManifestDocument baseM, ManifestDocument targetM, ComparisonResult result)
    {
        HashSet<string> baseSet = new(baseM.Topology.Resources, StringComparer.OrdinalIgnoreCase);
        HashSet<string> targetSet = new(targetM.Topology.Resources, StringComparer.OrdinalIgnoreCase);

        foreach (string r in targetSet.Where(r => !baseSet.Contains(r)))

            result.TopologyChanges.Add(new TopologyDelta { Resource = r, ChangeType = "Added" });

        foreach (string r in baseSet.Where(r => !targetSet.Contains(r)))

            result.TopologyChanges.Add(new TopologyDelta { Resource = r, ChangeType = "Removed" });
    }

    private static void CompareCost(ManifestDocument baseM, ManifestDocument targetM, ComparisonResult result)
    {
        decimal? b = baseM.Cost.MaxMonthlyCost;
        decimal? t = targetM.Cost.MaxMonthlyCost;

        if (b != t)

            result.CostChanges.Add(new CostDelta { BaseCost = b, TargetCost = t });
    }

    private static void BuildSummary(ComparisonResult r)
    {
        if (r.DecisionChanges.Count > 0)
            r.SummaryHighlights.Add($"{r.DecisionChanges.Count} decision change(s).");

        if (r.RequirementChanges.Count > 0)
            r.SummaryHighlights.Add($"{r.RequirementChanges.Count} requirement change(s).");

        if (r.SecurityChanges.Count > 0)
            r.SummaryHighlights.Add($"{r.SecurityChanges.Count} security posture delta(s).");

        if (r.TopologyChanges.Count > 0)
            r.SummaryHighlights.Add($"{r.TopologyChanges.Count} topology resource change(s).");

        if (r.CostChanges.Count > 0)
            r.SummaryHighlights.Add("Maximum monthly cost changed.");

        if (r.SummaryHighlights.Count == 0)
            r.SummaryHighlights.Add("No material differences detected in compared sections.");
    }

    private enum RequirementBucket
    {
        Covered,
        Uncovered
    }

    private sealed record RequirementState(RequirementBucket Bucket, string CoverageStatus, bool IsMandatory);
}

