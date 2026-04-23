namespace ArchLucid.Core.Billing;

/// <summary>Result of Marketplace <c>ChangePlan</c> / <c>ChangeQuantity</c> application-layer mutation handling.</summary>
public enum MarketplaceWebhookMutationOutcome
{
    /// <summary>GA flag off — no ledger mutation; caller should acknowledge with HTTP 202 / skip integration publish.</summary>
    DeferredGaDisabled,

    /// <summary>Ledger mutation attempted (GA on).</summary>
    Applied
}
