using System.Net;
using System.Net.Http.Headers;

using ArchLucid.Application.Scim.Tokens;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests.Scim;

[Trait("Suite", "Core")]
public sealed class ScimDiscoveryAuthenticatedIntegrationTests : IClassFixture<JwtLocalSigningWebAppFactory>
{
    private readonly JwtLocalSigningWebAppFactory _factory;

    public ScimDiscoveryAuthenticatedIntegrationTests(JwtLocalSigningWebAppFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task ServiceProviderConfig_returns_application_scim_json_when_scim_bearer_accepted()
    {
        HttpClient http = _factory.CreateClient();

        using (IServiceScope scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            IScimTokenIssuer issuer = scope.ServiceProvider.GetRequiredService<IScimTokenIssuer>();
            ScimTokenIssueResult minted = await issuer.IssueTokenAsync(Guid.NewGuid(), CancellationToken.None);

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", minted.PlaintextToken);
        }

        HttpResponseMessage response = await http.GetAsync("/scim/v2/ServiceProviderConfig");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/scim+json");

        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"patch\"");
    }

    [SkippableFact]
    public async Task ResourceTypes_lists_user_and_group_endpoints()
    {
        HttpClient http = _factory.CreateClient();

        using (IServiceScope scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            IScimTokenIssuer issuer = scope.ServiceProvider.GetRequiredService<IScimTokenIssuer>();
            ScimTokenIssueResult minted = await issuer.IssueTokenAsync(Guid.NewGuid(), CancellationToken.None);

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", minted.PlaintextToken);
        }

        HttpResponseMessage response = await http.GetAsync("/scim/v2/ResourceTypes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("/Users");
        body.Should().Contain("/Groups");
    }
}
