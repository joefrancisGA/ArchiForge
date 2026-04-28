using System.Collections.Concurrent;

using ArchLucid.Core.Billing;

namespace ArchLucid.TestSupport.Billing;

/// <summary>
///     Test double that records <see cref="IBillingLedger.ChangePlanAsync" /> /
///     <see cref="IBillingLedger.ChangeQuantityAsync" />
///     invocations before delegating. The SQL ledger maps those methods to Dapper calls against
///     <c>dbo.sp_Billing_ChangePlan</c>
///     and <c>dbo.sp_Billing_ChangeQuantity</c>; this recorder is the lightweight stand-in for a Dapper command
///     interceptor when
///     wiring full SQL hosts in <c>WebApplicationFactory</c> tests.
/// </summary>
public sealed class BillingLedgerDapperDispatchRecorder(
    IBillingLedger inner,
    ConcurrentBag<string> recordedLogicalProcedures)
    : IBillingLedger
{
    private readonly IBillingLedger _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly ConcurrentBag<string> _recordedLogicalProcedures =
        recordedLogicalProcedures ?? throw new ArgumentNullException(nameof(recordedLogicalProcedures));

    public Task<bool> TenantHasActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _inner.TenantHasActiveSubscriptionAsync(tenantId, cancellationToken);
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
        return _inner.UpsertPendingCheckoutAsync(
            tenantId,
            workspaceId,
            projectId,
            provider,
            providerSessionId,
            tierCode,
            seats,
            workspaces,
            cancellationToken);
    }

    public Task<bool> TryInsertWebhookEventAsync(
        string dedupeKey,
        string provider,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        return _inner.TryInsertWebhookEventAsync(dedupeKey, provider, eventType, payloadJson, cancellationToken);
    }

    public Task MarkWebhookProcessedAsync(string dedupeKey, string resultStatus, CancellationToken cancellationToken)
    {
        return _inner.MarkWebhookProcessedAsync(dedupeKey, resultStatus, cancellationToken);
    }

    public Task<string?> GetWebhookEventResultStatusAsync(string dedupeKey, CancellationToken cancellationToken)
    {
        return _inner.GetWebhookEventResultStatusAsync(dedupeKey, cancellationToken);
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
        return _inner.ActivateSubscriptionAsync(
            tenantId,
            workspaceId,
            projectId,
            provider,
            providerSubscriptionId,
            tierCode,
            seats,
            workspaces,
            rawWebhookJson,
            cancellationToken);
    }

    public Task SuspendSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _inner.SuspendSubscriptionAsync(tenantId, cancellationToken);
    }

    public Task ReinstateSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _inner.ReinstateSubscriptionAsync(tenantId, cancellationToken);
    }

    public Task CancelSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _inner.CancelSubscriptionAsync(tenantId, cancellationToken);
    }

    public async Task ChangePlanAsync(Guid tenantId, string tierCode, string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        _recordedLogicalProcedures.Add("dbo.sp_Billing_ChangePlan");

        await _inner.ChangePlanAsync(tenantId, tierCode, rawWebhookJson, cancellationToken);
    }

    public async Task ChangeQuantityAsync(Guid tenantId, int seatsPurchased, string? rawWebhookJson,
        CancellationToken cancellationToken)
    {
        _recordedLogicalProcedures.Add("dbo.sp_Billing_ChangeQuantity");

        await _inner.ChangeQuantityAsync(tenantId, seatsPurchased, rawWebhookJson, cancellationToken);
    }

    public Task<IReadOnlyList<BillingSubscriptionStateHistoryEntry>> GetSubscriptionStateHistoryAsync(Guid tenantId,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        return _inner.GetSubscriptionStateHistoryAsync(tenantId, maxRows, cancellationToken);
    }
}
