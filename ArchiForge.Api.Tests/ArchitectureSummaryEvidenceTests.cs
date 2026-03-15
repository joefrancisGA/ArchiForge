using System.Net;
using System.Net.Http.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureSummaryEvidenceTests : IntegrationTestBase
{
    public ArchitectureSummaryEvidenceTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetManifestSummary_IncludesEvidenceContext()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-SUMMARY-EVIDENCE-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        var manifestVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        var summaryResponse = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}/summary");

        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var summaryPayload = await summaryResponse.Content.ReadFromJsonAsync<ManifestSummaryResponse>(JsonOptions);
        summaryPayload.Should().NotBeNull();
        summaryPayload!.Summary.Should().Contain("## Evidence Context");
        summaryPayload.Summary.Should().Contain("### Policy Evidence");
        summaryPayload.Summary.Should().Contain("### Service Catalog Hints");
        summaryPayload.Summary.Should().Contain("Managed Identity");
        summaryPayload.Summary.Should().Contain("Azure AI Search");
    }
}
