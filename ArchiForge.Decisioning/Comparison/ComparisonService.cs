using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Comparison;

public sealed class ComparisonService : IComparisonService
{
    public ComparisonResult Compare(GoldenManifest baseM, GoldenManifest targetM)
    {
        var result = new ComparisonResult
        {
            BaseRunId = baseM.RunId,
            TargetRunId = targetM.RunId
        };

        CompareDecisions(baseM, targetM, result);
        CompareRequirements(baseM, targetM, result);
        CompareSecurity(baseM, targetM, result);
        CompareTopology(baseM, targetM, result);
        CompareCost(baseM, targetM, result);
        BuildSummary(result);

        return result;
    }

    private static string DecisionKey(ResolvedArchitectureDecision d) =>
        !string.IsNullOrWhiteSpace(d.DecisionId) ? d.DecisionId : $"{d.Category}::{d.Title}";

    private static void CompareDecisions(GoldenManifest baseM, GoldenManifest targetM, ComparisonResult result)
    {
        var baseMap = baseM.Decisions.GroupBy(DecisionKey).ToDictionary(g => g.Key, g => g.First());
        var targetMap = targetM.Decisions.GroupBy(DecisionKey).ToDictionary(g => g.Key, g => g.First());

        foreach (var key in baseMap.Keys.Union(targetMap.Keys))
        {
            baseMap.TryGetValue(key, out var b);
            targetMap.TryGetValue(key, out var t);

            if (b is null)
            {
                result.DecisionChanges.Add(new DecisionDelta
                {
                    DecisionKey = key,
                    TargetValue = t!.SelectedOption,
                    ChangeType = "Added"
                });
            }
            else if (t is null)
            {
                result.DecisionChanges.Add(new DecisionDelta
                {
                    DecisionKey = key,
                    BaseValue = b.SelectedOption,
                    ChangeType = "Removed"
                });
            }
            else if (!string.Equals(b.SelectedOption, t.SelectedOption, StringComparison.Ordinal))
            {
                result.DecisionChanges.Add(new DecisionDelta
                {
                    DecisionKey = key,
                    BaseValue = b.SelectedOption,
                    TargetValue = t.SelectedOption,
                    ChangeType = "Modified"
                });
            }
        }
    }

    private static void CompareRequirements(GoldenManifest baseM, GoldenManifest targetM, ComparisonResult result)
    {
        var baseStates = RequirementStates(baseM.Requirements);
        var targetStates = RequirementStates(targetM.Requirements);

        foreach (var name in baseStates.Keys.Union(targetStates.Keys, StringComparer.OrdinalIgnoreCase))
        {
            baseStates.TryGetValue(name, out var b);
            targetStates.TryGetValue(name, out var t);

            if (b is null && t is not null)
            {
                result.RequirementChanges.Add(new RequirementDelta
                {
                    RequirementName = name,
                    ChangeType = t.Bucket == RequirementBucket.Covered ? "Covered" : "Uncovered"
                });
                continue;
            }

            if (b is not null && t is null)
            {
                result.RequirementChanges.Add(new RequirementDelta
                {
                    RequirementName = name,
                    ChangeType = "Removed"
                });
                continue;
            }

            if (b is null || t is null)
                continue;

            if (b.Bucket != t.Bucket)
            {
                result.RequirementChanges.Add(new RequirementDelta
                {
                    RequirementName = name,
                    ChangeType = t.Bucket == RequirementBucket.Covered ? "Covered" : "Uncovered"
                });
                continue;
            }

            if (!string.Equals(b.CoverageStatus, t.CoverageStatus, StringComparison.Ordinal) ||
                b.IsMandatory != t.IsMandatory)
            {
                result.RequirementChanges.Add(new RequirementDelta
                {
                    RequirementName = name,
                    ChangeType = "Changed"
                });
            }
        }
    }

    private enum RequirementBucket
    {
        Covered,
        Uncovered
    }

    private sealed record RequirementState(RequirementBucket Bucket, string CoverageStatus, bool IsMandatory);

    private static Dictionary<string, RequirementState> RequirementStates(RequirementsCoverageSection section)
    {
        var map = new Dictionary<string, RequirementState>(StringComparer.OrdinalIgnoreCase);
        foreach (var x in section.Covered)
            map[x.RequirementName] = new RequirementState(RequirementBucket.Covered, x.CoverageStatus, x.IsMandatory);
        foreach (var x in section.Uncovered)
            map[x.RequirementName] = new RequirementState(RequirementBucket.Uncovered, x.CoverageStatus, x.IsMandatory);
        return map;
    }

    private static void CompareSecurity(GoldenManifest baseM, GoldenManifest targetM, ComparisonResult result)
    {
        string Key(SecurityPostureItem c) =>
            string.IsNullOrWhiteSpace(c.ControlId) ? c.ControlName : $"{c.ControlId}|{c.ControlName}";

        var baseMap = baseM.Security.Controls.GroupBy(Key).ToDictionary(g => g.Key, g => g.First());
        var targetMap = targetM.Security.Controls.GroupBy(Key).ToDictionary(g => g.Key, g => g.First());

        foreach (var key in baseMap.Keys.Union(targetMap.Keys))
        {
            baseMap.TryGetValue(key, out var b);
            targetMap.TryGetValue(key, out var t);

            if (b is null && t is not null)
            {
                result.SecurityChanges.Add(new SecurityDelta
                {
                    ControlName = t.ControlName,
                    BaseStatus = null,
                    TargetStatus = t.Status
                });
                continue;
            }

            if (b is not null && t is null)
            {
                result.SecurityChanges.Add(new SecurityDelta
                {
                    ControlName = b.ControlName,
                    BaseStatus = b.Status,
                    TargetStatus = null
                });
                continue;
            }

            if (b is null || t is null)
                continue;

            if (!string.Equals(b.Status, t.Status, StringComparison.Ordinal))
            {
                result.SecurityChanges.Add(new SecurityDelta
                {
                    ControlName = b.ControlName,
                    BaseStatus = b.Status,
                    TargetStatus = t.Status
                });
            }
        }
    }

    private static void CompareTopology(GoldenManifest baseM, GoldenManifest targetM, ComparisonResult result)
    {
        var baseSet = new HashSet<string>(baseM.Topology.Resources, StringComparer.OrdinalIgnoreCase);
        var targetSet = new HashSet<string>(targetM.Topology.Resources, StringComparer.OrdinalIgnoreCase);

        foreach (var r in targetSet)
        {
            if (!baseSet.Contains(r))
                result.TopologyChanges.Add(new TopologyDelta { Resource = r, ChangeType = "Added" });
        }

        foreach (var r in baseSet)
        {
            if (!targetSet.Contains(r))
                result.TopologyChanges.Add(new TopologyDelta { Resource = r, ChangeType = "Removed" });
        }
    }

    private static void CompareCost(GoldenManifest baseM, GoldenManifest targetM, ComparisonResult result)
    {
        var b = baseM.Cost.MaxMonthlyCost;
        var t = targetM.Cost.MaxMonthlyCost;
        if (b != t)
        {
            result.CostChanges.Add(new CostDelta
            {
                BaseCost = b,
                TargetCost = t
            });
        }
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
}
