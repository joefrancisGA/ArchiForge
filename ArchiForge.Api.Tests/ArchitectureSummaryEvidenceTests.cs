using System.Net;
using System.Net.Http.Json;

using ArchiForge.Api.Models;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureSummaryEvidenceTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetManifestSummary_IncludesEvidenceContext()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-SUMMARY-EVIDENCE-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        CommitRunResponseDto? commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        string manifestVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        HttpResponseMessage summaryResponse = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}/summary");

        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ManifestSummaryResponse? summaryPayload = await summaryResponse.Content.ReadFromJsonAsync<ManifestSummaryResponse>(JsonOptions);
        summaryPayload.Should().NotBeNull();
        summaryPayload.Summary.Should().Contain("## Evidence Context");
        summaryPayload.Summary.Should().Contain("### Policy Evidence");
        summaryPayload.Summary.Should().Contain("### Service Catalog Hints");
        summaryPayload.Summary.Should().Contain("Managed Identity");
        summaryPayload.Summary.Should().Contain("Azure AI Search");
    }
}
