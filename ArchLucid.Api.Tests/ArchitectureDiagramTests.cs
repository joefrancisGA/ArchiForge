using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Models;
using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Diagram.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureDiagramTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task GetManifestDiagram_ReturnsMermaid()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DIAGRAM-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        CommitRunResponseDto? commitPayload =
            await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        string manifestVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        HttpResponseMessage diagramResponse =
            await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}/diagram");

        diagramResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        DiagramResponse? diagramPayload = await diagramResponse.Content.ReadFromJsonAsync<DiagramResponse>(JsonOptions);
        diagramPayload.Should().NotBeNull();
        diagramPayload.Format.Should().Be("mermaid");
        diagramPayload.Diagram.Should().Contain("flowchart LR");
        diagramPayload.Diagram.Should().Contain("rag-api");
        diagramPayload.Diagram.Should().Contain("rag-search");
        diagramPayload.Diagram.Should().Contain("rag-metadata");
    }

    [SkippableFact]
    public async Task GetManifestDiagramV2_ReturnsManifestDiagramResponse_WithOptions()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DIAGRAM-002")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        CommitRunResponseDto? commitPayload =
            await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        string manifestVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        HttpResponseMessage diagramResponse = await Client.GetAsync(
            $"/v1/architecture/manifest/{manifestVersion}/diagram/v2?layout=TB&includeRuntimePlatform=false&relationshipLabels=none&groupBy=serviceType");

        diagramResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ManifestDiagramResponse? diagramPayload =
            await diagramResponse.Content.ReadFromJsonAsync<ManifestDiagramResponse>(JsonOptions);
        diagramPayload.Should().NotBeNull();
        diagramPayload.DiagramType.Should().Be("Mermaid");
        diagramPayload.Content.Should().Contain("flowchart TB");
        diagramPayload.Content.Should().Contain("subgraph");
        diagramPayload.Content.Should().Contain("rag-api");
    }
}
