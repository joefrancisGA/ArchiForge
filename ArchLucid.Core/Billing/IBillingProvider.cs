namespace ArchLucid.Core.Billing;

/// <summary>Payment / marketplace integration behind <see cref="IBillingProviderRegistry" />.</summary>
public interface IBillingProvider
{
    string ProviderName
    {
        get;
    }

    Task<BillingCheckoutResult> CreateCheckoutSessionAsync(
        BillingCheckoutRequest request,
        CancellationToken cancellationToken);

    Task<BillingWebhookHandleResult> HandleWebhookAsync(
        BillingWebhookInbound inbound,
        CancellationToken cancellationToken);
}
