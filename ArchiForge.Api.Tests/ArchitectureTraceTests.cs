using System.Net;
using System.Net.Http.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureTraceTests : IntegrationTestBase
{
    public ArchitectureTraceTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetRunTraces_ReturnsPromptAndRawResponseAfterExecute()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-TRACE-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var tracesResponse = await Client.GetAsync($"/v1/architecture/run/{runId}/traces");

        tracesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await tracesResponse.Content.ReadFromJsonAsync<AgentExecutionTraceResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Traces.Should().NotBeEmpty();
        payload.Traces.Should().Contain(t => !string.IsNullOrWhiteSpace(t.SystemPrompt));
        payload.Traces.Should().Contain(t => !string.IsNullOrWhiteSpace(t.UserPrompt));
        payload.Traces.Should().Contain(t => !string.IsNullOrWhiteSpace(t.RawResponse));
    }
}
