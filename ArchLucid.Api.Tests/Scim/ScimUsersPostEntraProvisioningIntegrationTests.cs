using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Scim;

/// <summary>Entra-shaped POST User — exercised against <see cref="JwtLocalSigningWebAppFactory" /> (in-memory catalog).</summary>
[Trait("Suite", "Core")]
public sealed class ScimUsersPostEntraProvisioningIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private readonly JwtLocalSigningWebAppFactory _factory;

    public ScimUsersPostEntraProvisioningIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Post_users_accepts_entra_style_payload_and_returns_user_resource()
    {
        HttpClient http = await ScimIntegrationClientFactory.CreateAuthenticatedClientAsync(_factory);
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string userName = $"scim.post.{suffix}@example.com";

        string json =
            $$"""
             {"schemas":["urn:ietf:params:scim:schemas:core:2.0:User"],"userName":"{{userName}}","name":{"givenName":"Entra","familyName":"Sim"},"emails":[{"value":"{{userName}}","type":"work","primary":true}],"active":true}
             """;

        using HttpResponseMessage response =
            await http.PostAsync("/scim/v2/Users", new StringContent(json, Encoding.UTF8, "application/scim+json"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/scim+json");

        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = doc.RootElement;

        root.GetProperty("schemas")[0].GetString().Should().Be("urn:ietf:params:scim:schemas:core:2.0:User");
        root.GetProperty("userName").GetString().Should().Be(userName);
        root.GetProperty("active").GetBoolean().Should().BeTrue();
        root.GetProperty("externalId").GetString().Should().Be(userName);
        root.TryGetProperty("id", out JsonElement idEl).Should().BeTrue();

        Guid parsedId = Guid.Parse(idEl.GetString() ?? "");
        parsedId.Should().NotBe(Guid.Empty);
    }
}
