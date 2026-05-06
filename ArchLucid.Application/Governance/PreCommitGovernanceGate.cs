using ArchLucid.Application.Runs;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Validation;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Governance;
/// <summary>
///     Blocks commit when an enabled assignment enforces a severity threshold and persisted findings meet that bar.
/// </summary>
public sealed class PreCommitGovernanceGate(IOptions<PreCommitGovernanceGateOptions> options, IScopeContextProvider scopeContextProvider, IRunRepository runRepository, IFindingsSnapshotRepository findingsSnapshotRepository, IPolicyPackAssignmentRepository policyPackAssignmentRepository, ISchemaValidationService schemaValidationService, IOptions<AuthorityCommitSchemaValidationOptions> authorityCommitSchemaValidationOptions) : IPreCommitGovernanceGate
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(options, scopeContextProvider, runRepository, findingsSnapshotRepository, policyPackAssignmentRepository, schemaValidationService, authorityCommitSchemaValidationOptions);
    private static byte __ValidatePrimaryConstructorArguments(Microsoft.Extensions.Options.IOptions<ArchLucid.Contracts.Governance.PreCommitGovernanceGateOptions> options, ArchLucid.Core.Scoping.IScopeContextProvider scopeContextProvider, ArchLucid.Persistence.Interfaces.IRunRepository runRepository, ArchLucid.Decisioning.Interfaces.IFindingsSnapshotRepository findingsSnapshotRepository, ArchLucid.Decisioning.Governance.PolicyPacks.IPolicyPackAssignmentRepository policyPackAssignmentRepository, ArchLucid.Decisioning.Validation.ISchemaValidationService schemaValidationService, Microsoft.Extensions.Options.IOptions<ArchLucid.Contracts.Architecture.AuthorityCommitSchemaValidationOptions> authorityCommitSchemaValidationOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(scopeContextProvider);
        ArgumentNullException.ThrowIfNull(runRepository);
        ArgumentNullException.ThrowIfNull(findingsSnapshotRepository);
        ArgumentNullException.ThrowIfNull(policyPackAssignmentRepository);
        ArgumentNullException.ThrowIfNull(schemaValidationService);
        ArgumentNullException.ThrowIfNull(authorityCommitSchemaValidationOptions);
        return (byte)0;
    }

    private readonly IOptions<AuthorityCommitSchemaValidationOptions> _authorityCommitSchemaValidationOptions = authorityCommitSchemaValidationOptions ?? throw new ArgumentNullException(nameof(authorityCommitSchemaValidationOptions));
    private readonly IFindingsSnapshotRepository _findingsSnapshotRepository = findingsSnapshotRepository ?? throw new ArgumentNullException(nameof(findingsSnapshotRepository));
    private readonly IOptions<PreCommitGovernanceGateOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IPolicyPackAssignmentRepository _policyPackAssignmentRepository = policyPackAssignmentRepository ?? throw new ArgumentNullException(nameof(policyPackAssignmentRepository));
    private readonly IRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
    private readonly ISchemaValidationService _schemaValidationService = schemaValidationService ?? throw new ArgumentNullException(nameof(schemaValidationService));
    private readonly IScopeContextProvider _scopeContextProvider = scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));
    /// <inheritdoc/>
    public Task<PreCommitGateResult> EvaluateAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runId);
        return SimulateSyntheticFindingsInternalAsync(runId, null, 0, null, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<PreCommitGateResult> EvaluateAsync(string runId, string goldenManifestWireJson, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(goldenManifestWireJson);
        return SimulateSyntheticFindingsInternalAsync(runId, null, 0, goldenManifestWireJson, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<PreCommitGateResult> SimulateSyntheticFindingsAsync(string runId, FindingSeverity syntheticSeverity, int syntheticCount, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runId);
        return syntheticCount < 0 ? throw new ArgumentOutOfRangeException(nameof(syntheticCount), syntheticCount, "Count must be non-negative.") : SimulateSyntheticFindingsInternalAsync(runId, syntheticSeverity, syntheticCount, null, cancellationToken);
    }

    private async Task<PreCommitGateResult> SimulateSyntheticFindingsInternalAsync(string runId, FindingSeverity? syntheticSeverity, int syntheticCount, string? goldenManifestWireJson, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        if (goldenManifestWireJson is not null && _authorityCommitSchemaValidationOptions.Value.ValidateGoldenManifestSchema)
        {
            SchemaValidationResult manifestSchemaResult = _schemaValidationService.ValidateGoldenManifestJson(goldenManifestWireJson);
            if (!manifestSchemaResult.IsValid)
                throw new GoldenManifestSchemaValidationException(manifestSchemaResult);
        }

        if (!_options.Value.PreCommitGateEnabled || !Guid.TryParse(runId, out Guid runKey))
            return PreCommitGateResult.Allowed();
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        RunRecord? run = await _runRepository.GetByIdAsync(scope, runKey, cancellationToken);
        if (run is null || !run.FindingsSnapshotId.HasValue)
            return PreCommitGateResult.Allowed();
        IReadOnlyList<PolicyPackAssignment> assignments = await _policyPackAssignmentRepository.ListByScopeAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, cancellationToken);
        PolicyPackAssignment? enforcing = assignments.Where(static a => a.IsEnabled && (a.BlockCommitOnCritical || a.BlockCommitMinimumSeverity.HasValue)).OrderByDescending(static a => a.AssignedUtc).FirstOrDefault();
        if (enforcing is null)
            return PreCommitGateResult.Allowed();
        FindingsSnapshot? snapshot = await _findingsSnapshotRepository.GetByIdAsync(run.FindingsSnapshotId.Value, cancellationToken);
        List<Finding> findings = snapshot?.Findings is { Count: > 0 } ? snapshot.Findings.ToList() : [];
        if (syntheticSeverity is { } sev && syntheticCount > 0)
        {
            for (int i = 0; i < syntheticCount; i++)
                findings.Add(CreateSyntheticFinding(runId, i, sev));
        }

        return PreCommitGateEvaluator.EvaluateForAssignment(findings, enforcing, _options.Value);
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
}