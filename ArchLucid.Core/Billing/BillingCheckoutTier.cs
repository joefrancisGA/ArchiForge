namespace ArchLucid.Core.Billing;

/// <summary>Commercial SKU selected at checkout (maps to <see cref="Tenancy.TenantTier" /> after activation).</summary>
public enum BillingCheckoutTier
{
    Team = 0,

    Pro = 1,

    Enterprise = 2
}
