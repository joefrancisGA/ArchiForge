using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using ArchLucid.Contracts.Requests;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>
///     Unit tests for ArchLucidApiClient using mocked HTTP (no real API).
/// </summary>
[Trait("Category", "Unit")]
public sealed class ArchLucidApiClientHttpTests
{
    private static readonly JsonSerializerOptions SJsonCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static ArchLucidApiClient CreateClient(HttpResponseMessage response)
    {
        MockHttpMessageHandler handler = new(response);
        HttpClient http = new(handler) { BaseAddress = new Uri("http://localhost") };
        return new ArchLucidApiClient(http);
    }

    private static ArchitectureRequest CreateValidRequest()
    {
        return new ArchitectureRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            Description = "A test architecture request with enough length",
            SystemName = "TestSystem",
            Environment = "prod"
        };
    }

    [Fact]
    public async Task CreateRunAsync_On201_ReturnsSuccessAndRunId()
    {
        string runId = "run-abc-123";
        string json = JsonSerializer.Serialize(
            new
            {
                run = new
                {
                    runId,
                    requestId = "req-1",
                    status = 0,
                    createdUtc = DateTime.UtcNow,
                    currentManifestVersion = (string?)null
                },
                tasks = Array.Empty<object>()
            }, SJsonCamelCase);
        HttpResponseMessage response = new(HttpStatusCode.Created) { Content = new StringContent(json) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        ArchLucidApiClient client = CreateClient(response);
        ArchLucidApiClient.CreateRunResult result = await client.CreateRunAsync(CreateValidRequest());

        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Run.RunId.Should().Be(runId);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task CreateRunAsync_On400_ReturnsFailureWithParsedError()
    {
        string json = JsonSerializer.Serialize(new { detail = "Validation failed" });
        HttpResponseMessage response = new(HttpStatusCode.BadRequest) { Content = new StringContent(json) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        ArchLucidApiClient client = CreateClient(response);
        ArchLucidApiClient.CreateRunResult result = await client.CreateRunAsync(CreateValidRequest());

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Validation failed");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetRunAsync_On200_ReturnsGetRunResult()
    {
        string runId = "run-x";
        string json = JsonSerializer.Serialize(
            new
            {
                run = new
                {
                    runId,
                    requestId = "req-1",
                    status = 0,
                    createdUtc = DateTime.UtcNow,
                    currentManifestVersion = (string?)null
                },
                tasks = Array.Empty<object>(),
                results = Array.Empty<object>()
            }, SJsonCamelCase);
        HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new StringContent(json) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        ArchLucidApiClient client = CreateClient(response);
        ArchLucidApiClient.GetRunResult? result = await client.GetRunAsync(runId);

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be(runId);
        result.Tasks.Should().BeEmpty();
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRunAsync_On404_ReturnsNull()
    {
        HttpResponseMessage response = new(HttpStatusCode.NotFound);

        ArchLucidApiClient client = CreateClient(response);
        ArchLucidApiClient.GetRunResult? result = await client.GetRunAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CommitRunAsync_On200_ReturnsSuccessAndManifestVersion()
    {
        string version = "v2";
        string json = JsonSerializer.Serialize(
            new
            {
                manifest = new
                {
                    runId = "run-1", systemName = "Test", metadata = new { manifestVersion = version }
                },
                warnings = Array.Empty<string>()
            }, SJsonCamelCase);
        HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new StringContent(json) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        ArchLucidApiClient client = CreateClient(response);
        ArchLucidApiClient.CommitRunResult? result = await client.CommitRunAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Manifest.Metadata.ManifestVersion.Should().Be(version);
    }

    [Fact]
    public async Task CommitRunAsync_On409_ReturnsFailureWithHttpStatusCode()
    {
        string json = JsonSerializer.Serialize(new { detail = "Conflict with current state." });
        HttpResponseMessage response = new(HttpStatusCode.Conflict) { Content = new StringContent(json) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        ArchLucidApiClient client = CreateClient(response);
        ArchLucidApiClient.CommitRunResult? result = await client.CommitRunAsync("run-1");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.HttpStatusCode.Should().Be(409);
        result.Error!.ToLowerInvariant().Should().Contain("conflict");
    }

    [Fact]
    public async Task CheckHealthAsync_On200_ReturnsTrue()
    {
        HttpResponseMessage response = new(HttpStatusCode.OK);

        ArchLucidApiClient client = CreateClient(response);
        bool result = await client.CheckHealthAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_On503_ReturnsFalse()
    {
        HttpResponseMessage response = new((HttpStatusCode)503);

        ArchLucidApiClient client = CreateClient(response);
        bool result = await client.CheckHealthAsync();

        result.Should().BeFalse();
    }

    private sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }
}
