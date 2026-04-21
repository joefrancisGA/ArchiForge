using System.Text.Json;

using ArchLucid.Core.Billing;
using ArchLucid.Core.Billing.AzureMarketplace;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Billing;

/// <summary>Maps Marketplace <c>ChangeQuantity</c> payloads to <see cref="IBillingLedger.ChangeQuantityAsync"/> when GA is enabled.</summary>
public sealed class MarketplaceChangeQuantityWebhookMutationHandler(
    IOptionsMonitor<BillingOptions> billingOptions,
    IBillingLedger ledger,
    ILogger<MarketplaceChangeQuantityWebhookMutationHandler> logger) : IMarketplaceChangeQuantityWebhookMutationHandler
{
    private readonly IOptionsMonitor<BillingOptions> _billingOptions =
        billingOptions ?? throw new ArgumentNullException(nameof(billingOptions));

    private readonly IBillingLedger _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));

    private readonly ILogger<MarketplaceChangeQuantityWebhookMutationHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<MarketplaceWebhookMutationOutcome> HandleAsync(
        Guid tenantId,
        JsonElement root,
        string rawBody,
        CancellationToken cancellationToken)
    {
        if (!_billingOptions.CurrentValue.AzureMarketplace.GaEnabled)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Marketplace ChangeQuantity acknowledged without subscription mutation (Billing:AzureMarketplace:GaEnabled=false). TenantId={TenantId}",
                    tenantId);
            }

            return MarketplaceWebhookMutationOutcome.DeferredGaDisabled;
        }

        int seats = MarketplaceWebhookPayloadParser.ReadQuantity(root);

        await _ledger.ChangeQuantityAsync(tenantId, seats, rawBody, cancellationToken);

        return MarketplaceWebhookMutationOutcome.Applied;
    }
}
