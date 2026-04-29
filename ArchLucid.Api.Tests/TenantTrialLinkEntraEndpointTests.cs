using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>POST /v1/tenant/link-entra</c> (<see cref="Core.Authorization.ArchLucidPolicies.AdminAuthority" />).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class TenantTrialLinkEntraEndpointTests
{
    private const string EndpointPath = "/v1/tenant/link-entra";

    [Fact]
    public async Task Post_WithReaderRole_Returns403_BecauseAdminAuthorityIsRequired()
    {
        await using ReaderRoleArchLucidApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            EndpointPath,
            new { entraTenantId = Guid.Parse("88888888-8888-8888-8888-888888888888") });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "link-entra is gated on AdminAuthority; Reader role lacks it.");
    }
}
