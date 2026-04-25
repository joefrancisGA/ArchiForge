using System.Text.Json;

namespace ArchLucid.Core.Billing;

/// <summary>
///     Application-layer handler for Marketplace <c>ChangeQuantity</c> (seat count +
///     <see cref="IBillingLedger.ChangeQuantityAsync" /> when GA is on).
/// </summary>
public interface IMarketplaceChangeQuantityWebhookMutationHandler
{
    Task<MarketplaceWebhookMutationOutcome> HandleAsync(
        Guid tenantId,
        JsonElement root,
        string rawBody,
        CancellationToken cancellationToken);
}
