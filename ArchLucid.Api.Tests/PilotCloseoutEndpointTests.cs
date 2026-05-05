using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>HTTP coverage for <c>POST /v1/pilots/closeout</c>.</summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class PilotCloseoutEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task PostCloseout_valid_body_returns_201_with_closeout_id()
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/pilots/closeout");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = JsonContent(
            new
            {
                baselineHours = 12.5m,
                speedScore = 4,
                manifestPackageScore = 5,
                traceabilityScore = 4,
                notes = "solid pilot",
                runId = (string?)null
            });

        HttpResponseMessage response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Dictionary<string, JsonElement>? body =
            await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions);

        body.Should().NotBeNull();
        body!["closeoutId"].GetGuid().Should().NotBeEmpty();
    }

    [SkippableFact]
    public async Task PostCloseout_invalid_run_id_returns_400()
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/pilots/closeout");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = JsonContent(
            new
            {
                speedScore = 3,
                manifestPackageScore = 3,
                traceabilityScore = 3,
                runId = "not-a-guid"
            });

        HttpResponseMessage response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
