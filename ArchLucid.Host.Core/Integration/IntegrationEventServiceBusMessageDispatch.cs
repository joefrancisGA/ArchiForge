using System.Text.Json;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Logging;

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
        string eventType = ResolveEventType(message);

        if (string.IsNullOrWhiteSpace(eventType))
        {
            await settlement.DeadLetterAsync(
                    message,
                    "MissingEventType",
                    "Application property event_type and Subject were empty.",
                    cancellationToken)
                .ConfigureAwait(false);

            return;
        }

        IIntegrationEventHandler? handler = ResolveHandler(handlers, eventType);

        if (handler is null)
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
            {
                logger.LogWarning(
                    ex,
                    "Integration event handler failed; abandoning for redelivery. EventType={EventType}, deliveryCount={DeliveryCount}",
                    LogSanitizer.Sanitize(eventType),
                    message.DeliveryCount);
            }

            await settlement.AbandonAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static string ResolveEventType(ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.TryGetValue("event_type", out object? value)
            && value is string s
            && !string.IsNullOrWhiteSpace(s))
        {
            return s.Trim();
        }

        string? subject = message.Subject;

        return string.IsNullOrWhiteSpace(subject) ? string.Empty : subject.Trim();
    }

    internal static IIntegrationEventHandler? ResolveHandler(IEnumerable<IIntegrationEventHandler> handlers, string eventType)
    {
        List<IIntegrationEventHandler> list = handlers.ToList();

        IIntegrationEventHandler? specific = list.FirstOrDefault(
            h =>
                h.EventType != IntegrationEventTypes.WildcardEventType
                && IntegrationEventTypes.AreEquivalent(h.EventType, eventType));

        return specific ?? list.FirstOrDefault(h => h.EventType == IntegrationEventTypes.WildcardEventType);
    }
}
