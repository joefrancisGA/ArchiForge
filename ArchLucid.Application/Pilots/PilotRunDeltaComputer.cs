using ArchLucid.Application.Bootstrap;
using ArchLucid.Application.Explanation;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Audit;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Pilots;

/// <inheritdoc cref="IPilotRunDeltaComputer" />
/// <remarks>
/// Read-only by construction: makes one filtered audit query, one trace query, and at most one evidence-chain
/// query per call. Failures in the audit / trace / evidence queries are swallowed (warning-logged) so a sponsor
/// report still renders for runs whose ancillary stores are temporarily unavailable.
/// </remarks>
public sealed class PilotRunDeltaComputer(
    IFindingEvidenceChainService evidenceChainService,
    IAgentExecutionTraceRepository agentExecutionTraceRepository,
    IAuditRepository auditRepository,
    IScopeContextProvider scopeContextProvider,
    ILogger<PilotRunDeltaComputer> logger) : IPilotRunDeltaComputer
{
    /// <summary>Hard cap on audit-row scans for a single run; keeps the sponsor report O(1) even on noisy runs.</summary>
    private const int AuditRowQueryCap = 500;

    private readonly IFindingEvidenceChainService _evidenceChainService =
        evidenceChainService ?? throw new ArgumentNullException(nameof(evidenceChainService));

    private readonly IAgentExecutionTraceRepository _agentExecutionTraceRepository =
        agentExecutionTraceRepository ?? throw new ArgumentNullException(nameof(agentExecutionTraceRepository));

    private readonly IAuditRepository _auditRepository =
        auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly ILogger<PilotRunDeltaComputer> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<PilotRunDeltas> ComputeAsync(
        ArchitectureRunDetail detail,
        CancellationToken cancellationToken = default)
    {
        if (detail is null)
            throw new ArgumentNullException(nameof(detail));

        Contracts.Metadata.ArchitectureRun run = detail.Run;
        string runId = run.RunId;

        DateTime? committedUtc = detail.Manifest?.Metadata.CreatedUtc;
        TimeSpan? wall = committedUtc is { } c
            ? c - run.CreatedUtc
            : null;

        IReadOnlyList<KeyValuePair<string, int>> findings = AggregateFindingsBySeverity(detail);
        ArchitectureFinding? topFinding = SelectTopSeverityFinding(detail);

        int llmCallCount = await TryCountLlmCallsAsync(runId, cancellationToken);
        (int auditCount, bool auditTruncated) = await TryCountAuditRowsAsync(runId, cancellationToken);
        Contracts.Explanation.FindingEvidenceChainResponse? chain = topFinding is null
            ? null
            : await TryBuildEvidenceChainAsync(runId, topFinding.FindingId, cancellationToken);

        bool isDemo =
            ContosoRetailDemoIdentifiers.IsDemoRunId(runId)
            || ContosoRetailDemoIdentifiers.IsDemoRequestId(run.RequestId);

        return new PilotRunDeltas
        {
            RunCreatedUtc = run.CreatedUtc,
            ManifestCommittedUtc = committedUtc,
            TimeToCommittedManifest = wall,
            FindingsBySeverity = findings,
            AuditRowCount = auditCount,
            AuditRowCountTruncated = auditTruncated,
            LlmCallCount = llmCallCount,
            TopFindingId = topFinding?.FindingId,
            TopFindingSeverity = topFinding?.Severity.ToString(),
            TopFindingEvidenceChain = chain,
            IsDemoTenant = isDemo,
        };
    }

    /// <summary>Returns severity counts in descending order (highest count first), grouped case-insensitively.</summary>
    private static IReadOnlyList<KeyValuePair<string, int>> AggregateFindingsBySeverity(ArchitectureRunDetail detail) =>
        detail.Results
            .Where(_ => true)
            .SelectMany(static r => r.Findings)
            .Where(_ => true)
            .GroupBy(
                static f => f.Severity.ToString(),
                StringComparer.OrdinalIgnoreCase)
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(static p => p.Value)
            .ThenBy(static p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Picks the single highest-severity finding; ties broken by first-seen order to keep output deterministic.</summary>
    private static ArchitectureFinding? SelectTopSeverityFinding(ArchitectureRunDetail detail) =>
        detail.Results
            .Where(_ => true)
            .SelectMany(static r => r.Findings)
            .Where(_ => true)
            .OrderByDescending(static f => (int)f.Severity)
            .FirstOrDefault();

    private async Task<int> TryCountLlmCallsAsync(string runId, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<Contracts.Agents.AgentExecutionTrace> traces =
                await _agentExecutionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

            return traces.Count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Pilot delta: LLM call count unavailable for run {RunId}; reporting 0.", runId);

            return 0;
        }
    }

    private async Task<(int Count, bool Truncated)> TryCountAuditRowsAsync(string runId, CancellationToken cancellationToken)
    {
        if (!TryParseRunGuid(runId, out Guid runGuid))
            return (0, false);

        try
        {
            ScopeContext scope = _scopeContextProvider.GetCurrentScope();
            AuditEventFilter filter = new()
            {
                RunId = runGuid,
                Take = AuditRowQueryCap,
            };

            IReadOnlyList<AuditEvent> events = await _auditRepository.GetFilteredAsync(
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                filter,
                cancellationToken);

            int count = events.Count;
            bool truncated = count >= AuditRowQueryCap;

            return (count, truncated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Pilot delta: audit row count unavailable for run {RunId}; reporting 0.", runId);

            return (0, false);
        }
    }

    private async Task<Contracts.Explanation.FindingEvidenceChainResponse?> TryBuildEvidenceChainAsync(
        string runId,
        string findingId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _evidenceChainService.BuildAsync(runId, findingId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Pilot delta: evidence chain unavailable for run {RunId} finding {FindingId}; omitting chain pointers.",
                runId,
                findingId);

            return null;
        }
    }

    private static bool TryParseRunGuid(string runId, out Guid runGuid)
    {
        return Guid.TryParseExact(runId, "N", out runGuid) || Guid.TryParse(runId, out runGuid);
    }
}
