using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureDecisionEngineV2Tests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CommitRun_PersistsDecisionNodes()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DECISION-V2-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var decisionsResponse = await Client.GetAsync($"/v1/architecture/run/{runId}/decisions");
        decisionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await decisionsResponse.Content.ReadFromJsonAsync<DecisionNodeResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Decisions.Should().NotBeEmpty();
        payload.Decisions.Should().Contain(d => d.Topic == "TopologyAcceptance");
        payload.Decisions.Should().Contain(d => d.Topic == "SecurityControlPromotion");
        payload.Decisions.Should().Contain(d => d.Topic == "ComplexityDisposition");
    }
}

