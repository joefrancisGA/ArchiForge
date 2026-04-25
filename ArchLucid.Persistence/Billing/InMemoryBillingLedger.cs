using System.Collections.Concurrent;

using ArchLucid.Core.Billing;

using JetBrains.Annotations;

namespace ArchLucid.Persistence.Billing;

public sealed class InMemoryBillingLedger : IBillingLedger
{
    private readonly ConcurrentDictionary<Guid, BillingSubRow> _subscriptions = new();

    private readonly ConcurrentDictionary<string, string> _webhookStatuses = new();

    public Task<bool> TenantHasActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_subscriptions.TryGetValue(tenantId, out BillingSubRow? row) &&
                               string.Equals(row.Status, "Active", StringComparison.OrdinalIgnoreCase));
    }

    public Task UpsertPendingCheckoutAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string provider,
        string providerSessionId,
        string tierCode,
        int seats,
        int workspaces,
        CancellationToken cancellationToken)
    {
        _subscriptions[tenantId] = new BillingSubRow(
            tenantId,
            workspaceId,
            projectId,
            provider,
            providerSessionId,
            tierCode,
            seats,
            workspaces,
            "Pending");

        return Task.CompletedTask;
    }

    public Task<bool> TryInsertWebhookEventAsync(
        string dedupeKey,
        string provider,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        bool added = _webhookStatuses.TryAdd(dedupeKey, "Received");

        return Task.FromResult(added);
    }

    public Task MarkWebhookProcessedAsync(string dedupeKey, string resultStatus, CancellationToken cancellationToken)
    {
        _webhookStatuses[dedupeKey] = resultStatus;

        return Task.CompletedTask;
    }

    public Task<string?> GetWebhookEventResultStatusAsync(string dedupeKey, CancellationToken cancellationToken)
    {
        return _webhookStatuses.TryGetValue(dedupeKey, out string? status)
            ? Task.FromResult<string?>(status)
            : Task.FromResult<string?>(null);
    }

    public Task ActivateSubscriptionAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string provider,
        string providerSubscriptionId,
        string tierCode,
        int seats,
        int workspaces,
        string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        _subscriptions[tenantId] = new BillingSubRow(
            tenantId,
            workspaceId,
            projectId,
            provider,
            providerSubscriptionId,
            tierCode,
            seats,
            workspaces,
            "Active");

        return Task.CompletedTask;
    }

    public Task SuspendSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))

            _subscriptions[tenantId] = row with
            {
                Status = "Suspended"
            };


        return Task.CompletedTask;
    }

    public Task ReinstateSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))

            _subscriptions[tenantId] = row with
            {
                Status = "Active"
            };


        return Task.CompletedTask;
    }

    public Task CancelSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))

            _subscriptions[tenantId] = row with
            {
                Status = "Canceled"
            };


        return Task.CompletedTask;
    }

    public Task ChangePlanAsync(Guid tenantId, string tierCode, string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))
            _subscriptions[tenantId] = row with
            {
                Tier = tierCode
            };

        return Task.CompletedTask;
    }

    public Task ChangeQuantityAsync(Guid tenantId, int seatsPurchased, string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))
            _subscriptions[tenantId] = row with
            {
                Seats = seatsPurchased
            };

        return Task.CompletedTask;
    }

    private sealed record BillingSubRow(
        [UsedImplicitly] Guid TenantId,
        Guid WorkspaceId,
        Guid ProjectId,
        string Provider,
        string ProviderSubscriptionId,
        string Tier,
        int Seats,
        int Workspaces,
        string Status);
}
