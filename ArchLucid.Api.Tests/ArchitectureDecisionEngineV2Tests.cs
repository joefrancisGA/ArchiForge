using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Decision Engine V2.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureDecisionEngineV2Tests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CommitRun_PersistsDecisionNodes()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DECISION-V2-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        HttpResponseMessage decisionsResponse = await Client.GetAsync($"/v1/architecture/run/{runId}/decisions");
        decisionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        DecisionNodeResponseDto? payload =
            await decisionsResponse.Content.ReadFromJsonAsync<DecisionNodeResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Decisions.Should().NotBeEmpty();
        payload.Decisions.Should().Contain(d => d.Topic == "TopologyAcceptance");
        payload.Decisions.Should().Contain(d => d.Topic == "SecurityControlPromotion");
        payload.Decisions.Should().Contain(d => d.Topic == "ComplexityDisposition");
    }
}
