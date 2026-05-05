using ArchLucid.Contracts.Governance;

namespace ArchLucid.Application.Governance;

/// <summary>
///     Dry-runs proposed <see cref="ArchLucid.Decisioning.Governance.PolicyPacks.PolicyPackContentDocument" /> JSON
///     against a target run's findings using the same severity gate logic as <see cref="IPreCommitGovernanceGate" />.
/// </summary>
public interface IPolicyPackGovernanceDryRunService
{
    /// <summary>
    ///     Loads findings for the target under the ambient scope (RLS), evaluates proposed enforcement, returns null when
    ///     the run or manifest cannot be resolved in scope.
    /// </summary>
    Task<PolicyPackGovernanceDryRunResult?> EvaluateAsync(
        string policyPackContentJson,
        string? targetRunId,
        Guid? targetManifestId,
        bool? blockCommitOnCritical,
        int? blockCommitMinimumSeverity,
        Guid? proposedPolicyPackId,
        CancellationToken cancellationToken = default);
}
