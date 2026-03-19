using System.Net;
using System.Net.Http.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureSummaryTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetManifestSummary_ReturnsMarkdown()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-SUMMARY-001")));

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
        summaryPayload!.Format.Should().Be("markdown");
        summaryPayload.Content.Should().Contain("# Architecture Summary: EnterpriseRag");
        summaryPayload.Content.Should().Contain("## Services");
        summaryPayload.Content.Should().Contain("rag-api");
        summaryPayload.Content.Should().Contain("rag-search");
        summaryPayload.Content.Should().Contain("## Required Controls");
        summaryPayload.Content.Should().Contain("Managed Identity");
    }
}
