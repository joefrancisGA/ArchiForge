using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;

using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Integration;

/// <summary>Pulls JSON integration events from a Service Bus topic subscription and dispatches to <see cref="IIntegrationEventHandler"/>.</summary>
[ExcludeFromCodeCoverage(Justification = "Requires live Service Bus; covered by configuration and handler unit tests.")]
public sealed class AzureServiceBusIntegrationEventConsumer(
    IEnumerable<IIntegrationEventHandler> handlers,
    IOptionsMonitor<IntegrationEventsOptions> options,
    ILogger<AzureServiceBusIntegrationEventConsumer> logger)
    : BackgroundService
{
    private readonly IEnumerable<IIntegrationEventHandler> _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
    private readonly IOptionsMonitor<IntegrationEventsOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<AzureServiceBusIntegrationEventConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            try
            {
                await _processor.StopProcessingAsync(cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "Service Bus integration event processor stop failed.");
                }
            }

            await _processor.DisposeAsync();
            _processor = null;
        }

        if (_client is not null)
        {
            await _client.DisposeAsync();
            _client = null;
        }

        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IntegrationEventsOptions o = _options.CurrentValue;

        if (!o.ConsumerEnabled)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Integration event Service Bus consumer is disabled (IntegrationEvents:ConsumerEnabled=false).");
            }

            return;
        }

        string? topic = o.QueueOrTopicName?.Trim();
        string? subscription = o.SubscriptionName?.Trim();

        if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(subscription))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Integration event consumer enabled but QueueOrTopicName or SubscriptionName is missing; consumer not started.");
            }

            return;
        }

        string? fullyQualifiedNamespace = o.ServiceBusFullyQualifiedNamespace?.Trim();
        string? connectionString = o.ServiceBusConnectionString?.Trim();
        string? managedIdentityClientId = o.ServiceBusManagedIdentityClientId?.Trim();

        if (string.IsNullOrEmpty(fullyQualifiedNamespace) && string.IsNullOrEmpty(connectionString))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Integration event consumer enabled but neither ServiceBusFullyQualifiedNamespace nor ServiceBusConnectionString is set.");
            }

            return;
        }

        try
        {
            _client = CreateClient(fullyQualifiedNamespace, connectionString, managedIdentityClientId);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to create Service Bus client for integration event consumer.");
            }

            return;
        }

        int concurrent = Math.Clamp(o.MaxConcurrentCalls, 1, 64);
        int prefetch = Math.Max(0, o.PrefetchCount);

        ServiceBusProcessorOptions processorOptions = new()
        {
            MaxConcurrentCalls = concurrent,
            PrefetchCount = prefetch,
            AutoCompleteMessages = false,
        };

        _processor = _client.CreateProcessor(topic, subscription, processorOptions);
        _processor.ProcessMessageAsync += OnProcessMessageAsync;
        _processor.ProcessErrorAsync += OnProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Integration event Service Bus consumer started: topic={Topic}, subscription={Subscription}, maxConcurrentCalls={MaxConcurrentCalls}.",
                LogSanitizer.Sanitize(topic),
                LogSanitizer.Sanitize(subscription),
                concurrent);
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    private Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(
                args.Exception,
                "Service Bus processor error: {ErrorSource}, entity={EntityPath}",
                LogSanitizer.Sanitize(args.ErrorSource.ToString()),
                LogSanitizer.Sanitize(args.EntityPath));
        }

        return Task.CompletedTask;
    }

    private async Task OnProcessMessageAsync(ProcessMessageEventArgs args)
    {
        string eventType = ResolveEventType(args.Message);

        if (string.IsNullOrWhiteSpace(eventType))
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "MissingEventType",
                "Application property event_type and Subject were empty.");

            return;
        }

        IIntegrationEventHandler? handler = ResolveHandler(eventType);

        if (handler is null)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "NoHandler",
                $"No IIntegrationEventHandler registered for event type '{eventType}'.");

            return;
        }

        try
        {
            await handler.HandleAsync(args.Message.Body, args.CancellationToken);
            await args.CompleteMessageAsync(args.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FormatException)
        {
            await args.DeadLetterMessageAsync(args.Message, "BadPayload", "Handler rejected payload format.");
        }
        catch (JsonException)
        {
            await args.DeadLetterMessageAsync(args.Message, "BadPayload", "Invalid JSON in handler.");
        }
        catch (Exception ex) when (!args.CancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    ex,
                    "Integration event handler failed; abandoning for redelivery. EventType={EventType}, deliveryCount={DeliveryCount}",
                    LogSanitizer.Sanitize(eventType),
                    args.Message.DeliveryCount);
            }

            await args.AbandonMessageAsync(args.Message);
        }
    }

    private IIntegrationEventHandler? ResolveHandler(string eventType)
    {
        List<IIntegrationEventHandler> list = _handlers.ToList();

        IIntegrationEventHandler? specific = list.FirstOrDefault(
            h =>
                h.EventType != IntegrationEventTypes.WildcardEventType
                && IntegrationEventTypes.AreEquivalent(h.EventType, eventType));

        return specific ?? list.FirstOrDefault(h => h.EventType == IntegrationEventTypes.WildcardEventType);
    }

    private static string ResolveEventType(ServiceBusReceivedMessage message)
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

    private static ServiceBusClient CreateClient(
        string? fullyQualifiedNamespace,
        string? connectionString,
        string? managedIdentityClientId)
    {
        if (string.IsNullOrEmpty(fullyQualifiedNamespace))
            return !string.IsNullOrEmpty(connectionString)
                ? new ServiceBusClient(connectionString)
                : throw new InvalidOperationException("Service Bus namespace or connection string is required.");
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
