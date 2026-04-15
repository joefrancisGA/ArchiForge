using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Regression: requesting API version 2 on versioned routes must not silently map to v1 controllers.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class ApiVersioningUnmappedV2IntegrationTests
{
    [Fact]
    public async Task Get_v2_architecture_runs_is_not_routed_to_v1_controllers()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v2/architecture/runs");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
