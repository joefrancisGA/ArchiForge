using ArchiForge.Core.Integration;
using ArchiForge.Decisioning.Alerts;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Persistence.Alerts;

/// <summary>Publishes alert lifecycle integration events (Service Bus) after persistence and delivery.</summary>
internal static class AlertIntegrationEventPublishing
{
    internal static Task TryPublishFiredAsync(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger logger,
        AlertRecord alert,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEventPublisher);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(alert);

        object payload = new
        {
            schemaVersion = 1,
            tenantId = alert.TenantId,
            workspaceId = alert.WorkspaceId,
            projectId = alert.ProjectId,
            alertId = alert.AlertId,
            runId = alert.RunId,
            comparedToRunId = alert.ComparedToRunId,
            ruleId = alert.RuleId,
            category = alert.Category,
            severity = alert.Severity,
            title = alert.Title,
            deduplicationKey = alert.DeduplicationKey,
        };

        string messageId = $"{alert.AlertId:D}:{IntegrationEventTypes.AlertFiredV1}";

        return IntegrationEventPublishing.TryPublishAsync(
            integrationEventPublisher,
            logger,
            IntegrationEventTypes.AlertFiredV1,
            payload,
            messageId,
            cancellationToken);
    }

    internal static Task TryPublishResolvedAsync(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger logger,
        AlertRecord alert,
        string userId,
        string? comment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEventPublisher);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(alert);

        object payload = new
        {
            schemaVersion = 1,
            tenantId = alert.TenantId,
            workspaceId = alert.WorkspaceId,
            projectId = alert.ProjectId,
            alertId = alert.AlertId,
            runId = alert.RunId,
            resolvedByUserId = userId,
            comment,
        };

        string messageId = $"{alert.AlertId:D}:{IntegrationEventTypes.AlertResolvedV1}";

        return IntegrationEventPublishing.TryPublishAsync(
            integrationEventPublisher,
            logger,
            IntegrationEventTypes.AlertResolvedV1,
            payload,
            messageId,
            cancellationToken);
    }
}
