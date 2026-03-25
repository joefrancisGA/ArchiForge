using System.Net.Http.Json;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureRunDetailsTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetRun_ReturnsTypedRunDetailsResponse()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-RUNDETAILS-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        HttpResponseMessage runResponse = await Client.GetAsync($"/v1/architecture/run/{runId}");
        runResponse.EnsureSuccessStatusCode();

        RunDetailsResponseDto? payload = await runResponse.Content.ReadFromJsonAsync<RunDetailsResponseDto>(JsonOptions);

        payload.Should().NotBeNull();
        payload.Run.RunId.Should().Be(runId);
        payload.Tasks.Should().HaveCount(3);
        payload.Results.Should().HaveCount(3);
        payload.Manifest.Should().NotBeNull();
        payload.Manifest!.SystemName.Should().Be("EnterpriseRag");
        payload.DecisionTraces.Should().NotBeEmpty();
    }
}

