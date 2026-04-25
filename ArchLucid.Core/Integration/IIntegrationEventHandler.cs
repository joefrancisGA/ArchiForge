namespace ArchLucid.Core.Integration;

/// <summary>In-process handler for messages received from the integration-events Service Bus subscription.</summary>
public interface IIntegrationEventHandler
{
    /// <summary>
    ///     Logical event type this handler serves (canonical <c>com.archlucid.*</c>), or
    ///     <see cref="IntegrationEventTypes.WildcardEventType" /> for catch-all.
    /// </summary>
    string EventType
    {
        get;
    }

    Task HandleAsync(ReadOnlyMemory<byte> utf8JsonPayload, CancellationToken ct);
}
