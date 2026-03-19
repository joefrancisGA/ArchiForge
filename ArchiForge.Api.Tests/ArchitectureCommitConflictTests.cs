using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureCommitConflictTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CommitBeforeExecute_Returns409Conflict()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-COMMIT-CONFLICT-001")));

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var commit = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        commit.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await commit.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("title").GetString().Should().Be("Conflict");
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(409);
    }
}
