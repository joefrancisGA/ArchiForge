using System.Net;

using ArchLucid.Api.Tests;

using ArchLucid.Core.Tenancy;

using ArchLucid.Persistence.Tenancy;

using FluentAssertions;

using Xunit;

namespace ArchLucid.Api.Tests.Alerts;

/// <summary>
///     Isolated <see cref="ArchLucid.Api.Tests.ArchLucidApiFactory" /> (<see cref="InMemoryTenantRepository" /> singleton)
///     so Facts that flip <see cref="TenantTier" /> cannot race parallel integration tests sharing the default tenant scope.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class AlertsCommercialTierPackagingIntegrationTests : IDisposable
{
    private readonly ArchLucidApiFactory _factory = new();

    /// <inheritdoc />
    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task Get_alerts_requires_standard_tier_free_returns_404_then_standard_returns_200()
    {
        HttpClient client = _factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        await CommercialTierIntegrationTestTenant.SetDefaultScopedTenantTierAsync(_factory, TenantTier.Free);

        using (HttpResponseMessage free = await client.GetAsync("/v1/alerts"))
        {
            free.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        await CommercialTierIntegrationTestTenant.SetDefaultScopedTenantTierAsync(_factory, TenantTier.Standard);

        using (HttpResponseMessage std = await client.GetAsync("/v1/alerts"))
        {
            std.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
