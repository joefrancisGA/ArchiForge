using ArchLucid.Api.Filters;
using ArchLucid.Core.Tenancy;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Attributes;

/// <summary>
/// Declares the minimum <see cref="TenantTier"/> required for the action or controller (enforced by <see cref="CommercialTenantTierFilter"/>).
/// </summary>
public sealed class RequiresCommercialTenantTierAttribute : TypeFilterAttribute
{
    /// <summary>Passes <paramref name="minimumTier"/> as the first constructor argument to <see cref="CommercialTenantTierFilter"/>.</summary>
    public RequiresCommercialTenantTierAttribute(TenantTier minimumTier)
        : base(typeof(CommercialTenantTierFilter))
    {
        Arguments = [minimumTier];
    }
}
