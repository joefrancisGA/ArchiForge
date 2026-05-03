using System.Net;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Scim;

/// <summary>RFC 7644 §4 discovery assertions beyond smoke coverage — patch/filter/authentication schemes.</summary>
[Trait("Suite", "Core")]
public sealed class ScimServiceProviderConfigCapabilitiesIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private readonly JwtLocalSigningWebAppFactory _factory;

    public ScimServiceProviderConfigCapabilitiesIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task ServiceProviderConfig_documents_patch_filter_and_oauth_bearer_scheme()
    {
        HttpClient http = await ScimIntegrationClientFactory.CreateAuthenticatedClientAsync(_factory);

        using HttpResponseMessage response = await http.GetAsync("/scim/v2/ServiceProviderConfig");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/scim+json");

        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = doc.RootElement;

        root.GetProperty("patch").GetProperty("supported").GetBoolean().Should().BeTrue();
        root.GetProperty("filter").GetProperty("supported").GetBoolean().Should().BeTrue();
        root.GetProperty("filter").GetProperty("maxResults").GetInt32().Should().Be(200);

        JsonElement schemes = root.GetProperty("authenticationSchemes");
        schemes.GetArrayLength().Should().BeGreaterThan(0);

        JsonElement first = schemes[0];
        first.GetProperty("type").GetString().Should().Be("oauthbearertoken");
        first.GetProperty("name").GetString().Should().Contain("Bearer");
    }
}
