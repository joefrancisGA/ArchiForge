using System.Text.Json;

using ArchLucid.Application.Notifications.Email;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Integration;

using ArchLucid.Host.Core.Auth.Services;

using ArchLucid.Persistence;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Notifications.Email;

/// <summary>
/// Wraps <see cref="AuditService"/> so durable audit append paths can enqueue trial lifecycle email integration events
/// without introducing HTTP controller call sites.
/// </summary>
public sealed class TrialLifecycleEmailPublishingAuditDecorator(
    AuditService inner,
    IIntegrationEventOutboxRepository outboxRepository,
    IIntegrationEventPublisher integrationEventPublisher,
    IOptionsMonitor<IntegrationEventsOptions> integrationEventsOptions,
    ILogger<TrialLifecycleEmailPublishingAuditDecorator> logger) : IAuditService
{
    private readonly AuditService _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly IIntegrationEventOutboxRepository _outboxRepository =
        outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));

    private readonly IIntegrationEventPublisher _integrationEventPublisher =
        integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));

    private readonly IOptionsMonitor<IntegrationEventsOptions> _integrationEventsOptions =
        integrationEventsOptions ?? throw new ArgumentNullException(nameof(integrationEventsOptions));

    private readonly ILogger<TrialLifecycleEmailPublishingAuditDecorator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        await _inner.LogAsync(auditEvent, cancellationToken).ConfigureAwait(false);

        try
        {
            await TryPublishTrialEmailAsync(auditEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    ex,
                    "Trial lifecycle email integration publish failed after audit append for event {EventId}.",
                    auditEvent.EventId);
            }
        }
    }

    private async Task TryPublishTrialEmailAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        TrialLifecycleEmailIntegrationEnvelope? envelope = TryMap(auditEvent);

        if (envelope is null)
        {
            return;
        }

        IntegrationEventsOptions options = _integrationEventsOptions.CurrentValue;
        string messageId = $"trial-email-audit|{auditEvent.EventId:N}";

        await TrialLifecycleIntegrationEventPublisher
            .TryPublishAsync(
                _outboxRepository,
                _integrationEventPublisher,
                options,
                _logger,
                envelope,
                messageId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static TrialLifecycleEmailIntegrationEnvelope? TryMap(AuditEvent auditEvent)
    {
        if (string.Equals(auditEvent.EventType, AuditEventTypes.TrialProvisioned, StringComparison.Ordinal))
        {
            return new TrialLifecycleEmailIntegrationEnvelope
            {
                SchemaVersion = 1,
                Trigger = TrialLifecycleEmailTrigger.TrialProvisioned,
                TenantId = auditEvent.TenantId,
                WorkspaceId = auditEvent.WorkspaceId,
                ProjectId = auditEvent.ProjectId,
                RunId = auditEvent.RunId,
            };
        }

        if (string.Equals(auditEvent.EventType, AuditEventTypes.CoordinatorRunCommitCompleted, StringComparison.Ordinal))
        {
            return new TrialLifecycleEmailIntegrationEnvelope
            {
                SchemaVersion = 1,
                Trigger = TrialLifecycleEmailTrigger.FirstRunCommitted,
                TenantId = auditEvent.TenantId,
                WorkspaceId = auditEvent.WorkspaceId,
                ProjectId = auditEvent.ProjectId,
                RunId = auditEvent.RunId,
            };
        }

        if (!string.Equals(auditEvent.EventType, AuditEventTypes.TenantTrialConverted, StringComparison.Ordinal))
            return null;

        string? targetTier = TryReadTargetTier(auditEvent.DataJson);

        return new TrialLifecycleEmailIntegrationEnvelope
        {
            SchemaVersion = 1,
            Trigger = TrialLifecycleEmailTrigger.Converted,
            TenantId = auditEvent.TenantId,
            WorkspaceId = auditEvent.WorkspaceId,
            ProjectId = auditEvent.ProjectId,
            RunId = auditEvent.RunId,
            TargetTier = targetTier,
        };

    }

    private static string? TryReadTargetTier(string? dataJson)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(dataJson);

            if (doc.RootElement.TryGetProperty("targetTier", out JsonElement tier))
            {
                return tier.ValueKind == JsonValueKind.String ? tier.GetString() : null;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
