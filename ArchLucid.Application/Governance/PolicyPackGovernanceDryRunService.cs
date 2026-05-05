using System.Text.Json;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Llm.Redaction;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;

using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Governance;

/// <inheritdoc cref="IPolicyPackGovernanceDryRunService" />
/// <remarks>
///     <para>
///         Does not honor <see cref="PreCommitGovernanceGateOptions.PreCommitGateEnabled" /> — the global toggle only applies
///         to live commits; dry-run always evaluates proposed enforcement so operators can preview blocking behavior.
///     </para>
///     <para>
///         After a successful evaluation, logs <see cref="AuditEventTypes.GovernanceDryRunRequested" /> with proposed pack
///         JSON redacted via <see cref="IPromptRedactor" /> (PENDING_QUESTIONS Q37), using
///         <see cref="DurableAuditLogRetry.TryLogAsync" /> so audit I/O failures do not block the read-only response (
///         same shape as <see cref="PolicyPackDryRunService" />).
///     </para>
/// </remarks>
public sealed class PolicyPackGovernanceDryRunService(
    IScopeContextProvider scopeContextProvider,
    IRunRepository runRepository,
    IFindingsSnapshotRepository findingsSnapshotRepository,
    IGoldenManifestRepository goldenManifestRepository,
    IOptions<PreCommitGovernanceGateOptions> preCommitOptions,
    IPromptRedactor promptRedactor,
    IAuditService auditService,
    ILogger<PolicyPackGovernanceDryRunService> logger) : IPolicyPackGovernanceDryRunService
{
    private static readonly string[] BlockCommitOnCriticalMetadataKeys =
    [
        "governance.blockCommitOnCritical",
        "blockCommitOnCritical"
    ];

    private static readonly string[] BlockCommitMinimumSeverityMetadataKeys =
    [
        "governance.blockCommitMinimumSeverity",
        "blockCommitMinimumSeverity"
    ];

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IFindingsSnapshotRepository _findingsSnapshotRepository =
        findingsSnapshotRepository ?? throw new ArgumentNullException(nameof(findingsSnapshotRepository));

    private readonly IGoldenManifestRepository _goldenManifestRepository =
        goldenManifestRepository ?? throw new ArgumentNullException(nameof(goldenManifestRepository));

    private readonly ILogger<PolicyPackGovernanceDryRunService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IOptions<PreCommitGovernanceGateOptions> _preCommitOptions =
        preCommitOptions ?? throw new ArgumentNullException(nameof(preCommitOptions));

    private readonly IPromptRedactor _promptRedactor =
        promptRedactor ?? throw new ArgumentNullException(nameof(promptRedactor));

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    /// <inheritdoc />
    public async Task<PolicyPackGovernanceDryRunResult?> EvaluateAsync(
        string policyPackContentJson,
        string? targetRunId,
        Guid? targetManifestId,
        bool? blockCommitOnCritical,
        int? blockCommitMinimumSeverity,
        Guid? proposedPolicyPackId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyPackContentJson);

        PolicyPackContentDocument document =
            JsonSerializer.Deserialize<PolicyPackContentDocument>(
                policyPackContentJson,
                PolicyPackJsonSerializerOptions.Default) ?? new PolicyPackContentDocument();

        (bool mergedCritical, int? mergedMin) =
            MergeEnforcement(blockCommitOnCritical, blockCommitMinimumSeverity, document);

        bool gateActive = mergedCritical || mergedMin.HasValue;
        string packLabel = proposedPolicyPackId?.ToString("N") ?? "dry-run-proposed-pack";

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        Guid? usedManifestId = null;
        Guid runKey;

        if (!string.IsNullOrWhiteSpace(targetRunId))
        {
            if (!Guid.TryParse(targetRunId.Trim(), out runKey))
                return null;
        }
        else if (targetManifestId is { } manifestKey)
        {
            ManifestDocument? manifest = await _goldenManifestRepository
                .GetByIdAsync(scope, manifestKey, cancellationToken)
                .ConfigureAwait(false);

            if (manifest is null)
                return null;

            runKey = manifest.RunId;
            usedManifestId = manifestKey;
        }
        else
            throw new InvalidOperationException("Target run or manifest is required.");

        RunRecord? run = await _runRepository.GetByIdAsync(scope, runKey, cancellationToken).ConfigureAwait(false);

        if (run is null)
            return null;

        List<Finding> findings = [];
        if (run.FindingsSnapshotId is { } snapshotId)
        {
            FindingsSnapshot? snapshot =
                await _findingsSnapshotRepository.GetByIdAsync(snapshotId, cancellationToken).ConfigureAwait(false);

            if (snapshot?.Findings is { Count: > 0 } list)
                findings = list.ToList();
        }

        PreCommitGateResult gate = gateActive
            ? PreCommitGateEvaluator.Evaluate(
                findings,
                mergedCritical,
                mergedMin,
                packLabel,
                _preCommitOptions.Value.WarnOnlySeverities)
            : PreCommitGateResult.Allowed();

        List<string> passed =
        [
            "policy_pack_content_json: parsed",
            "target: resolved run under tenant/workspace/project scope"
        ];

        List<string> failed = [];

        if (gateActive)
        {
            if (gate.Blocked)
                failed.Add("pre_commit_severity_gate: would block commit (findings meet proposed minimum severity)");
            else if (gate.WarnOnly)
                passed.Add("pre_commit_severity_gate: evaluated (warn-only — commit would be allowed)");
            else
                passed.Add("pre_commit_severity_gate: evaluated (passed — commit would be allowed)");
        }
        else
            passed.Add("pre_commit_severity_gate: skipped (no enforcement flags in proposed pack or request overrides)");

        List<string> warnings = [..gate.Warnings];

        PolicyPackGovernanceDryRunResult result = new()
        {
            ResolvedRunId = runKey.ToString("N"),
            TargetManifestId = usedManifestId,
            GateResult = gate,
            PassedChecks = passed,
            FailedChecks = failed,
            Warnings = warnings
        };

        await TryLogProposedPackDryRunAuditAsync(
            policyPackContentJson,
            targetRunId,
            targetManifestId,
            proposedPolicyPackId,
            packLabel,
            runKey,
            usedManifestId,
            gate,
            cancellationToken);

        return result;
    }

    private static (bool BlockCommitOnCritical, int? MinimumSeverity) MergeEnforcement(
        bool? requestCritical,
        int? requestMinSeverity,
        PolicyPackContentDocument document)
    {
        bool? fromMeta = TryReadNullableBool(document.Metadata, BlockCommitOnCriticalMetadataKeys);
        int? minFromMeta = TryReadNullableInt(document.Metadata, BlockCommitMinimumSeverityMetadataKeys);

        bool critical = requestCritical ?? fromMeta ?? false;
        int? min = requestMinSeverity ?? minFromMeta;

        return (critical, min);
    }

    private static bool? TryReadNullableBool(IReadOnlyDictionary<string, string> metadata, string[] keys)
    {
        foreach (string key in keys)
        {
            if (!metadata.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
                continue;

            if (bool.TryParse(raw.Trim(), out bool b))
                return b;

            if (int.TryParse(raw.Trim(), out int i))
            {
                if (i == 1)
                    return true;

                if (i == 0)
                    return false;
            }

            if (string.Equals(raw.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(raw.Trim(), "no", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return null;
    }

    private static int? TryReadNullableInt(IReadOnlyDictionary<string, string> metadata, string[] keys)
    {
        foreach (string key in keys)
        {
            if (!metadata.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
                continue;

            if (int.TryParse(raw.Trim(), out int value))
                return value;
        }

        return null;
    }

    /// <summary>
    ///     Redacts <paramref name="policyPackContentJson" /> through <see cref="IPromptRedactor" /> before it is stored in
    ///     <c>dbo.AuditEvents</c> (Q37).
    /// </summary>
    internal string RedactPolicyPackContentJsonForAudit(string policyPackContentJson)
    {
        PromptRedactionOutcome outcome = _promptRedactor.Redact(policyPackContentJson);

        return outcome.Text;
    }

    private async Task TryLogProposedPackDryRunAuditAsync(
        string policyPackContentJson,
        string? targetRunId,
        Guid? targetManifestId,
        Guid? proposedPolicyPackId,
        string packLabel,
        Guid runKey,
        Guid? usedManifestId,
        PreCommitGateResult gate,
        CancellationToken cancellationToken)
    {
        string redactedContentJson = RedactPolicyPackContentJsonForAudit(policyPackContentJson);

        string dataJson = JsonSerializer.Serialize(
            new
            {
                workflow = "proposedPolicyPackContent",
                proposedPolicyPackId,
                policyPackContentRedacted = redactedContentJson,
                targetRunId = string.IsNullOrWhiteSpace(targetRunId) ? null : targetRunId.Trim(),
                targetManifestId,
                resolvedRunId = runKey.ToString("N"),
                gateSummary = new
                {
                    blocked = gate.Blocked,
                    warnOnly = gate.WarnOnly,
                    packLabel,
                    warningCount = gate.Warnings.Count
                }
            },
            AuditJsonSerializationOptions.Instance);

        AuditEvent auditEvent = new() { EventType = AuditEventTypes.GovernanceDryRunRequested, DataJson = dataJson };

        string operationKey = proposedPolicyPackId is { } packId ? packId.ToString("D") : runKey.ToString("N");

        await DurableAuditLogRetry.TryLogAsync(
            ct => _auditService.LogAsync(auditEvent, ct),
            _logger,
            $"GovernanceDryRunRequested:proposed:{operationKey}",
            cancellationToken);
    }
}
