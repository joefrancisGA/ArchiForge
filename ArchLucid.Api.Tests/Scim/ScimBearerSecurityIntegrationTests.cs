using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Scim;

[Trait("Suite", "Core")]
public sealed class ScimBearerSecurityIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private readonly JwtLocalSigningWebAppFactory _factory;

    public ScimBearerSecurityIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Scim_discovery_requires_authentication()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/scim/v2/ServiceProviderConfig");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
