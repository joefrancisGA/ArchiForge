using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureEvidenceTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetRunEvidence_ReturnsEvidencePackageAfterExecute()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-EVIDENCE-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var evidenceResponse = await Client.GetAsync($"/v1/architecture/run/{runId}/evidence");

        evidenceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await evidenceResponse.Content.ReadFromJsonAsync<AgentEvidencePackageResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Evidence.RunId.Should().Be(runId);
        payload.Evidence.SystemName.Should().Be("EnterpriseRag");
        payload.Evidence.Request.Description.Should().Contain("secure Azure RAG system");
        payload.Evidence.Policies.Should().NotBeEmpty();
        payload.Evidence.ServiceCatalog.Should().NotBeEmpty();
    }
}
