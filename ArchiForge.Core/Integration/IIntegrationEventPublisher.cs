namespace ArchiForge.Core.Integration;

/// <summary>
/// Publishes JSON integration events to external systems (e.g. Azure Service Bus). Default implementation is a no-op.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <param name="eventType">Logical type (e.g. <c>com.archiforge.authority.run.completed</c>).</param>
    /// <param name="utf8JsonPayload">UTF-8 JSON body for the message.</param>
    Task PublishAsync(string eventType, ReadOnlyMemory<byte> utf8JsonPayload, CancellationToken cancellationToken = default);
}
