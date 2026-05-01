using ArchLucid.Contracts.DecisionTraces;

using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Architecture;

/// <summary>
///     Integrity rules for runs committed on the <see cref="Decisioning.Interfaces.IDecisionEngine" /> path:
///     the projected <see cref="Cm.GoldenManifest" /> must reference the rule-audit trace in
///     <c>Metadata.DecisionTraceIds</c>.
/// </summary>
public static class AuthorityCommitTraceabilityRules
{
    /// <summary>
    ///     Returns human-readable gaps when the contract manifest and persisted traces disagree (authority path).
    /// </summary>
    public static IReadOnlyList<string> GetLinkageGaps(
        Cm.GoldenManifest? manifest,
        IReadOnlyList<DecisionTrace> traces)
    {
        if (manifest is null)
            return [];

        List<string> gaps = [];
        HashSet<string> onManifest = new(StringComparer.OrdinalIgnoreCase);

        foreach (string id in manifest.Metadata.DecisionTraceIds)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                gaps.Add("Manifest.Metadata.DecisionTraceIds contains an empty entry (authority path).");
                continue;
            }

            onManifest.Add(id);
        }

        List<string> ruleAuditN = [];

        foreach (DecisionTrace t in traces)
        {
            if (t is not RuleAuditTrace rat)
            {
                gaps.Add(
                    $"Decision trace is not a {nameof(RuleAuditTrace)} (Kind={t.Kind}); authority commits expect rule-audit traces only.");
                continue;
            }

            string n = rat.RuleAudit.DecisionTraceId.ToString("N");

            if (string.IsNullOrWhiteSpace(n))
                gaps.Add("A rule-audit decision trace has an empty DecisionTraceId.");
            else
                ruleAuditN.Add(n);
        }

        HashSet<string> onTrace = new(ruleAuditN, StringComparer.OrdinalIgnoreCase);

        foreach (string tid in onTrace)

            if (!onManifest.Contains(tid))
                gaps.Add($"Trace '{tid}' is missing from Manifest.Metadata.DecisionTraceIds (authority path).");

        foreach (string mid in onManifest)

            if (!onTrace.Contains(mid))
                gaps.Add($"Manifest.Metadata.DecisionTraceIds lists unknown trace id '{mid}' (authority path).");

        return gaps;
    }
}
