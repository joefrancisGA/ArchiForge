using System.Data;
using System.Text.Json;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Persistence;

/// <summary>
/// Publishes integration events through <see cref="IIntegrationEventOutboxRepository"/> when
/// <see cref="IntegrationEventsOptions.TransactionalOutboxEnabled"/> is true; otherwise uses
/// <see cref="IntegrationEventPublishing.TryPublishAsync"/> (best-effort direct Service Bus).
/// </summary>
public static class OutboxAwareIntegrationEventPublishing
{
    /// <summary>
    /// When <paramref name="connection"/> and <paramref name="transaction"/> are both set and the outbox is enabled,
    /// enqueues in the same SQL transaction as the caller’s commit. Otherwise enqueues on a standalone connection or
    /// publishes directly, matching <see cref="IntegrationEventsOptions.TransactionalOutboxEnabled"/>.
    /// </summary>
    public static async Task TryPublishOrEnqueueAsync(
        IIntegrationEventOutboxRepository outbox,
        IIntegrationEventPublisher publisher,
        IntegrationEventsOptions options,
        ILogger logger,
        string eventType,
        object payload,
        string? messageId,
        Guid? runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IDbConnection? connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(payload);

        if (options.TransactionalOutboxEnabled)
        {
            byte[] utf8;
            try
            {
                utf8 = JsonSerializer.SerializeToUtf8Bytes(payload, IntegrationEventJson.Options);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Warning))

                    logger.LogWarning(ex, "Integration event serialization failed for {EventType}", eventType);


                return;
            }

            ReadOnlyMemory<byte> mem = utf8;

            try
            {
                if (connection is not null && transaction is not null)
                {
                    await outbox.EnqueueAsync(
                        runId,
                        eventType,
                        messageId,
                        mem,
                        tenantId,
                        workspaceId,
                        projectId,
                        connection,
                        transaction,
                        ct);

                    return;
                }

                await outbox.EnqueueAsync(runId, eventType, messageId, mem, tenantId, workspaceId, projectId, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Warning))

                    logger.LogWarningIntegrationEventOutboxEnqueueFailed(ex, eventType);
            }

            return;
        }

        await IntegrationEventPublishing.TryPublishAsync(publisher, logger, eventType, payload, messageId, ct);
    }
}
