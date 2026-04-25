using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Anonymous callers cannot list authority runs when the host uses API-key mode (no DevelopmentBypass).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class AuthorityQueryControllerAnonymousIntegrationTests
{
    [Fact]
    public async Task ListRunsByProject_anonymous_returns_401()
    {
        await using HealthEndpointSecurityApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/authority/projects/default/runs?take=5");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
