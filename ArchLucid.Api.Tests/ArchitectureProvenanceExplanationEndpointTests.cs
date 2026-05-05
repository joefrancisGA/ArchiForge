using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Models;
using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>Coordinator provenance node explanation stub: scoped run lookup plus <c>501</c> pending payload.</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureProvenanceExplanationEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null) }
    };

    private static StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);

        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [SkippableFact]
    public async Task GetProvenanceNodeExplanation_singular_run_route_returns_501_when_run_exists_in_scope()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage createResponse =
            await client.PostAsync("/v1/architecture/request",
                JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-PROV-EXPLAIN-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);

        created.Should().NotBeNull();

        string runId = created!.Run.RunId;

        HttpResponseMessage response =
            await client.GetAsync($"/v1/architecture/run/{runId}/provenance/test-node/explanation");

        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);

        ProvenanceNodeExplanationPendingResponse? body =
            await response.Content.ReadFromJsonAsync<ProvenanceNodeExplanationPendingResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Message.Should().Be("Explanation feature pending");
    }

    [SkippableFact]
    public async Task GetProvenanceNodeExplanation_plural_runs_alias_matches_singular_behavior()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage createResponse =
            await client.PostAsync("/v1/architecture/request",
                JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-PROV-EXPLAIN-002")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);

        string runId = created!.Run.RunId;

        HttpResponseMessage plural =
            await client.GetAsync(
                $"/v1/architecture/runs/{Uri.EscapeDataString(runId)}/provenance/node-b/explanation");

        plural.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    [SkippableFact]
    public async Task GetProvenanceNodeExplanation_returns_404_when_run_not_in_scope()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        Guid missingRun = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        HttpResponseMessage response =
            await client.GetAsync($"/v1/architecture/run/{missingRun:D}/provenance/a-node/explanation");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
