using System.Net;
using System.Net.Http.Json;

using ArchiForge.Api.Models;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureTraceTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetRunTraces_ReturnsPromptAndRawResponseAfterExecute()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-TRACE-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage tracesResponse = await Client.GetAsync($"/v1/architecture/run/{runId}/traces");

        tracesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        AgentExecutionTraceResponse? payload = await tracesResponse.Content.ReadFromJsonAsync<AgentExecutionTraceResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Traces.Should().NotBeEmpty();
        payload.Traces.Should().Contain(t => !string.IsNullOrWhiteSpace(t.SystemPrompt));
        payload.Traces.Should().Contain(t => !string.IsNullOrWhiteSpace(t.UserPrompt));
        payload.Traces.Should().Contain(t => !string.IsNullOrWhiteSpace(t.RawResponse));
    }
}
