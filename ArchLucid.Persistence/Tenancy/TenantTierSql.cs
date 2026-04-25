using ArchLucid.Core.Tenancy;

namespace ArchLucid.Persistence.Tenancy;

internal static class TenantTierSql
{
    internal static string ToTierString(TenantTier tier)
    {
        return tier switch
        {
            TenantTier.Free => "Free",
            TenantTier.Standard => "Standard",
            TenantTier.Enterprise => "Enterprise",
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
        };
    }

    internal static TenantTier ParseTier(string value)
    {
        return value switch
        {
            "Free" => TenantTier.Free,
            "Standard" => TenantTier.Standard,
            "Enterprise" => TenantTier.Enterprise,
            _ => TenantTier.Standard
        };
    }
}
