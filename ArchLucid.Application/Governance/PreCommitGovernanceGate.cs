using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Governance;

/// <summary>
/// Blocks commit when an enabled assignment enforces a severity threshold
/// and the run's findings snapshot contains findings at or above that threshold.
/// Supports configurable <see cref="PolicyPackAssignment.BlockCommitMinimumSeverity"/>
/// and warn-only severities via <see cref="PreCommitGovernanceGateOptions.WarnOnlySeverities"/>.
/// </summary>
public sealed class PreCommitGovernanceGate(
    IOptions<PreCommitGovernanceGateOptions> options,
    IScopeContextProvider scopeContextProvider,
    IRunRepository runRepository,
    IFindingsSnapshotRepository findingsSnapshotRepository,
    IPolicyPackAssignmentRepository policyPackAssignmentRepository) : IPreCommitGovernanceGate
{
    private readonly IOptions<PreCommitGovernanceGateOptions> _options =
        options ?? throw new ArgumentNullException(nameof(options));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IFindingsSnapshotRepository _findingsSnapshotRepository =
        findingsSnapshotRepository ?? throw new ArgumentNullException(nameof(findingsSnapshotRepository));

    private readonly IPolicyPackAssignmentRepository _policyPackAssignmentRepository =
        policyPackAssignmentRepository ?? throw new ArgumentNullException(nameof(policyPackAssignmentRepository));

    /// <inheritdoc />
    public async Task<PreCommitGateResult> EvaluateAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (!_options.Value.PreCommitGateEnabled)
            return PreCommitGateResult.Allowed();


        if (!Guid.TryParse(runId, out Guid runKey))
            return PreCommitGateResult.Allowed();


        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        RunRecord? run = await _runRepository.GetByIdAsync(scope, runKey, cancellationToken);

        if (run is null || !run.FindingsSnapshotId.HasValue)
            return PreCommitGateResult.Allowed();


        IReadOnlyList<PolicyPackAssignment> assignments = await _policyPackAssignmentRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            cancellationToken);

        PolicyPackAssignment? enforcing = assignments
            .Where(static a => a.IsEnabled && (a.BlockCommitOnCritical || a.BlockCommitMinimumSeverity.HasValue))
            .OrderByDescending(static a => a.AssignedUtc)
            .FirstOrDefault();

        if (enforcing is null)
            return PreCommitGateResult.Allowed();


        FindingsSnapshot? snapshot =
            await _findingsSnapshotRepository.GetByIdAsync(run.FindingsSnapshotId.Value, cancellationToken);

        if (snapshot is null)
            return PreCommitGateResult.Allowed();


        int effectiveMinSeverity = ResolveEffectiveMinimumSeverity(enforcing);
        FindingSeverity effectiveSeverityEnum = (FindingSeverity)effectiveMinSeverity;

        List<string> blockingIds = snapshot.Findings
            .Where(f => (int)f.Severity >= effectiveMinSeverity)
            .Select(static f => f.FindingId)
            .ToList();

        if (blockingIds.Count == 0)
            return PreCommitGateResult.Allowed();

        string packLabel = enforcing.PolicyPackId.ToString("N");
        string severityLabel = effectiveSeverityEnum.ToString();

        if (!IsWarnOnlySeverity(severityLabel))
            return new PreCommitGateResult
            {
                Blocked = true,
                Reason =
                    $"{blockingIds.Count} {severityLabel}+ finding(s) block commit per policy pack assignment (pack {packLabel}).",
                BlockingFindingIds = blockingIds,
                PolicyPackId = packLabel,
                MinimumBlockingSeverity = (int)effectiveSeverityEnum,
            };

        string warningMessage =
            $"{blockingIds.Count} {severityLabel}+ finding(s) detected per policy pack (pack {packLabel}) — warn only.";

        return new PreCommitGateResult
        {
            Blocked = false,
            WarnOnly = true,
            Reason = warningMessage,
            BlockingFindingIds = blockingIds,
            PolicyPackId = packLabel,
            MinimumBlockingSeverity = (int)effectiveSeverityEnum,
            Warnings = [warningMessage],
        };
    }

    private static int ResolveEffectiveMinimumSeverity(PolicyPackAssignment assignment)
    {
        if (assignment.BlockCommitMinimumSeverity.HasValue)
            return assignment.BlockCommitMinimumSeverity.Value;

        // Legacy behavior: BlockCommitOnCritical=true → block on Critical (3) only
        return (int)FindingSeverity.Critical;
    }

    private bool IsWarnOnlySeverity(string severityLabel)
    {
        string[]? warnOnly = _options.Value.WarnOnlySeverities;

        if (warnOnly is null || warnOnly.Length == 0)
            return false;


        return warnOnly.Any(w => string.Equals(w, severityLabel, StringComparison.OrdinalIgnoreCase));
    }
}
