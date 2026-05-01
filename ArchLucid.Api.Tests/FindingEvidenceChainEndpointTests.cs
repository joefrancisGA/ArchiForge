using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Smoke coverage for the explainability evidence-chain route (404 on unknown run is stable contract surface).
/// </summary>
[Trait("Suite", "Integration")]
public sealed class FindingEvidenceChainEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task GetFindingEvidenceChain_WhenRunUnknown_Returns404()
    {
        Guid runId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        HttpResponseMessage response = await Client.GetAsync(
            $"/v1/architecture/run/{runId:D}/findings/any-finding/evidence-chain");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
