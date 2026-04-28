using System.Collections.Concurrent;

using ArchLucid.Core.Billing;

using JetBrains.Annotations;

namespace ArchLucid.Persistence.Billing;

public sealed class InMemoryBillingLedger : IBillingLedger
{
    private readonly ConcurrentDictionary<Guid, BillingSubRow> _subscriptions = new();

    private readonly ConcurrentDictionary<string, string> _webhookStatuses = new();

    private readonly List<BillingSubscriptionStateHistoryEntry> _stateHistory = [];

    private readonly Lock _historyGate = new();

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
        _ = _subscriptions.TryGetValue(tenantId, out BillingSubRow? previous);
        BillingSubRow next = new(
            tenantId,
            workspaceId,
            projectId,
            provider,
            providerSessionId,
            tierCode,
            seats,
            workspaces,
            "Pending");

        _subscriptions[tenantId] = next;

        RecordStateChange("UpsertPending", previous, next);

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
        _ = _subscriptions.TryGetValue(tenantId, out BillingSubRow? previous);
        BillingSubRow next = new(
            tenantId,
            workspaceId,
            projectId,
            provider,
            providerSubscriptionId,
            tierCode,
            seats,
            workspaces,
            "Active");

        _subscriptions[tenantId] = next;

        RecordStateChange("Activate", previous, next);

        return Task.CompletedTask;
    }

    public Task SuspendSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))
        {
            BillingSubRow next = row with
            {
                Status = "Suspended"
            };

            _subscriptions[tenantId] = next;

            RecordStateChange("Suspend", row, next);
        }


        return Task.CompletedTask;
    }

    public Task ReinstateSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))
        {
            BillingSubRow next = row with
            {
                Status = "Active"
            };

            _subscriptions[tenantId] = next;

            RecordStateChange("Reinstate", row, next);
        }


        return Task.CompletedTask;
    }

    public Task CancelSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))
        {
            BillingSubRow next = row with
            {
                Status = "Canceled"
            };

            _subscriptions[tenantId] = next;

            RecordStateChange("Cancel", row, next);
        }


        return Task.CompletedTask;
    }

    public Task ChangePlanAsync(Guid tenantId, string tierCode, string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))
        {
            BillingSubRow next = row with
            {
                Tier = tierCode
            };

            _subscriptions[tenantId] = next;

            RecordStateChange("ChangePlan", row, next);
        }

        return Task.CompletedTask;
    }

    public Task ChangeQuantityAsync(Guid tenantId, int seatsPurchased, string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        if (_subscriptions.TryGetValue(tenantId, out BillingSubRow? row))
        {
            BillingSubRow next = row with
            {
                Seats = seatsPurchased
            };

            _subscriptions[tenantId] = next;

            RecordStateChange("ChangeQuantity", row, next);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BillingSubscriptionStateHistoryEntry>> GetSubscriptionStateHistoryAsync(Guid tenantId,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0 || maxRows > 500)
            throw new ArgumentOutOfRangeException(nameof(maxRows));


        lock (_historyGate)
        {
            List<BillingSubscriptionStateHistoryEntry> page = _stateHistory
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(static e => e.RecordedUtc)
                .Take(maxRows)
                .ToList();

            return Task.FromResult<IReadOnlyList<BillingSubscriptionStateHistoryEntry>>(page);
        }
    }

    private void RecordStateChange(string changeKind, BillingSubRow? previous, BillingSubRow next)
    {
        lock (_historyGate)
        {
            _stateHistory.Add(new BillingSubscriptionStateHistoryEntry
            {
                HistoryId = Guid.NewGuid(),
                TenantId = next.TenantId,
                WorkspaceId = next.WorkspaceId,
                ProjectId = next.ProjectId,
                RecordedUtc = DateTimeOffset.UtcNow,
                ChangeKind = changeKind,
                PrevStatus = previous?.Status,
                NewStatus = next.Status,
                PrevTier = previous?.Tier,
                NewTier = next.Tier,
                PrevSeatsPurchased = previous?.Seats,
                NewSeatsPurchased = next.Seats,
                PrevWorkspacesPurchased = previous?.Workspaces,
                NewWorkspacesPurchased = next.Workspaces,
                PrevProvider = previous?.Provider,
                NewProvider = next.Provider,
                PrevProviderSubscriptionId = previous?.ProviderSubscriptionId,
                NewProviderSubscriptionId = next.ProviderSubscriptionId,
            });
        }
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
