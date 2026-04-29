using ArchLucid.Application;
using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Pilots;

/// <inheritdoc cref="ISponsorEvidencePackService" />
public sealed class SponsorEvidencePackService(
    IWhyArchLucidSnapshotService whyArchLucidSnapshotService,
    IRunDetailQueryService runDetailQueryService,
    IPilotRunDeltaComputer pilotRunDeltaComputer,
    IFindingsSnapshotRepository findingsSnapshotRepository,
    IGovernanceDashboardService governanceDashboardService,
    IScopeContextProvider scopeContextProvider,
    ILogger<SponsorEvidencePackService> logger) : ISponsorEvidencePackService
{
    private const int GovernanceListCap = 50;

    private readonly IWhyArchLucidSnapshotService _whyArchLucidSnapshotService =
        whyArchLucidSnapshotService ?? throw new ArgumentNullException(nameof(whyArchLucidSnapshotService));

    private readonly IRunDetailQueryService _runDetailQueryService =
        runDetailQueryService ?? throw new ArgumentNullException(nameof(runDetailQueryService));

    private readonly IPilotRunDeltaComputer _pilotRunDeltaComputer =
        pilotRunDeltaComputer ?? throw new ArgumentNullException(nameof(pilotRunDeltaComputer));

    private readonly IFindingsSnapshotRepository _findingsSnapshotRepository =
        findingsSnapshotRepository ?? throw new ArgumentNullException(nameof(findingsSnapshotRepository));

    private readonly IGovernanceDashboardService _governanceDashboardService =
        governanceDashboardService ?? throw new ArgumentNullException(nameof(governanceDashboardService));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly ILogger<SponsorEvidencePackService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<SponsorEvidencePackResponse> BuildAsync(CancellationToken cancellationToken)
    {
        WhyArchLucidSnapshotResponse process = await _whyArchLucidSnapshotService.BuildAsync(cancellationToken);
        string demoRunId = process.DemoRunId;

        ArchitectureRunDetail? detail = await _runDetailQueryService.GetRunDetailAsync(demoRunId, cancellationToken);

        PilotRunDeltasResponse? deltas = null;

        if (detail is not null)
        {
            PilotRunDeltas computed = await _pilotRunDeltaComputer.ComputeAsync(detail, cancellationToken);
            deltas = PilotRunDeltasResponseMapper.ToResponse(computed);
        }

        FindingsSnapshot resolved = await ResolveFindingsSnapshotAsync(detail, cancellationToken);
        TraceCompletenessSummary traceSummary = ExplainabilityTraceCompletenessAnalyzer.AnalyzeSnapshot(resolved);
        ExplainabilityTraceCompletenessPack explainability = SponsorEvidenceExplainabilityMapper.ToContract(traceSummary);

        SponsorEvidenceGovernanceOutcomes governance = await TryBuildGovernanceOutcomesAsync(cancellationToken);

        return new SponsorEvidencePackResponse
        {
            GeneratedUtc = process.GeneratedUtc,
            DemoRunId = demoRunId,
            ProcessInstrumentation = process,
            ExplainabilityTrace = explainability,
            DemoRunValueReportDelta = deltas,
            GovernanceOutcomes = governance,
        };
    }

    private async Task<FindingsSnapshot> ResolveFindingsSnapshotAsync(
        ArchitectureRunDetail? detail,
        CancellationToken cancellationToken)
    {
        if (detail?.Run.FindingsSnapshotId is not { } snapshotId)

            return new FindingsSnapshot { Findings = [] };

        try
        {
            FindingsSnapshot? loaded = await _findingsSnapshotRepository.GetByIdAsync(snapshotId, cancellationToken);

            if (loaded is not null)
                return loaded;

            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(
                    "Sponsor evidence pack: findings snapshot {SnapshotId} not found for demo run.",
                    snapshotId);

            return new FindingsSnapshot { Findings = [], FindingsSnapshotId = snapshotId };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(ex, "Sponsor evidence pack: findings snapshot load failed.");

            return new FindingsSnapshot { Findings = [] };
        }
    }

    private async Task<SponsorEvidenceGovernanceOutcomes> TryBuildGovernanceOutcomesAsync(
        CancellationToken cancellationToken)
    {
        Guid tenantId = _scopeContextProvider.GetCurrentScope().TenantId;

        try
        {
            GovernanceDashboardSummary dash = await _governanceDashboardService.GetDashboardAsync(
                tenantId,
                maxPending: GovernanceListCap,
                maxDecisions: GovernanceListCap,
                maxChanges: GovernanceListCap,
                cancellationToken);

            return new SponsorEvidenceGovernanceOutcomes
            {
                PendingApprovalCount = dash.PendingCount,
                RecentTerminalDecisionCount = dash.RecentDecisions.Count,
                RecentPolicyPackChangeCount = dash.RecentChanges.Count,
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(ex, "Sponsor evidence pack: governance dashboard unavailable; returning zeros.");

            return new SponsorEvidenceGovernanceOutcomes
            {
                PendingApprovalCount = 0,
                RecentTerminalDecisionCount = 0,
                RecentPolicyPackChangeCount = 0,
            };
        }
    }
}
