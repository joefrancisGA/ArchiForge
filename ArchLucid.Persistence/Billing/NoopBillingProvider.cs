using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;

namespace ArchLucid.Persistence.Billing;

public sealed class NoopBillingProvider(IBillingLedger ledger) : IBillingProvider
{
    private readonly IBillingLedger _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));

    public string ProviderName => BillingProviderNames.Noop;

    public async Task<BillingCheckoutResult> CreateCheckoutSessionAsync(
        BillingCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        string sessionId = $"noop_sess_{Guid.NewGuid():N}";
        string tierCode = BillingTierCode.FromCheckoutTier(request.TargetTier);

        await _ledger.UpsertPendingCheckoutAsync(
            request.TenantId,
            request.WorkspaceId,
            request.ProjectId,
            ProviderName,
            sessionId,
            tierCode,
            Math.Max(1, request.Seats),
            Math.Max(1, request.Workspaces),
            cancellationToken);

        return new BillingCheckoutResult
        {
            CheckoutUrl = $"https://billing.archlucid.local/noop-checkout?session={sessionId}",
            ProviderSessionId = sessionId,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
        };
    }

    public Task<BillingWebhookHandleResult> HandleWebhookAsync(
        BillingWebhookInbound inbound,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(BillingWebhookHandleResult.Rejected("Noop billing provider does not accept webhooks."));
    }
}
