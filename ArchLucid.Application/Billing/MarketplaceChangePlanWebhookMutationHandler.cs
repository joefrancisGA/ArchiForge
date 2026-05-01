using System.Text.Json;

using ArchLucid.Core.Billing;
using ArchLucid.Core.Billing.AzureMarketplace;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Billing;

/// <summary>
///     Maps Marketplace <c>ChangePlan</c> payloads to <see cref="IBillingLedger.ChangePlanAsync" /> when GA is
///     enabled.
/// </summary>
public sealed class MarketplaceChangePlanWebhookMutationHandler(
    IOptionsMonitor<BillingOptions> billingOptions,
    IBillingLedger ledger,
    ILogger<MarketplaceChangePlanWebhookMutationHandler> logger) : IMarketplaceChangePlanWebhookMutationHandler
{
    private readonly IOptionsMonitor<BillingOptions> _billingOptions =
        billingOptions ?? throw new ArgumentNullException(nameof(billingOptions));

    private readonly IBillingLedger _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));

    private readonly ILogger<MarketplaceChangePlanWebhookMutationHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<MarketplaceWebhookMutationOutcome> HandleAsync(
        Guid tenantId,
        JsonElement root,
        string rawBody,
        CancellationToken cancellationToken)
    {
        BillingOptions snapshot = _billingOptions.CurrentValue;

        if (!BillingPlanMutationPolicy.WebhookPlanMutationsEnabled(snapshot))
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "ChangePlan acknowledged without subscription mutation (Billing:AzureMarketplace:GaEnabled=false and Billing:Provider is not Stripe). TenantId={TenantId}",
                    tenantId);
            }

            return MarketplaceWebhookMutationOutcome.DeferredGaDisabled;
        }

        string tierCode = MarketplaceWebhookPayloadParser.TryGetPlanId(root, out string? planId)
            ? MarketplaceWebhookPayloadParser.TierStorageCodeFromPlanId(planId)
            : nameof(TenantTier.Standard);

        await _ledger.ChangePlanAsync(tenantId, tierCode, rawBody, cancellationToken);

        return MarketplaceWebhookMutationOutcome.Applied;
    }
}
