using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture End To End Compare Run Not Found.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureEndToEndCompareRunNotFoundTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task CompareRunsEndToEnd_WhenRunMissing_Returns404RunNotFound()
    {
        HttpResponseMessage response = await Client.GetAsync(
            "/v1/architecture/run/compare/end-to-end?leftRunId=missing-left&rightRunId=missing-right");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("run-not-found");
        json.Should().Contain("Run Not Found");
    }
}
