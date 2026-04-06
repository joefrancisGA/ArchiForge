using System.Diagnostics;
using System.Text.Json;

using ArchiForge.Core.Audit;
using ArchiForge.Core.Diagnostics;
using ArchiForge.Core.Integration;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Persistence.Alerts.Helpers;
using ArchiForge.Persistence.Serialization;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Persistence.Alerts;

/// <summary>
/// Evaluates composite (multi-metric) alert rules after policy governance filtering, applies suppression policy, then persists and delivers.
/// </summary>
/// <param name="ruleRepository">Enabled composite rules for the scope.</param>
/// <param name="snapshotBuilder">Builds <see cref="AlertMetricSnapshot"/> from the evaluation context.</param>
/// <param name="ruleEvaluator">Whether a single composite rule’s predicate matches the snapshot.</param>
/// <param name="suppressionPolicy">Decides whether a match should create an alert or be suppressed.</param>
/// <param name="alertRepository">Creates <see cref="AlertRecord"/> rows for accepted matches.</param>
/// <param name="alertDeliveryDispatcher">Sends notifications for new composite alerts.</param>
/// <param name="auditService">Audits suppressions and successful triggers.</param>
/// <param name="effectiveGovernanceLoader">Fallback when <see cref="AlertEvaluationContext.EffectiveGovernanceContent"/> is not set.</param>
/// <remarks>
/// Implements <see cref="ICompositeAlertService"/>. Called alongside <see cref="AlertService"/> from advisory scan and similar orchestration paths.
/// </remarks>
public sealed class CompositeAlertService(
    ICompositeAlertRuleRepository ruleRepository,
    IAlertMetricSnapshotBuilder snapshotBuilder,
    ICompositeAlertRuleEvaluator ruleEvaluator,
    IAlertSuppressionPolicy suppressionPolicy,
    IAlertRecordRepository alertRepository,
    IAlertDeliveryDispatcher alertDeliveryDispatcher,
    IAuditService auditService,
    IEffectiveGovernanceLoader effectiveGovernanceLoader,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<CompositeAlertService> logger) : ICompositeAlertService
{
    /// <summary>
    /// Loads composite rules, filters with <see cref="PolicyPackGovernanceFilter.FilterCompositeRules"/>, evaluates each rule, and persists non-suppressed matches.
    /// </summary>
    /// <param name="context">Same context as simple alerts; may carry preloaded effective governance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created alerts and a count of matches suppressed by policy.</returns>
    public async Task<CompositeAlertEvaluationResult> EvaluateAndPersistAsync(
        AlertEvaluationContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);

        Stopwatch evaluationStopwatch = Stopwatch.StartNew();

        try
        {
            IReadOnlyList<CompositeAlertRule> rules = await ruleRepository
                .ListEnabledByScopeAsync(context.TenantId, context.WorkspaceId, context.ProjectId, ct)
                ;

            PolicyPackContentDocument effective = await AlertGovernanceResolver
                .ResolveAsync(context, effectiveGovernanceLoader, ct)
                ;

            rules = PolicyPackGovernanceFilter.FilterCompositeRules(rules, effective);

            AlertMetricSnapshot snapshot = snapshotBuilder.Build(context);
            List<AlertRecord> created = [];
            int suppressedMatches = 0;

            foreach (CompositeAlertRule rule in rules)
            {
                bool matched = ruleEvaluator.Evaluate(rule, snapshot);

                if (!matched)
                    continue;

                AlertSuppressionDecision suppression = await suppressionPolicy
                    .DecideAsync(rule, context, snapshot, ct)
                    ;

                if (!suppression.ShouldCreateAlert)
                {
                    suppressedMatches++;
                    await auditService.LogAsync(
                        new AuditEvent
                        {
                            EventType = AuditEventTypes.AlertSuppressedByPolicy,
                            RunId = context.RunId,
                            DataJson = JsonSerializer.Serialize(
                                new
                                {
                                    compositeRuleId = rule.CompositeRuleId,
                                    rule.Name,
                                    suppression.Reason,
                                    suppression.DeduplicationKey,
                                },
                                AuditJsonSerializationOptions.Instance),
                        },
                        ct);
                    continue;
                }

                string triggerSummary = BuildTriggerSummary(snapshot);
                AlertRecord alert = new()
                {
                    AlertId = Guid.NewGuid(),
                    RuleId = rule.CompositeRuleId,
                    TenantId = context.TenantId,
                    WorkspaceId = context.WorkspaceId,
                    ProjectId = context.ProjectId,
                    RunId = context.RunId,
                    ComparedToRunId = context.ComparedToRunId,
                    Title = $"Composite alert: {rule.Name}",
                    Category = AlertCategories.CompositeAlert,
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

                await alertRepository.CreateAsync(alert, ct);
                await alertDeliveryDispatcher.DeliverAsync(alert, ct);
                created.Add(alert);

                await AlertIntegrationEventPublishing.TryPublishFiredAsync(integrationEventPublisher, logger, alert, ct);

                await auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.CompositeAlertTriggered,
                        RunId = context.RunId,
                        DataJson = JsonSerializer.Serialize(
                            new
                            {
                                alertId = alert.AlertId,
                                compositeRuleId = rule.CompositeRuleId,
                                rule.Name,
                                alert.DeduplicationKey,
                            },
                            AuditJsonSerializationOptions.Instance),
                    },
                    ct);
            }

            return new CompositeAlertEvaluationResult(created, suppressedMatches);
        }
        finally
        {
            evaluationStopwatch.Stop();
            ArchiForgeInstrumentation.AlertEvaluationDurationMilliseconds.Record(
                evaluationStopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("rule_kind", "composite"));
        }
    }

    /// <summary>Compact string stored on <see cref="AlertRecord.TriggerValue"/> for operator triage.</summary>
    private static string BuildTriggerSummary(AlertMetricSnapshot s) =>
        $"CompositeRuleMatched|gaps={s.NewComplianceGapCount}|cost%={s.CostIncreasePercent:0.##}|recCH={s.CriticalRecommendationCount}";
}
