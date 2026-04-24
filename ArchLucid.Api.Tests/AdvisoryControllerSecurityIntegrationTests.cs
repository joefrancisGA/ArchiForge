using System.Net;
using System.Text;
using System.Text.Json;

using ArchLucid.Decisioning.Advisory.Workflow;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Policy coverage for <see cref="ArchLucid.Api.Controllers.Advisory.AdvisoryController" />: anonymous callers on read
///     routes,
///     and <see cref="ArchLucid.Core.Authorization.ArchLucidPolicies.ExecuteAuthority" /> on recommendation actions.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class AdvisoryControllerSecurityIntegrationTests
{
    [Fact]
    public async Task ListRecommendations_anonymous_with_api_key_mode_returns_401()
    {
        await using HealthEndpointSecurityApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync(
            $"/v1/advisory/runs/{Guid.NewGuid():D}/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApplyRecommendationAction_reader_role_returns_403()
    {
        await using ReaderRoleArchLucidApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        using StringContent body = new(
            JsonSerializer.Serialize(new RecommendationActionRequest { Action = RecommendationActionType.Accept }),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await client.PostAsync(
            $"/v1/advisory/recommendations/{Guid.NewGuid():D}/action",
            body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
