using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Commit Conflict.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureCommitConflictTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CommitBeforeExecute_Returns409Conflict()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-COMMIT-CONFLICT-001")));

        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage commit = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        commit.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using JsonDocument doc = JsonDocument.Parse(await commit.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("title").GetString().Should().Be("Conflict");
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(409);
    }
}
