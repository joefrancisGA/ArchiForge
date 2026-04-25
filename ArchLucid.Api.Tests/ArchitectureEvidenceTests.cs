using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Evidence.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureEvidenceTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetRunEvidence_ReturnsEvidencePackageAfterExecute()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-EVIDENCE-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage evidenceResponse = await Client.GetAsync($"/v1/architecture/run/{runId}/evidence");

        evidenceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        AgentEvidencePackageResponse? payload =
            await evidenceResponse.Content.ReadFromJsonAsync<AgentEvidencePackageResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Evidence.RunId.Should().Be(runId);
        payload.Evidence.SystemName.Should().Be("EnterpriseRag");
        payload.Evidence.Request.Description.Should().Contain("secure Azure RAG system");
        payload.Evidence.Policies.Should().NotBeEmpty();
        payload.Evidence.ServiceCatalog.Should().NotBeEmpty();
    }
}
