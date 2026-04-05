using System.Diagnostics.CodeAnalysis;
using System.Text;

using ArchiForge.Core.Integration;

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Host.Core.Integration;

/// <summary>Publishes UTF-8 JSON payloads to an Azure Service Bus queue or topic.</summary>
[ExcludeFromCodeCoverage(Justification = "Requires live Service Bus; exercised via integration tests if configured.")]
public sealed class AzureServiceBusIntegrationEventPublisher : IIntegrationEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureServiceBusIntegrationEventPublisher> _logger;

    public AzureServiceBusIntegrationEventPublisher(
        string connectionString,
        string queueOrTopicName,
        ILogger<AzureServiceBusIntegrationEventPublisher> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueOrTopicName);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(queueOrTopicName);
    }

    /// <inheritdoc />
    public async Task PublishAsync(string eventType, ReadOnlyMemory<byte> utf8JsonPayload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        ServiceBusMessage message = new(utf8JsonPayload.ToArray())
        {
            ContentType = "application/json",
            Subject = eventType,
            ApplicationProperties =
            {
                ["event_type"] = eventType,
            },
        };

        try
        {
            await _sender.SendMessageAsync(message, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "Failed to publish integration event type {EventType} to Service Bus.", eventType);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
