using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// When effective policy content lists compliance rule IDs or keys, restrict evaluation to those rules.
/// Empty lists mean no filter (full file-based pack).
/// </summary>
public static class ComplianceRulePackGovernanceFilter
{
    public static ComplianceRulePack Filter(ComplianceRulePack source, PolicyPackContentDocument effective)
    {
        if (effective.ComplianceRuleIds.Count == 0 && effective.ComplianceRuleKeys.Count == 0)
            return source;

        var keySet = effective.ComplianceRuleKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var guidSet = effective.ComplianceRuleIds.ToHashSet();

        var rules = source.Rules
            .Where(
                r => keySet.Contains(r.RuleId) ||
                     (Guid.TryParse(r.RuleId, out var g) && guidSet.Contains(g)))
            .ToList();

        return new ComplianceRulePack
        {
            RulePackId = source.RulePackId,
            Name = source.Name,
            Version = source.Version,
            RulePackHash = source.RulePackHash,
            SourcePath = source.SourcePath,
            Rules = rules,
        };
    }
}
