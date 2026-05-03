using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Scim;

/// <summary>PATCH User with flat replace operations — matches typical Entra soft-disable + display name refresh shape.</summary>
[Trait("Suite", "Core")]
public sealed class ScimUsersPatchEntraProvisioningIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private readonly JwtLocalSigningWebAppFactory _factory;

    public ScimUsersPatchEntraProvisioningIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Patch_users_replace_displayName_and_active_entra_shape_updates_resource()
    {
        HttpClient http = await ScimIntegrationClientFactory.CreateAuthenticatedClientAsync(_factory);
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string userName = $"scim.patch.{suffix}@example.com";

        string createBody =
            $$"""
             {"schemas":["urn:ietf:params:scim:schemas:core:2.0:User"],"userName":"{{userName}}","active":true}
             """;

        using HttpResponseMessage created =
            await http.PostAsync("/scim/v2/Users", new StringContent(createBody, Encoding.UTF8, "application/scim+json"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid userId = Guid.Parse(JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString() ?? "");

        string patchBody =
            """
            {"schemas":["urn:ietf:params:scim:api:messages:2.0:PatchOp"],"Operations":[{"op":"replace","path":"displayName","value":"Entra Patch Display"},{"op":"replace","path":"active","value":false}]}
            """;

        using HttpResponseMessage patched =
            await http.PatchAsync($"/scim/v2/Users/{userId:D}",
                new StringContent(patchBody, Encoding.UTF8, "application/scim+json"));

        patched.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument patchedDoc = JsonDocument.Parse(await patched.Content.ReadAsStringAsync());
        patchedDoc.RootElement.GetProperty("displayName").GetString().Should().Be("Entra Patch Display");
        patchedDoc.RootElement.GetProperty("active").GetBoolean().Should().BeFalse();

        using HttpResponseMessage roundTrip = await http.GetAsync($"/scim/v2/Users/{userId:D}");

        roundTrip.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument getDoc = JsonDocument.Parse(await roundTrip.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("displayName").GetString().Should().Be("Entra Patch Display");
        getDoc.RootElement.GetProperty("active").GetBoolean().Should().BeFalse();
    }
}
