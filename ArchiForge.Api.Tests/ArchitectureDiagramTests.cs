using System.Net;
using System.Net.Http.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureDiagramTests : IntegrationTestBase
{
    public ArchitectureDiagramTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetManifestDiagram_ReturnsMermaid()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DIAGRAM-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        var manifestVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        var diagramResponse = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}/diagram");

        diagramResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var diagramPayload = await diagramResponse.Content.ReadFromJsonAsync<DiagramResponse>(JsonOptions);
        diagramPayload.Should().NotBeNull();
        diagramPayload!.Format.Should().Be("mermaid");
        diagramPayload.Diagram.Should().Contain("flowchart LR");
        diagramPayload.Diagram.Should().Contain("rag-api");
        diagramPayload.Diagram.Should().Contain("rag-search");
        diagramPayload.Diagram.Should().Contain("rag-metadata");
    }

    [Fact]
    public async Task GetManifestDiagramV2_ReturnsManifestDiagramResponse_WithOptions()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DIAGRAM-002")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        var manifestVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        var diagramResponse = await Client.GetAsync(
            $"/v1/architecture/manifest/{manifestVersion}/diagram/v2?layout=TB&includeRuntimePlatform=false&relationshipLabels=none&groupBy=serviceType");

        diagramResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var diagramPayload = await diagramResponse.Content.ReadFromJsonAsync<ManifestDiagramResponse>(JsonOptions);
        diagramPayload.Should().NotBeNull();
        diagramPayload!.DiagramType.Should().Be("Mermaid");
        diagramPayload.Content.Should().Contain("flowchart TB");
        diagramPayload.Content.Should().Contain("subgraph");
        diagramPayload.Content.Should().Contain("rag-api");
    }
}
