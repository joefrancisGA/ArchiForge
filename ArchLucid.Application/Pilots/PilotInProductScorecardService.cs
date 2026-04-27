using System.Text.Json;

using ArchLucid.Application.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Pilots;

namespace ArchLucid.Application.Pilots;

public sealed class PilotInProductScorecardService(
    IScopeContextProvider scopeContextProvider,
    IPilotScorecardMetricsReader scorecardMetricsReader,
    IPilotBaselineRepository pilotBaselineRepository,
    IAuditService auditService,
    IActorContext actorContext) : IPilotInProductScorecardService
{
    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly IPilotScorecardMetricsReader _scorecardMetricsReader =
        scorecardMetricsReader ?? throw new ArgumentNullException(nameof(scorecardMetricsReader));

    private readonly IPilotBaselineRepository _pilotBaselineRepository =
        pilotBaselineRepository ?? throw new ArgumentNullException(nameof(pilotBaselineRepository));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IActorContext _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));

    public async Task<PilotInProductScorecardResult> GetAsync(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        PilotScorecardTenantMetrics m = await _scorecardMetricsReader.GetAsync(scope.TenantId, cancellationToken);
        PilotBaselineRecord? row = await _pilotBaselineRepository.GetAsync(scope.TenantId, cancellationToken);
        PilotInProductBaselinesView? baselines = row is null
            ? null
            : new PilotInProductBaselinesView
            {
                BaselineHoursPerReview = row.BaselineHoursPerReview,
                BaselineReviewsPerQuarter = row.BaselineReviewsPerQuarter,
                BaselineArchitectHourlyCost = row.BaselineArchitectHourlyCost,
                UpdatedUtc = row.UpdatedUtc
            };

        int? daysSinceFirst = m.FirstCommitUtc is { } f
            ? (int)Math.Floor((DateTimeOffset.UtcNow - f).TotalDays)
            : null;

        PilotInProductRoiEstimate? roi = TryBuildRoi(row);

        return new PilotInProductScorecardResult
        {
            TenantId = scope.TenantId,
            TotalRunsCommitted = m.TotalRunsCommitted,
            TotalManifestsCreated = m.TotalManifestsCreated,
            TotalFindingsResolved = m.TotalFindingsResolved,
            AverageTimeToManifestMinutes = m.AverageTimeToManifestMinutes,
            TotalAuditEventsGenerated = m.TotalAuditEventsGenerated,
            TotalGovernanceApprovalsCompleted = m.TotalGovernanceApprovalsCompleted,
            FirstCommitUtc = m.FirstCommitUtc,
            DaysSinceFirstCommit = daysSinceFirst,
            Baselines = baselines,
            RoiEstimate = roi
        };
    }

    public async Task UpsertBaselinesAsync(
        decimal? baselineHoursPerReview,
        int? baselineReviewsPerQuarter,
        decimal? baselineArchitectHourlyCost,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PilotBaselineRecord record = new()
        {
            TenantId = scope.TenantId,
            BaselineHoursPerReview = baselineHoursPerReview,
            BaselineReviewsPerQuarter = baselineReviewsPerQuarter,
            BaselineArchitectHourlyCost = baselineArchitectHourlyCost,
            UpdatedUtc = now
        };

        await _pilotBaselineRepository.UpsertAsync(record, cancellationToken);
        string actor = _actorContext.GetActor();
        string payload = JsonSerializer.Serialize(
            new
            {
                baselineHoursPerReview,
                baselineReviewsPerQuarter,
                baselineArchitectHourlyCost
            });

        AuditEvent auditEvent = new()
        {
            EventType = AuditEventTypes.PilotScorecardBaselinesUpdated,
            ActorUserId = actor,
            ActorUserName = actor,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            DataJson = payload,
            CorrelationId = "pilot-scorecard-baselines"
        };

        await _auditService.LogAsync(auditEvent, cancellationToken);
    }

    private static PilotInProductRoiEstimate? TryBuildRoi(PilotBaselineRecord? row)
    {
        if (row is null)
            return null;

        if (row.BaselineHoursPerReview is not { } h || row.BaselineReviewsPerQuarter is not { } q
            || row.BaselineArchitectHourlyCost is not { } c)
            return null;

        if (h <= 0m || q <= 0 || c <= 0m)
            return null;

        decimal rQ = q;
        decimal statusQuo = PilotReviewRoiFormulas.AnnualReviewCostStatusQuo(rQ, h, c);
        decimal savings = PilotReviewRoiFormulas.AnnualReviewSavings(rQ, h, c);

        return new PilotInProductRoiEstimate
        {
            AnnualReviewCostStatusQuoUsd = statusQuo,
            AnnualReviewSavingsFromReviewTimeLeverUsd = savings
        };
    }
}
