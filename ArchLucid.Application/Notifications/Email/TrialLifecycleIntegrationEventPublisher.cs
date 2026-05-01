using ArchLucid.Core.Integration;
using ArchLucid.Persistence;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Notifications.Email;

/// <summary>Publishes <see cref="IntegrationEventTypes.TrialLifecycleEmailV1" /> through the transactional outbox helper.</summary>
public static class TrialLifecycleIntegrationEventPublisher
{
    public static Task TryPublishAsync(
        IIntegrationEventOutboxRepository outbox,
        IIntegrationEventPublisher publisher,
        IntegrationEventsOptions options,
        ILogger logger,
        TrialLifecycleEmailIntegrationEnvelope envelope,
        string messageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return OutboxAwareIntegrationEventPublishing.TryPublishOrEnqueueAsync(
            outbox,
            publisher,
            options,
            logger,
            IntegrationEventTypes.TrialLifecycleEmailV1,
            envelope,
            messageId,
            envelope.RunId,
            envelope.TenantId,
            envelope.WorkspaceId,
            envelope.ProjectId,
            null,
            null,
            cancellationToken);
    }
}
