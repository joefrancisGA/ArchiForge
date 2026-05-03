using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Policy smoke tests for <see cref="ArchLucid.Api.Controllers.Advisory.DigestSubscriptionsController" />.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class DigestSubscriptionsControllerAuthorizationIntegrationTests
{
    [SkippableFact]
    public async Task List_anonymous_returns_unauthorized()
    {
        await using HealthEndpointSecurityApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/digest-subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task Create_reader_returns_forbidden()
    {
        await using ReaderRoleArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        using StringContent body = new(
            """{"name":"d","channelType":"Email","destination":"x@example.test","metadataJson":"{}"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await client.PostAsync("/v1/digest-subscriptions", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
