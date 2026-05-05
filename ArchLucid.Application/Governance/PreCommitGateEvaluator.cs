using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Governance;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Application.Governance;

/// <summary>
///     Shared severity-threshold evaluation used by <see cref="PreCommitGovernanceGate" /> and policy-pack dry-runs.
/// </summary>
public static class PreCommitGateEvaluator
{
    /// <summary>
    ///     Evaluates persisted <see cref="PolicyPackAssignment" /> enforcement against findings (production gate).
    /// </summary>
    public static PreCommitGateResult EvaluateForAssignment(
        IReadOnlyList<Finding> findings,
        PolicyPackAssignment enforcing,
        PreCommitGovernanceGateOptions options)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(enforcing);
        ArgumentNullException.ThrowIfNull(options);

        return Evaluate(
            findings,
            enforcing.BlockCommitOnCritical,
            enforcing.BlockCommitMinimumSeverity,
            enforcing.PolicyPackId.ToString("N"),
            options.WarnOnlySeverities);
    }

    /// <summary>
    ///     Evaluates proposed enforcement flags against findings (dry-run / what-if).
    /// </summary>
    public static PreCommitGateResult Evaluate(
        IReadOnlyList<Finding> findings,
        bool blockCommitOnCritical,
        int? blockCommitMinimumSeverity,
        string policyPackIdLabel,
        string[]? warnOnlySeverities)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyPackIdLabel);

        if (!blockCommitOnCritical && !blockCommitMinimumSeverity.HasValue)
            return PreCommitGateResult.Allowed();

        int effectiveMinSeverity = blockCommitMinimumSeverity ?? (int)FindingSeverity.Critical;
        FindingSeverity effectiveSeverityEnum = (FindingSeverity)effectiveMinSeverity;

        List<string> blockingIds = findings
            .Where(f => (int)f.Severity >= effectiveMinSeverity)
            .Select(static f => f.FindingId)
            .ToList();

        if (blockingIds.Count == 0)
            return PreCommitGateResult.Allowed();

        string severityLabel = effectiveSeverityEnum.ToString();

        if (!IsWarnOnlySeverity(severityLabel, warnOnlySeverities))
            return new PreCommitGateResult
            {
                Blocked = true,
                Reason =
                    $"{blockingIds.Count} {severityLabel}+ finding(s) block commit per policy pack assignment (pack {policyPackIdLabel}).",
                BlockingFindingIds = blockingIds,
                PolicyPackId = policyPackIdLabel,
                MinimumBlockingSeverity = (int)effectiveSeverityEnum
            };

        string warningMessage =
            $"{blockingIds.Count} {severityLabel}+ finding(s) detected per policy pack (pack {policyPackIdLabel}) — warn only.";

        return new PreCommitGateResult
        {
            Blocked = false,
            WarnOnly = true,
            Reason = warningMessage,
            BlockingFindingIds = blockingIds,
            PolicyPackId = policyPackIdLabel,
            MinimumBlockingSeverity = (int)effectiveSeverityEnum,
            Warnings = [warningMessage]
        };
    }

    private static bool IsWarnOnlySeverity(string severityLabel, string[]? warnOnly)
    {
        if (warnOnly is null || warnOnly.Length == 0)
            return false;

        return warnOnly.Any(w => string.Equals(w, severityLabel, StringComparison.OrdinalIgnoreCase));
    }
}
