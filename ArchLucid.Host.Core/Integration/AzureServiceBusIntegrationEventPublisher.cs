using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;

using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;

namespace ArchLucid.Host.Core.Integration;

/// <summary>Publishes UTF-8 JSON payloads to an Azure Service Bus queue or topic.</summary>
[ExcludeFromCodeCoverage(Justification = "Requires live Service Bus; exercised via integration tests if configured.")]
public sealed class AzureServiceBusIntegrationEventPublisher : IIntegrationEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureServiceBusIntegrationEventPublisher> _logger;

    /// <summary>Connection-string auth (legacy).</summary>
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

    /// <summary>Managed identity (or other <see cref="DefaultAzureCredential"/>) auth to the namespace.</summary>
    public AzureServiceBusIntegrationEventPublisher(
        string fullyQualifiedNamespace,
        string queueOrTopicName,
        string? managedIdentityClientId,
        ILogger<AzureServiceBusIntegrationEventPublisher> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueOrTopicName);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        TokenCredential credential = CreateCredential(managedIdentityClientId);
        _client = new ServiceBusClient(fullyQualifiedNamespace, credential);
        _sender = _client.CreateSender(queueOrTopicName);
    }

    private static TokenCredential CreateCredential(string? managedIdentityClientId)
    {
        DefaultAzureCredentialOptions options = new();

        if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
        {
            options.ManagedIdentityClientId = managedIdentityClientId.Trim();
        }

        return new DefaultAzureCredential(options);
    }

    /// <inheritdoc />
    public Task PublishAsync(string eventType, ReadOnlyMemory<byte> utf8JsonPayload, CancellationToken cancellationToken = default)
    {
        return PublishAsync(eventType, utf8JsonPayload, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        string eventType,
        ReadOnlyMemory<byte> utf8JsonPayload,
        string? messageId,
        CancellationToken cancellationToken)
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

        if (!string.IsNullOrEmpty(messageId))
        {
            message.MessageId = TrimMessageId(messageId);
        }

        try
        {
            await _sender.SendMessageAsync(message, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "Failed to publish integration event type {EventType} to Service Bus.", LogSanitizer.Sanitize(eventType));
            }

            throw;
        }
    }

    /// <summary>Service Bus message ids are limited to 128 characters.</summary>
    private static string TrimMessageId(string messageId)
    {
        const int maxLen = 128;

        return messageId.Length <= maxLen ? messageId : messageId[..maxLen];
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
