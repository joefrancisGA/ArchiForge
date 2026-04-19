using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;
using ArchLucid.Host.Core.Integration;

using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Jobs;

/// <summary>
/// Drains a bounded batch from the integration Service Bus subscription (peek-lock), then exits — for Container Apps Jobs + KEDA.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires live Service Bus; logic shared with AzureServiceBusIntegrationEventConsumer via IntegrationEventServiceBusMessageDispatch.")]
public sealed class ServiceBusIntegrationEventsArchLucidJob(
    IEnumerable<IIntegrationEventHandler> handlers,
    IOptionsMonitor<IntegrationEventsOptions> options,
    ILogger<ServiceBusIntegrationEventsArchLucidJob> logger) : IArchLucidJob
{
    private const int MaxMessagesPerRun = 50;

    private static readonly TimeSpan MaxRunDuration = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan ReceiveWait = TimeSpan.FromSeconds(5);

    private readonly IEnumerable<IIntegrationEventHandler> _handlers =
        handlers ?? throw new ArgumentNullException(nameof(handlers));

    private readonly IOptionsMonitor<IntegrationEventsOptions> _options =
        options ?? throw new ArgumentNullException(nameof(options));

    private readonly ILogger<ServiceBusIntegrationEventsArchLucidJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string Name => ArchLucidJobNames.ServiceBusIntegrationEvents;

    /// <inheritdoc />
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        IntegrationEventsOptions o = _options.CurrentValue;

        if (!o.ConsumerEnabled)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Integration event Service Bus job skipped (IntegrationEvents:ConsumerEnabled=false).");
            }

            return ArchLucidJobExitCodes.Success;
        }

        string? topic = o.QueueOrTopicName?.Trim();
        string? subscription = o.SubscriptionName?.Trim();

        if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(subscription))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Integration event job enabled but QueueOrTopicName or SubscriptionName is missing; exiting.");
            }

            return ArchLucidJobExitCodes.ConfigurationError;
        }

        string? fullyQualifiedNamespace = o.ServiceBusFullyQualifiedNamespace?.Trim();
        string? connectionString = o.ServiceBusConnectionString?.Trim();
        string? managedIdentityClientId = o.ServiceBusManagedIdentityClientId?.Trim();

        if (string.IsNullOrEmpty(fullyQualifiedNamespace) && string.IsNullOrEmpty(connectionString))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Integration event job enabled but neither ServiceBusFullyQualifiedNamespace nor ServiceBusConnectionString is set.");
            }

            return ArchLucidJobExitCodes.ConfigurationError;
        }

        await using ServiceBusClient client = CreateClient(fullyQualifiedNamespace, connectionString, managedIdentityClientId);
        await using ServiceBusReceiver receiver = client.CreateReceiver(
            topic,
            subscription,
            new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });

        int processed = 0;
        DateTimeOffset deadlineUtc = DateTimeOffset.UtcNow + MaxRunDuration;

        while (processed < MaxMessagesPerRun
               && DateTimeOffset.UtcNow < deadlineUtc
               && !cancellationToken.IsCancellationRequested)
        {
            int remaining = MaxMessagesPerRun - processed;
            IReadOnlyList<ServiceBusReceivedMessage> batch = await receiver
                .ReceiveMessagesAsync(Math.Min(10, remaining), ReceiveWait, cancellationToken)
                .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            ServiceBusReceiverSettlement settlement = new(receiver);

            foreach (ServiceBusReceivedMessage message in batch)
            {
                await IntegrationEventServiceBusMessageDispatch.ProcessPeekLockedMessageAsync(
                        message,
                        settlement,
                        _handlers,
                        _logger,
                        cancellationToken)
                    .ConfigureAwait(false);

                processed++;
            }
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Integration event Service Bus job finished: topic={Topic}, subscription={Subscription}, processed={Processed}.",
                LogSanitizer.Sanitize(topic),
                LogSanitizer.Sanitize(subscription),
                processed);
        }

        return ArchLucidJobExitCodes.Success;
    }

    private static ServiceBusClient CreateClient(
        string? fullyQualifiedNamespace,
        string? connectionString,
        string? managedIdentityClientId)
    {
        if (string.IsNullOrEmpty(fullyQualifiedNamespace))
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                return new ServiceBusClient(connectionString);
            }

            throw new InvalidOperationException("Service Bus namespace or connection string is required.");
        }

        TokenCredential credential = CreateCredential(managedIdentityClientId);

        return new ServiceBusClient(fullyQualifiedNamespace, credential);
    }

    private static TokenCredential CreateCredential(string? managedIdentityClientId)
    {
        DefaultAzureCredentialOptions credentialOptions = new();

        if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
        {
            credentialOptions.ManagedIdentityClientId = managedIdentityClientId.Trim();
        }

        return new DefaultAzureCredential(credentialOptions);
    }
}
