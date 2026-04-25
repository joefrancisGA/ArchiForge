using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>GET /v1/architecture/run/{runId}/traceability-bundle.zip</c> (404 contract for unknown
///     runs).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class TraceabilityBundleZipEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetTraceabilityBundleZip_WhenRunUnknown_Returns404Problem()
    {
        Guid runId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        using HttpResponseMessage res =
            await Client.GetAsync($"/v1/architecture/run/{runId:D}/traceability-bundle.zip");

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
