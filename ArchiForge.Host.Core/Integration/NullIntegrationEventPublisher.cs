using ArchiForge.Core.Integration;

namespace ArchiForge.Host.Core.Integration;

/// <summary>Default <see cref="IIntegrationEventPublisher"/> when Service Bus (or other bus) is not configured.</summary>
public sealed class NullIntegrationEventPublisher : IIntegrationEventPublisher
{
    public static readonly NullIntegrationEventPublisher Instance = new();

    private NullIntegrationEventPublisher()
    {
    }

    /// <inheritdoc />
    public Task PublishAsync(string eventType, ReadOnlyMemory<byte> utf8JsonPayload, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
