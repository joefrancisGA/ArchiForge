using System.Net;
using System.Text.Json;
using ArchiForge.Contracts.Requests;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Cli.Tests;

/// <summary>
/// Unit tests for ArchiForgeApiClient using mocked HTTP (no real API).
/// </summary>
public sealed class ArchiForgeApiClientHttpTests
{
    private static ArchiForgeApiClient CreateClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new ArchiForgeApiClient(http);
    }

    private static ArchitectureRequest CreateValidRequest() => new()
    {
        RequestId = Guid.NewGuid().ToString("N"),
        Description = "A test architecture request with enough length",
        SystemName = "TestSystem",
        Environment = "prod"
    };

    [Fact]
    public async Task CreateRunAsync_On201_ReturnsSuccessAndRunId()
    {
        var runId = "run-abc-123";
        var json = JsonSerializer.Serialize(new
        {
            run = new { runId, requestId = "req-1", status = 0, createdUtc = DateTime.UtcNow, currentManifestVersion = (string?)null },
            tasks = Array.Empty<object>()
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var response = new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(json) };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var client = CreateClient(response);
        var result = await client.CreateRunAsync(CreateValidRequest());

        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Run.RunId.Should().Be(runId);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task CreateRunAsync_On400_ReturnsFailureWithParsedError()
    {
        var json = JsonSerializer.Serialize(new { detail = "Validation failed" });
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(json) };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var client = CreateClient(response);
        var result = await client.CreateRunAsync(CreateValidRequest());

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Validation failed");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetRunAsync_On200_ReturnsGetRunResult()
    {
        var runId = "run-x";
        var json = JsonSerializer.Serialize(new
        {
            run = new { runId, requestId = "req-1", status = 0, createdUtc = DateTime.UtcNow, currentManifestVersion = (string?)null },
            tasks = Array.Empty<object>(),
            results = Array.Empty<object>()
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var client = CreateClient(response);
        var result = await client.GetRunAsync(runId);

        result.Should().NotBeNull();
        result!.Run.RunId.Should().Be(runId);
        result.Tasks.Should().BeEmpty();
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRunAsync_On404_ReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        var client = CreateClient(response);
        var result = await client.GetRunAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CommitRunAsync_On200_ReturnsSuccessAndManifestVersion()
    {
        var version = "v2";
        var json = JsonSerializer.Serialize(new
        {
            manifest = new
            {
                runId = "run-1",
                systemName = "Test",
                metadata = new { manifestVersion = version }
            },
            warnings = Array.Empty<string>()
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var client = CreateClient(response);
        var result = await client.CommitRunAsync("run-1");

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Manifest.Metadata.ManifestVersion.Should().Be(version);
    }

    [Fact]
    public async Task CheckHealthAsync_On200_ReturnsTrue()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        var client = CreateClient(response);
        var result = await client.CheckHealthAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_On503_ReturnsFalse()
    {
        var response = new HttpResponseMessage((HttpStatusCode)503);

        var client = CreateClient(response);
        var result = await client.CheckHealthAsync();

        result.Should().BeFalse();
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public MockHttpMessageHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
