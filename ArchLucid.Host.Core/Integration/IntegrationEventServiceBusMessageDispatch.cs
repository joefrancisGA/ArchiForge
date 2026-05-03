using System.Text.Json;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;

using Azure.Messaging.ServiceBus;

namespace ArchLucid.Host.Core.Integration;

/// <summary>Shared integration JSON dispatch + peek-lock settlement (consumer long-poll vs job batch drain).</summary>
internal static class IntegrationEventServiceBusMessageDispatch
{
    internal static async Task ProcessPeekLockedMessageAsync(
        ServiceBusReceivedMessage message,
        IIntegrationEventPeekLockSettlement settlement,
        IEnumerable<IIntegrationEventHandler> handlers,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        string rawEventType = ResolveEventType(message);

        if (string.IsNullOrWhiteSpace(rawEventType))
        {
            await settlement.DeadLetterAsync(
                    message,
                    "MissingEventType",
                    "Application property event_type and Subject were empty.",
                    cancellationToken)
                .ConfigureAwait(false);

            return;
        }

        string eventType = IntegrationEventTypes.MapToCanonical(rawEventType);

        IReadOnlyList<IIntegrationEventHandler> resolved = ResolveHandlers(handlers, eventType);

        if (resolved.Count == 0)
        {
            await settlement.DeadLetterAsync(
                    message,
                    "NoHandler",
                    $"No IIntegrationEventHandler registered for event type '{eventType}'.",
                    cancellationToken)
                .ConfigureAwait(false);

            return;
        }

        try
        {
            foreach (IIntegrationEventHandler handler in resolved)
                await handler.HandleAsync(message.Body, cancellationToken).ConfigureAwait(false);

            await settlement.CompleteAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FormatException)
        {
            await settlement.DeadLetterAsync(message, "BadPayload", "Handler rejected payload format.", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            await settlement.DeadLetterAsync(message, "BadPayload", "Invalid JSON in handler.", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Warning))

                logger.LogWarning(
                    ex,
                    "Integration event handler failed; abandoning for redelivery. EventType={EventType}, deliveryCount={DeliveryCount}",
                    LogSanitizer.Sanitize(eventType),
                    message.DeliveryCount);

            await settlement.AbandonAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static string ResolveEventType(ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.TryGetValue("event_type", out object? value)
            && value is string s
            && !string.IsNullOrWhiteSpace(s))

            return s.Trim();

        string? subject = message.Subject;

        return string.IsNullOrWhiteSpace(subject) ? string.Empty : subject.Trim();
    }

    /// <summary>
    /// Returns every non-wildcard handler whose <see cref="IIntegrationEventHandler.EventType"/> matches
    /// <paramref name="eventType"/>; if none, returns all wildcard handlers (typically logging).
    /// </summary>
    internal static IReadOnlyList<IIntegrationEventHandler> ResolveHandlers(
        IEnumerable<IIntegrationEventHandler> handlers,
        string eventType)
    {
        List<IIntegrationEventHandler> list = handlers.ToList();

        List<IIntegrationEventHandler> specifics =
        [
            .. list.Where(h =>
                h.EventType != IntegrationEventTypes.WildcardEventType
                && IntegrationEventTypes.AreEquivalent(h.EventType, eventType)),
        ];

        return specifics.Count > 0 ? specifics : [.. list.Where(h => h.EventType == IntegrationEventTypes.WildcardEventType)];
    }
}
