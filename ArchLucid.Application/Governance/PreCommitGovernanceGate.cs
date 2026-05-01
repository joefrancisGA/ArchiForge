using ArchLucid.Contracts.Findings;
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
///     Blocks commit when an enabled assignment enforces a severity threshold and persisted findings meet that bar.
/// </summary>
public sealed class PreCommitGovernanceGate(
    IOptions<PreCommitGovernanceGateOptions> options,
    IScopeContextProvider scopeContextProvider,
    IRunRepository runRepository,
    IFindingsSnapshotRepository findingsSnapshotRepository,
    IPolicyPackAssignmentRepository policyPackAssignmentRepository) : IPreCommitGovernanceGate
{
    private readonly IFindingsSnapshotRepository _findingsSnapshotRepository =
        findingsSnapshotRepository ?? throw new ArgumentNullException(nameof(findingsSnapshotRepository));

    private readonly IOptions<PreCommitGovernanceGateOptions> _options =
        options ?? throw new ArgumentNullException(nameof(options));

    private readonly IPolicyPackAssignmentRepository _policyPackAssignmentRepository =
        policyPackAssignmentRepository ?? throw new ArgumentNullException(nameof(policyPackAssignmentRepository));

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    /// <inheritdoc />
    public Task<PreCommitGateResult> EvaluateAsync(string runId, CancellationToken cancellationToken = default)
    {
        return SimulateSyntheticFindingsInternalAsync(runId, null, 0, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PreCommitGateResult> SimulateSyntheticFindingsAsync(
        string runId,
        FindingSeverity syntheticSeverity,
        int syntheticCount,
        CancellationToken cancellationToken = default)
    {
        return syntheticCount < 0
            ? throw new ArgumentOutOfRangeException(nameof(syntheticCount), syntheticCount,
                "Count must be non-negative.")
            : SimulateSyntheticFindingsInternalAsync(runId, syntheticSeverity, syntheticCount, cancellationToken);
    }

    private async Task<PreCommitGateResult> SimulateSyntheticFindingsInternalAsync(
        string runId,
        FindingSeverity? syntheticSeverity,
        int syntheticCount,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (!_options.Value.PreCommitGateEnabled || !Guid.TryParse(runId, out Guid runKey))
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

        List<Finding> findings = snapshot?.Findings is { Count: > 0 }
            ? snapshot.Findings.ToList()
            : [];

        if (syntheticSeverity is not { } sev || syntheticCount <= 0)
            return EvaluateAgainstFindings(enforcing, findings);

        for (int i = 0; i < syntheticCount; i++)
            findings.Add(CreateSyntheticFinding(runId, i, sev));

        return EvaluateAgainstFindings(enforcing, findings);
    }

    private PreCommitGateResult EvaluateAgainstFindings(
        PolicyPackAssignment enforcing,
        IReadOnlyList<Finding> findings)
    {
        int effectiveMinSeverity = ResolveEffectiveMinimumSeverity(enforcing);
        FindingSeverity effectiveSeverityEnum = (FindingSeverity)effectiveMinSeverity;

        List<string> blockingIds = findings
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
                MinimumBlockingSeverity = (int)effectiveSeverityEnum
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
            Warnings = [warningMessage]
        };
    }

    private static Finding CreateSyntheticFinding(string runId, int index, FindingSeverity severity)
    {
        return new Finding
        {
            FindingId = $"synthetic-precommit-{index}-{Guid.NewGuid():N}",
            FindingType = "SyntheticPreCommitSimulation",
            Category = "GovernanceSimulation",
            EngineType = "Synthetic",
            Severity = severity,
            Title = "Synthetic finding (pre-commit simulation)",
            Rationale = $"Ephemeral-only; not persisted. Run {runId}.",
            RunIdRef = runId
        };
    }

    private static int ResolveEffectiveMinimumSeverity(PolicyPackAssignment assignment)
    {
        if (assignment.BlockCommitMinimumSeverity.HasValue)
            return assignment.BlockCommitMinimumSeverity.Value;

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
