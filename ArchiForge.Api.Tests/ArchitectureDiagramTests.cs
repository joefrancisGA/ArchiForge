using System.Net;
using System.Net.Http.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(new JsonOptions().JsonSerializerOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(new JsonOptions().JsonSerializerOptions);
        var manifestVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        var diagramResponse = await Client.GetAsync($"/v1/architecture/manifest/{manifestVersion}/diagram");

        diagramResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var diagramPayload = await diagramResponse.Content.ReadFromJsonAsync<DiagramResponse>(new JsonOptions().JsonSerializerOptions);
        diagramPayload.Should().NotBeNull();
        diagramPayload!.Format.Should().Be("mermaid");
        diagramPayload.Diagram.Should().Contain("flowchart TD");
        diagramPayload.Diagram.Should().Contain("rag-api");
        diagramPayload.Diagram.Should().Contain("rag-search");
        diagramPayload.Diagram.Should().Contain("rag-metadata");
    }
}
