using ArchLucid.Core.Tenancy;

namespace ArchLucid.Core.Billing;

/// <summary>Maps checkout tier to persisted <c>dbo.Tenants.Tier</c> after conversion.</summary>
public static class BillingTierCode
{
    public static string FromCheckoutTier(BillingCheckoutTier tier)
    {
        return tier switch
        {
            BillingCheckoutTier.Team => nameof(TenantTier.Standard),
            BillingCheckoutTier.Pro => nameof(TenantTier.Standard),
            BillingCheckoutTier.Enterprise => nameof(TenantTier.Enterprise),
            _ => nameof(TenantTier.Standard)
        };
    }

    public static string CheckoutTierLabel(BillingCheckoutTier tier)
    {
        return tier switch
        {
            BillingCheckoutTier.Team => "Team",
            BillingCheckoutTier.Pro => "Pro",
            BillingCheckoutTier.Enterprise => "Enterprise",
            _ => "Team"
        };
    }
}
