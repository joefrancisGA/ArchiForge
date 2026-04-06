namespace ArchiForge.Persistence.Integration;

/// <summary>Drains <see cref="IIntegrationEventOutboxRepository"/> and publishes via <see cref="ArchiForge.Core.Integration.IIntegrationEventPublisher"/>.</summary>
public interface IIntegrationEventOutboxProcessor
{
    Task ProcessPendingBatchAsync(CancellationToken ct);
}
