using Azure.Messaging.ServiceBus;

namespace ArchLucid.Host.Core.Integration;

/// <summary>Peek-lock settlement surface shared by <see cref="AzureServiceBusIntegrationEventConsumer"/> and one-shot jobs.</summary>
internal interface IIntegrationEventPeekLockSettlement
{
    Task CompleteAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken);

    Task AbandonAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken);

    Task DeadLetterAsync(
        ServiceBusReceivedMessage message,
        string deadLetterReason,
        string deadLetterErrorDescription,
        CancellationToken cancellationToken);
}
