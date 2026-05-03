using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Scim;

/// <summary>GET Users with Entra-style <c>filter</c> literals (URL-encoded).</summary>
[Trait("Suite", "Core")]
public sealed class ScimUsersGetFilterEntraProvisioningIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private readonly JwtLocalSigningWebAppFactory _factory;

    public ScimUsersGetFilterEntraProvisioningIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Get_users_filter_userName_eq_entra_literal_returns_single_resource()
    {
        HttpClient http = await ScimIntegrationClientFactory.CreateAuthenticatedClientAsync(_factory);
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string userName = $"scim.filter.un.{suffix}@example.com";

        string createBody =
            $$"""
             {"schemas":["urn:ietf:params:scim:schemas:core:2.0:User"],"userName":"{{userName}}","active":true}
             """;

        using HttpResponseMessage created =
            await http.PostAsync("/scim/v2/Users", new StringContent(createBody, Encoding.UTF8, "application/scim+json"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        string filter = $"userName eq \"{userName}\"";
        string uri = "/scim/v2/Users?filter=" + Uri.EscapeDataString(filter);

        using HttpResponseMessage listed = await http.GetAsync(uri);

        listed.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument doc = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        JsonElement resources = doc.RootElement.GetProperty("Resources");

        resources.GetArrayLength().Should().Be(1);
        resources[0].GetProperty("userName").GetString().Should().Be(userName);
    }

    [SkippableFact]
    public async Task Get_users_filter_emails_work_value_eq_entra_literal_returns_single_resource()
    {
        HttpClient http = await ScimIntegrationClientFactory.CreateAuthenticatedClientAsync(_factory);
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string userName = $"scim.filter.em.{suffix}@example.com";

        string createBody =
            $$"""
             {"schemas":["urn:ietf:params:scim:schemas:core:2.0:User"],"userName":"{{userName}}","emails":[{"value":"{{userName}}","type":"work"}],"active":true}
             """;

        using HttpResponseMessage created =
            await http.PostAsync("/scim/v2/Users", new StringContent(createBody, Encoding.UTF8, "application/scim+json"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        string filter = $"emails[type eq \"work\"].value eq \"{userName}\"";
        string uri = "/scim/v2/Users?filter=" + Uri.EscapeDataString(filter);

        using HttpResponseMessage listed = await http.GetAsync(uri);

        listed.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument doc = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        JsonElement resources = doc.RootElement.GetProperty("Resources");

        resources.GetArrayLength().Should().Be(1);
        resources[0].GetProperty("userName").GetString().Should().Be(userName);
    }
}
