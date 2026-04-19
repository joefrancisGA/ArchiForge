using Azure.Messaging.ServiceBus;

namespace ArchLucid.Host.Core.Integration;

internal readonly struct ProcessMessageEventArgsSettlement(ProcessMessageEventArgs args) : IIntegrationEventPeekLockSettlement
{
    public Task CompleteAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken) =>
        args.CompleteMessageAsync(message, cancellationToken);

    public Task AbandonAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken) =>
        args.AbandonMessageAsync(message, cancellationToken: cancellationToken);

    public Task DeadLetterAsync(
        ServiceBusReceivedMessage message,
        string deadLetterReason,
        string deadLetterErrorDescription,
        CancellationToken cancellationToken) =>
        args.DeadLetterMessageAsync(message, deadLetterReason, deadLetterErrorDescription, cancellationToken);
}

internal readonly struct ServiceBusReceiverSettlement(ServiceBusReceiver receiver) : IIntegrationEventPeekLockSettlement
{
    public Task CompleteAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken) =>
        receiver.CompleteMessageAsync(message, cancellationToken);

    public Task AbandonAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken) =>
        receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);

    public Task DeadLetterAsync(
        ServiceBusReceivedMessage message,
        string deadLetterReason,
        string deadLetterErrorDescription,
        CancellationToken cancellationToken) =>
        receiver.DeadLetterMessageAsync(message, deadLetterReason, deadLetterErrorDescription, cancellationToken);
}
