using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Collection("ArchLucidEnvMutation")]
public sealed class RoiBulletinAdminControllerIntegrationTests : IClassFixture<GreenfieldSqlApiFactory>
{
    private readonly GreenfieldSqlApiFactory _fixture;

    public RoiBulletinAdminControllerIntegrationTests(GreenfieldSqlApiFactory fixture) => _fixture = fixture;

    [Fact]
    public async Task Preview_invalid_quarter_returns_400()
    {
        using HttpClient client = _fixture.CreateClient();

        using HttpResponseMessage res = await client.GetAsync("/v1/admin/roi-bulletin-preview?quarter=bad&minTenants=1");

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Preview_below_min_tenants_returns_400()
    {
        using HttpClient client = _fixture.CreateClient();

        using HttpResponseMessage res = await client.GetAsync("/v1/admin/roi-bulletin-preview?quarter=Q1-2099&minTenants=99999");

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("99999");
    }
}
