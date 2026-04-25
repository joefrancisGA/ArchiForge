using ArchLucid.Decisioning.Alerts;
using ArchLucid.Decisioning.Alerts.Composite;

namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>
///     Applies effective policy-pack allow-lists to in-memory alert rule collections before evaluation.
/// </summary>
/// <remarks>
///     <para>
///         <strong>Semantics:</strong> Empty <see cref="PolicyPackContentDocument.AlertRuleIds" /> /
///         <see cref="PolicyPackContentDocument.CompositeAlertRuleIds" />
///         means <em>no filter</em> (all rules in scope still run). Non-empty lists restrict to intersection by id.
///     </para>
///     <para>
///         <strong>Callers:</strong> <c>ArchLucid.Persistence.Alerts.AlertService</c> and <c>CompositeAlertService</c>
///         after loading
///         <see cref="IEffectiveGovernanceLoader" /> content.
///     </para>
/// </remarks>
public static class PolicyPackGovernanceFilter
{
    /// <summary>Filters simple <see cref="AlertRule" /> instances to those allowed by effective policy.</summary>
    /// <param name="rules">Candidate rules already scoped to tenant/workspace/project by repositories.</param>
    /// <param name="effective">Merged document from governance resolution.</param>
    /// <returns>Copy of <paramref name="rules" /> or a filtered subset; never mutates input list.</returns>
    /// <remarks>
    ///     When <see cref="PolicyPackContentDocument.AlertRuleIds" /> is empty, returns <c>rules.ToList()</c> (legacy
    ///     behavior).
    /// </remarks>
    public static List<AlertRule> FilterAlertRules(
        IReadOnlyList<AlertRule> rules,
        PolicyPackContentDocument effective)
    {
        if (effective.AlertRuleIds.Count == 0)
            return rules.ToList();

        HashSet<Guid> allow = effective.AlertRuleIds.ToHashSet();
        return rules.Where(r => allow.Contains(r.RuleId)).ToList();
    }

    /// <summary>Filters <see cref="CompositeAlertRule" /> instances to those allowed by effective policy.</summary>
    /// <param name="rules">Candidate composite rules in scope.</param>
    /// <param name="effective">Merged document from governance resolution.</param>
    /// <returns>Copy or filtered list; does not mutate inputs.</returns>
    public static List<CompositeAlertRule> FilterCompositeRules(
        IReadOnlyList<CompositeAlertRule> rules,
        PolicyPackContentDocument effective)
    {
        if (effective.CompositeAlertRuleIds.Count == 0)
            return rules.ToList();

        HashSet<Guid> allow = effective.CompositeAlertRuleIds.ToHashSet();
        return rules.Where(r => allow.Contains(r.CompositeRuleId)).ToList();
    }
}
