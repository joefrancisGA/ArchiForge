using System.Text.Json;
using ArchiForge.Core.Audit;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Persistence.Alerts;

public sealed class CompositeAlertService(
    ICompositeAlertRuleRepository ruleRepository,
    IAlertMetricSnapshotBuilder snapshotBuilder,
    ICompositeAlertRuleEvaluator ruleEvaluator,
    IAlertSuppressionPolicy suppressionPolicy,
    IAlertRecordRepository alertRepository,
    IAlertDeliveryDispatcher alertDeliveryDispatcher,
    IAuditService auditService,
    IEffectiveGovernanceLoader effectiveGovernanceLoader) : ICompositeAlertService
{
    public async Task<CompositeAlertEvaluationResult> EvaluateAndPersistAsync(
        AlertEvaluationContext context,
        CancellationToken ct)
    {
        var rules = await ruleRepository
            .ListEnabledByScopeAsync(context.TenantId, context.WorkspaceId, context.ProjectId, ct)
            .ConfigureAwait(false);

        var effective = context.EffectiveGovernanceContent ?? await effectiveGovernanceLoader
            .LoadEffectiveContentAsync(context.TenantId, context.WorkspaceId, context.ProjectId, ct)
            .ConfigureAwait(false);

        rules = PolicyPackGovernanceFilter.FilterCompositeRules(rules, effective);

        var snapshot = snapshotBuilder.Build(context);
        var created = new List<AlertRecord>();
        var suppressedMatches = 0;

        foreach (var rule in rules)
        {
            var matched = ruleEvaluator.Evaluate(rule, snapshot);
            if (!matched)
                continue;

            var suppression = await suppressionPolicy
                .DecideAsync(rule, context, snapshot, ct)
                .ConfigureAwait(false);

            if (!suppression.ShouldCreateAlert)
            {
                suppressedMatches++;
                await auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.AlertSuppressedByPolicy,
                        RunId = context.RunId,
                        DataJson = JsonSerializer.Serialize(new
                        {
                            compositeRuleId = rule.CompositeRuleId,
                            rule.Name,
                            suppression.Reason,
                            suppression.DeduplicationKey,
                        }),
                    },
                    ct).ConfigureAwait(false);
                continue;
            }

            var triggerSummary = BuildTriggerSummary(snapshot);
            var alert = new AlertRecord
            {
                AlertId = Guid.NewGuid(),
                RuleId = rule.CompositeRuleId,
                TenantId = context.TenantId,
                WorkspaceId = context.WorkspaceId,
                ProjectId = context.ProjectId,
                RunId = context.RunId,
                ComparedToRunId = context.ComparedToRunId,
                Title = $"Composite alert: {rule.Name}",
                Category = "CompositeAlert",
                Severity = rule.Severity,
                Status = AlertStatus.Open,
                TriggerValue = triggerSummary,
                Description =
                    $"{suppression.Reason} Metrics: critical/high recs={snapshot.CriticalRecommendationCount}, " +
                    $"compliance gaps={snapshot.NewComplianceGapCount}, costΔ%={snapshot.CostIncreasePercent:0.##}, " +
                    $"deferred high-pri={snapshot.DeferredHighPriorityRecommendationCount}, " +
                    $"rejected security={snapshot.RejectedSecurityRecommendationCount}, " +
                    $"acceptance%={snapshot.AcceptanceRatePercent:0.##}.",
                CreatedUtc = DateTime.UtcNow,
                DeduplicationKey = suppression.DeduplicationKey,
            };

            await alertRepository.CreateAsync(alert, ct).ConfigureAwait(false);
            await alertDeliveryDispatcher.DeliverAsync(alert, ct).ConfigureAwait(false);
            created.Add(alert);

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.CompositeAlertTriggered,
                    RunId = context.RunId,
                    DataJson = JsonSerializer.Serialize(new
                    {
                        alertId = alert.AlertId,
                        compositeRuleId = rule.CompositeRuleId,
                        rule.Name,
                        alert.DeduplicationKey,
                    }),
                },
                ct).ConfigureAwait(false);
        }

        return new CompositeAlertEvaluationResult(created, suppressedMatches);
    }

    private static string BuildTriggerSummary(AlertMetricSnapshot s) =>
        $"CompositeRuleMatched|gaps={s.NewComplianceGapCount}|cost%={s.CostIncreasePercent:0.##}|recCH={s.CriticalRecommendationCount}";
}
