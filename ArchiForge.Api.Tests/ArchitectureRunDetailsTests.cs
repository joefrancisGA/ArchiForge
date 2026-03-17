using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureRunDetailsTests : IntegrationTestBase
{
    public ArchitectureRunDetailsTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetRun_ReturnsTypedRunDetailsResponse()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-RUNDETAILS-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var runResponse = await Client.GetAsync($"/v1/architecture/run/{runId}");
        runResponse.EnsureSuccessStatusCode();

        var payload = await runResponse.Content.ReadFromJsonAsync<RunDetailsResponseDto>(JsonOptions);

        payload.Should().NotBeNull();
        payload!.Run.RunId.Should().Be(runId);
        payload.Tasks.Should().HaveCount(3);
        payload.Results.Should().HaveCount(3);
        payload.Manifest.Should().NotBeNull();
        payload.Manifest!.SystemName.Should().Be("EnterpriseRag");
        payload.DecisionTraces.Should().NotBeEmpty();
    }
}

