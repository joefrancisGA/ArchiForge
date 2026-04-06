using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

// ReSharper disable InvalidXmlDocComment
/// <summary>
/// Narrows an in-memory <see cref="ComplianceRulePack"/> using effective compliance ids/keys from policy packs.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Semantics:</strong> If both <see cref="PolicyPackContentDocument.ComplianceRuleIds"/> and
/// <see cref="PolicyPackContentDocument.ComplianceRuleKeys"/> are empty, returns <paramref name="source"/> unchanged (full pack).
/// Otherwise, keeps rules whose <see cref="ComplianceRule.RuleId"/> matches a key or parses as a listed GUID.
/// </para>
/// <para>
/// <strong>Caller:</strong> <c>ArchiForge.Persistence.Compliance.PolicyFilteredComplianceRulePackProvider</c> when building packs for evaluation.
/// </para>
/// </remarks>
/// // ReSharper enable InvalidXmlDocComment
public static class ComplianceRulePackGovernanceFilter
{
    /// <summary>Returns a new pack instance with <see cref="ComplianceRulePack.Rules"/> filtered; does not mutate <paramref name="source"/>.</summary>
    /// <param name="source">Full file-backed or merged pack before policy narrowing.</param>
    /// <param name="effective">Merged governance document for the evaluation scope.</param>
    public static ComplianceRulePack Filter(ComplianceRulePack source, PolicyPackContentDocument effective)
    {
        if (effective.ComplianceRuleIds.Count == 0 && effective.ComplianceRuleKeys.Count == 0)
            return source;

        HashSet<string> keySet = effective.ComplianceRuleKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<Guid> guidSet = effective.ComplianceRuleIds.ToHashSet();

        List<ComplianceRule> rules = source.Rules
            .Where(
                r => keySet.Contains(r.RuleId) ||
                     (Guid.TryParse(r.RuleId, out Guid g) && guidSet.Contains(g)))
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
