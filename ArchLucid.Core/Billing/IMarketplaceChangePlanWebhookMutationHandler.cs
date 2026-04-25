using System.Text.Json;

namespace ArchLucid.Core.Billing;

/// <summary>
///     Application-layer handler for Marketplace <c>ChangePlan</c> (tier mapping +
///     <see cref="IBillingLedger.ChangePlanAsync" /> when GA is on).
/// </summary>
public interface IMarketplaceChangePlanWebhookMutationHandler
{
    Task<MarketplaceWebhookMutationOutcome> HandleAsync(
        Guid tenantId,
        JsonElement root,
        string rawBody,
        CancellationToken cancellationToken);
}
