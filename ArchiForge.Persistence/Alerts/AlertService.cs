using System.Text.Json;
using ArchiForge.Core.Audit;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Persistence.Alerts;

public sealed class AlertService(
    IAlertRuleRepository ruleRepository,
    IAlertRecordRepository alertRepository,
    IAlertEvaluator alertEvaluator,
    IAlertDeliveryDispatcher alertDeliveryDispatcher,
    IAuditService auditService,
    IEffectiveGovernanceLoader effectiveGovernanceLoader) : IAlertService
{
    public async Task<AlertEvaluationOutcome> EvaluateAndPersistAsync(
        AlertEvaluationContext context,
        CancellationToken ct)
    {
        var rules = await ruleRepository
            .ListEnabledByScopeAsync(context.TenantId, context.WorkspaceId, context.ProjectId, ct)
            .ConfigureAwait(false);

        var effective = context.EffectiveGovernanceContent ?? await effectiveGovernanceLoader
            .LoadEffectiveContentAsync(context.TenantId, context.WorkspaceId, context.ProjectId, ct)
            .ConfigureAwait(false);

        rules = PolicyPackGovernanceFilter.FilterAlertRules(rules, effective);

        var generated = alertEvaluator.Evaluate(rules, context);
        var persisted = new List<AlertRecord>();

        foreach (var alert in generated)
        {
            var existing = await alertRepository
                .GetOpenByDeduplicationKeyAsync(
                    context.TenantId,
                    context.WorkspaceId,
                    context.ProjectId,
                    alert.DeduplicationKey,
                    ct)
                .ConfigureAwait(false);

            if (existing is not null)
                continue;

            await alertRepository.CreateAsync(alert, ct).ConfigureAwait(false);
            persisted.Add(alert);

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.AlertTriggered,
                    RunId = context.RunId,
                    DataJson = JsonSerializer.Serialize(new
                    {
                        alertId = alert.AlertId,
                        ruleId = alert.RuleId,
                        alert.Title,
                        alert.Severity,
                        alert.DeduplicationKey,
                    }),
                },
                ct).ConfigureAwait(false);

            await alertDeliveryDispatcher.DeliverAsync(alert, ct).ConfigureAwait(false);
        }

        return new AlertEvaluationOutcome(generated, persisted);
    }

    public async Task<AlertRecord?> ApplyActionAsync(
        Guid alertId,
        string userId,
        string userName,
        AlertActionRequest request,
        CancellationToken ct)
    {
        var alert = await alertRepository.GetByIdAsync(alertId, ct).ConfigureAwait(false);
        if (alert is null)
            return null;

        var newStatus = request.Action switch
        {
            AlertActionType.Acknowledge => AlertStatus.Acknowledged,
            AlertActionType.Resolve => AlertStatus.Resolved,
            AlertActionType.Suppress => AlertStatus.Suppressed,
            _ => null,
        };

        if (newStatus is null || string.Equals(alert.Status, newStatus, StringComparison.Ordinal))
            return alert;

        alert.Status = newStatus;

        alert.AcknowledgedByUserId = userId;
        alert.AcknowledgedByUserName = userName;
        alert.ResolutionComment = request.Comment;
        alert.LastUpdatedUtc = DateTime.UtcNow;

        await alertRepository.UpdateAsync(alert, ct).ConfigureAwait(false);

        var eventType = request.Action switch
        {
            AlertActionType.Acknowledge => AuditEventTypes.AlertAcknowledged,
            AlertActionType.Resolve => AuditEventTypes.AlertResolved,
            AlertActionType.Suppress => AuditEventTypes.AlertSuppressed,
            _ => null,
        };

        if (eventType is not null)
        {
            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = eventType,
                    RunId = alert.RunId,
                    DataJson = JsonSerializer.Serialize(new
                    {
                        alertId,
                        request.Action,
                        comment = request.Comment,
                    }),
                },
                ct).ConfigureAwait(false);
        }

        return alert;
    }
}
