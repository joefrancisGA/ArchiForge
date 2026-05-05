using System.Collections.Generic;

using ArchLucid.Core.Integration;

using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Health;

/// <summary>
///     Verifies data-plane reachability to the Azure Service Bus namespace used for integration events (same settings as
///     <see cref="ArchLucid.Host.Core.Integration.AzureServiceBusIntegrationEventPublisher" />). When Service Bus is not in use,
///     reports healthy without opening a connection.
/// </summary>
public sealed class AzureServiceBusNamespaceHealthCheck(IOptions<IntegrationEventsOptions> options) : IHealthCheck
{
    private readonly IntegrationEventsOptions _options =
        (options ?? throw new ArgumentNullException(nameof(options))).Value ??
        new IntegrationEventsOptions();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string? queueOrTopic = _options.QueueOrTopicName?.Trim();
        string? fullyQualifiedNamespace = _options.ServiceBusFullyQualifiedNamespace?.Trim();
        string? connectionString = _options.ServiceBusConnectionString?.Trim();
        string? managedIdentityClientId = _options.ServiceBusManagedIdentityClientId?.Trim();

        if (string.IsNullOrEmpty(queueOrTopic))
        {
            return HealthCheckResult.Healthy(
                "Azure Service Bus is not configured (IntegrationEvents:QueueOrTopicName is empty).",
                new Dictionary<string, object> { ["configured"] = false });
        }

        if (string.IsNullOrEmpty(fullyQualifiedNamespace) && string.IsNullOrEmpty(connectionString))
        {
            return HealthCheckResult.Degraded(
                "IntegrationEvents:QueueOrTopicName is set but neither ServiceBusFullyQualifiedNamespace nor " +
                "ServiceBusConnectionString is configured; integration event publishing is disabled.",
                data: new Dictionary<string, object> { ["configured"] = false, ["misconfigured"] = true });
        }

        try
        {
            await VerifySenderLinkAsync(
                    fullyQualifiedNamespace,
                    connectionString,
                    string.IsNullOrEmpty(managedIdentityClientId) ? null : managedIdentityClientId,
                    queueOrTopic,
                    cancellationToken)
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy(
                "Azure Service Bus namespace responded for the configured queue or topic.",
                new Dictionary<string, object> { ["configured"] = true });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("Azure Service Bus health check was canceled.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Azure Service Bus connectivity check failed for the configured namespace and entity.",
                ex,
                new Dictionary<string, object> { ["configured"] = true });
        }
    }

    private static async Task VerifySenderLinkAsync(
        string? fullyQualifiedNamespace,
        string? connectionString,
        string? managedIdentityClientId,
        string queueOrTopicName,
        CancellationToken cancellationToken)
    {
        await using ServiceBusClient client = CreateClient(
            fullyQualifiedNamespace,
            connectionString,
            managedIdentityClientId);

        await using ServiceBusSender sender = client.CreateSender(queueOrTopicName);

        using ServiceBusMessageBatch batch =
            await sender.CreateMessageBatchAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ServiceBusClient CreateClient(
        string? fullyQualifiedNamespace,
        string? connectionString,
        string? managedIdentityClientId)
    {
        if (!string.IsNullOrEmpty(fullyQualifiedNamespace))
        {
            TokenCredential credential = CreateCredential(managedIdentityClientId);

            return new ServiceBusClient(fullyQualifiedNamespace, credential);
        }

        if (!string.IsNullOrEmpty(connectionString))
            return new ServiceBusClient(connectionString);

        throw new InvalidOperationException("Service Bus credentials are unexpectedly empty.");
    }

    private static TokenCredential CreateCredential(string? managedIdentityClientId)
    {
        DefaultAzureCredentialOptions credentialOptions = new();

        if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
            credentialOptions.ManagedIdentityClientId = managedIdentityClientId.Trim();

        return new DefaultAzureCredential(credentialOptions);
    }
}
