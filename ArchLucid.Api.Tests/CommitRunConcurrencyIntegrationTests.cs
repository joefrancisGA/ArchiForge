using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Parallel <c>POST /v1/architecture/run/{runId}/commit</c> after execute: coordinator reconciliation returns the same
///     manifest version for every parallel caller once the first commit wins.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class CommitRunConcurrencyIntegrationTests
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
    public async Task Parallel_commits_after_execute_all_succeed_with_same_manifest_version()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage createResponse = await client.PostAsync(
            "/v1/architecture/request",
            JsonContent(
                TestRequestFactory.CreateArchitectureRequest("REQ-COMMIT-PAR-" + Guid.NewGuid().ToString("N")[..8])));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        const int parallel = 8;
        Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            tasks[i] = client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        }

        HttpResponseMessage[] responses = await Task.WhenAll(tasks);

        try
        {
            string? manifestVersion = null;

            foreach (HttpResponseMessage response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                CommitRunResponseDto? payload =
                    await response.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
                payload.Should().NotBeNull();
                payload.Manifest.Metadata.ManifestVersion.Should().NotBeNullOrWhiteSpace();

                if (manifestVersion is null)
                {
                    manifestVersion = payload.Manifest.Metadata.ManifestVersion;
                }
                else
                {
                    payload.Manifest.Metadata.ManifestVersion.Should().Be(manifestVersion);
                }
            }
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }
}
