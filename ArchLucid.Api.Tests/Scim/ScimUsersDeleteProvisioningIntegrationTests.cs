using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Scim;

/// <summary>DELETE User triggers ArchLucid SCIM soft-deprovision semantics (inactive directory row).</summary>
[Trait("Suite", "Core")]
public sealed class ScimUsersDeleteProvisioningIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private readonly JwtLocalSigningWebAppFactory _factory;

    public ScimUsersDeleteProvisioningIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Delete_users_returns_204_then_get_returns_scim_not_found()
    {
        HttpClient http = await ScimIntegrationClientFactory.CreateAuthenticatedClientAsync(_factory);
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string userName = $"scim.delete.{suffix}@example.com";

        string createBody =
            $$"""
             {"schemas":["urn:ietf:params:scim:schemas:core:2.0:User"],"userName":"{{userName}}","active":true}
             """;

        using HttpResponseMessage created =
            await http.PostAsync("/scim/v2/Users", new StringContent(createBody, Encoding.UTF8, "application/scim+json"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid userId = Guid.Parse(JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString() ?? "");

        using HttpResponseMessage deleted = await http.DeleteAsync($"/scim/v2/Users/{userId:D}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using HttpResponseMessage missing = await http.GetAsync($"/scim/v2/Users/{userId:D}");

        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string body = await missing.Content.ReadAsStringAsync();
        body.Should().Contain("notFound");
    }
}
