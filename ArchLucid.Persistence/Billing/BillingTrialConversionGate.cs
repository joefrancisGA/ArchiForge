using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Billing;

public sealed class BillingTrialConversionGate(IOptionsMonitor<BillingOptions> options, IBillingLedger ledger)
    : IBillingTrialConversionGate
{
    private readonly IBillingLedger _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));

    private readonly IOptionsMonitor<BillingOptions> _options =
        options ?? throw new ArgumentNullException(nameof(options));
    public async Task EnsureManualConversionAllowedAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        string provider = _options.CurrentValue.Provider.Trim();

        if (string.Equals(provider, BillingProviderNames.Noop, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(provider))
            return;

        if (await _ledger.TenantHasActiveSubscriptionAsync(tenantId, cancellationToken))
            return;

        throw new BillingConversionBlockedException(
            "Billing is configured for a paid provider, but no Active subscription row exists for this tenant. "
            + "Complete checkout and wait for the billing webhook, or set Billing:Provider to Noop for lab-only environments.");
    }
}
