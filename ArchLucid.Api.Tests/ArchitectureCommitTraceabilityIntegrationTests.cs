using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Models;
using ArchLucid.Api.Tests.TestDtos;
using ArchLucid.Application.Architecture;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Commit path through the real API host uses the production
///     <see cref="ArchLucid.Decisioning.Interfaces.IDecisionEngine" />
///     (not mocks). After ADR 0030 PR A3 (2026-04-24), the only commit path is the Authority FK chain â€” these tests
///     now assert the authority traceability invariant: the projected manifest must reference the rule-audit trace id
///     in <c>Metadata.DecisionTraceIds</c>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureCommitTraceabilityIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(null) }
    };

    private static StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [SkippableFact]
    public async Task CommitRun_manifest_decision_trace_ids_align_with_returned_traces()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage createResponse = await client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-COMMIT-TRACE-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();
        string runId = created.Run.RunId;

        HttpResponseMessage executeResponse = await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        CommitRunResponse? commitPayload =
            await commitResponse.Content.ReadFromJsonAsync<CommitRunResponse>(JsonOptions);
        commitPayload.Should().NotBeNull();

        IReadOnlyList<string> gaps = AuthorityCommitTraceabilityRules.GetLinkageGaps(
            commitPayload.Manifest,
            commitPayload.DecisionTraces);

        gaps.Should()
            .BeEmpty(
                "manifest metadata must list exactly the authority rule-audit trace ids returned with the commit body");
    }
}
