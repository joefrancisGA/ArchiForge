using System.Text.Json;

using ArchiForge.Core.Audit;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Persistence.Alerts;

/// <summary>
/// Loads alert rules for a scope, applies policy-pack governance filtering, evaluates, persists new open alerts, delivers, and audits.
/// </summary>
/// <param name="ruleRepository">Enabled rules for tenant/workspace/project.</param>
/// <param name="alertRepository">Persistence for alert rows and deduplication lookups.</param>
/// <param name="alertEvaluator">Pure evaluation of rules against <see cref="AlertEvaluationContext"/>.</param>
/// <param name="alertDeliveryDispatcher">Outbound notification path for newly created alerts.</param>
/// <param name="auditService">Audit trail for trigger and lifecycle events.</param>
/// <param name="effectiveGovernanceLoader">Used when <see cref="AlertEvaluationContext.EffectiveGovernanceContent"/> is not pre-filled.</param>
/// <remarks>
/// Implements <see cref="IAlertService"/>. Primary callers: authority run completion, advisory scan (<see cref="ArchiForge.Persistence.Advisory.AdvisoryScanRunner"/>), and tests.
/// When the context already carries <c>EffectiveGovernanceContent</c> (e.g. advisory scan), the loader is skipped to avoid duplicate governance I/O.
/// </remarks>
public sealed class AlertService(
    IAlertRuleRepository ruleRepository,
    IAlertRecordRepository alertRepository,
    IAlertEvaluator alertEvaluator,
    IAlertDeliveryDispatcher alertDeliveryDispatcher,
    IAuditService auditService,
    IEffectiveGovernanceLoader effectiveGovernanceLoader) : IAlertService
{
    /// <summary>
    /// Lists enabled rules, filters them with <see cref="PolicyPackGovernanceFilter.FilterAlertRules"/>, evaluates, and inserts only new dedup keys.
    /// </summary>
    /// <param name="context">Run/scan context; may include precomputed <see cref="AlertEvaluationContext.EffectiveGovernanceContent"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All generated alerts plus the subset that were newly persisted.</returns>
    public async Task<AlertEvaluationOutcome> EvaluateAndPersistAsync(
        AlertEvaluationContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<AlertRule> rules = await ruleRepository
            .ListEnabledByScopeAsync(context.TenantId, context.WorkspaceId, context.ProjectId, ct)
            .ConfigureAwait(false);

        PolicyPackContentDocument effective = context.EffectiveGovernanceContent ?? await effectiveGovernanceLoader
            .LoadEffectiveContentAsync(context.TenantId, context.WorkspaceId, context.ProjectId, ct)
            .ConfigureAwait(false);

        rules = PolicyPackGovernanceFilter.FilterAlertRules(rules, effective);

        IReadOnlyList<AlertRecord> generated = alertEvaluator.Evaluate(rules, context);
        List<AlertRecord> persisted = new();

        foreach (AlertRecord alert in generated)
        {
            AlertRecord? existing = await alertRepository
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

    /// <summary>
    /// Applies acknowledge / resolve / suppress to an alert and records audit when the status changes.
    /// </summary>
    /// <param name="alertId">Alert primary key.</param>
    /// <param name="userId">Acting user id (stored on the alert).</param>
    /// <param name="userName">Display name for the acting user.</param>
    /// <param name="request">Desired action and optional comment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated alert, or <c>null</c> if <paramref name="alertId"/> was not found.</returns>
    /// <remarks>No-op (returns existing row) when action is unknown or status is unchanged.</remarks>
    public async Task<AlertRecord?> ApplyActionAsync(
        Guid alertId,
        string userId,
        string userName,
        AlertActionRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        AlertRecord? alert = await alertRepository.GetByIdAsync(alertId, ct).ConfigureAwait(false);
        if (alert is null)
            return null;

        string? newStatus = request.Action switch
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

        string? eventType = request.Action switch
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
