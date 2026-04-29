using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using ArchLucid.Persistence.Tenancy;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Integration hosts using <see cref="ArchLucid.Api.Tests.ArchLucidApiFactory" /> (<c>ArchLucid:StorageProvider=InMemory</c>)
///     resolve commercial tier via <see cref="ITenantRepository" /> (singleton <see cref="InMemoryTenantRepository" />), not SQL.
/// </summary>
internal static class CommercialTierIntegrationTestTenant
{
    public static async Task SetDefaultScopedTenantTierAsync(
        ArchLucidApiFactory factory,
        TenantTier tier,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = factory.Services.CreateScope();

        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();

        if (tenants is not InMemoryTenantRepository inMemory)
        {
            throw new InvalidOperationException(
                $"Commercial tier tests require singleton {nameof(InMemoryTenantRepository)} (InMemory ArchLucid:StorageProvider). Found {tenants?.GetType().FullName ?? "null"}");
        }

        await inMemory.SetCommercialTierForIntegrationTestsAsync(ScopeIds.DefaultTenant, tier, cancellationToken);
    }
}
