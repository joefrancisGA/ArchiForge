using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Ensures <see cref="ArchLucid.Api.Controllers.Governance.GovernanceResolutionController" /> rejects anonymous callers when auth mode is API key (no DevelopmentBypass universal principal).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class GovernanceResolutionControllerAuthorizationIntegrationTests
{
    [SkippableFact]
    public async Task Resolve_anonymous_returns_unauthorized()
    {
        await using HealthEndpointSecurityApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/governance-resolution");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
