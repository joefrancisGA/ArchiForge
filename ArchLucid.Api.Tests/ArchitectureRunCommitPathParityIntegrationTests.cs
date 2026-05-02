using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

using JetBrains.Annotations;

namespace ArchLucid.Api.Tests;

/// <summary>
///     ADR 0030 PR A2 cohort evidence (authority path only after PR A3): simulator create â†’ execute â†’ commit
///     yields committed demo shape and stable <c>GET â€¦/pilot-run-deltas</c> fields. Volatile clock fields,
///     wall-clock seconds-to-commit, <c>topFindingId</c>, and <c>topFindingEvidenceChain</c> are excluded â€” see
///     <c>docs/evidence/phase3/pr-a2-cohort-parity.md</c>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureRunCommitPathParityIntegrationTests
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
    public async Task Authority_commit_path_cohort_produces_committed_demo_shape()
    {
        CohortArtifacts cohort = await CaptureCohortAsync();
        cohort.TraceabilityZipEntryNames.Should().NotBeEmpty();
        cohort.FirstValueReportMarkdown.Should().NotBeNullOrWhiteSpace();
        cohort.FirstValueReportMarkdown.Should().Contain("Sponsor send readiness (buyer-safe gate)");
        cohort.CommitManifestDecisionTraceIdCount.Should().BePositive();
    }

    private static async Task<CohortArtifacts> CaptureCohortAsync()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        const string requestId = "REQ-PR-A2-COHORT-DEMO-001";
        HttpResponseMessage createResponse = await client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest(requestId)));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();
        string runId = created.Run.RunId;

        HttpResponseMessage executeResponse = await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.OK, "commit should succeed on authority path");

        CommitRunResponseDto? commitPayload =
            await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        commitPayload.Should().NotBeNull();
        int manifestTraceIdCount = commitPayload.Manifest.Metadata.DecisionTraceIds.Count;
        string manifestJson = JsonSerializer.Serialize(commitPayload.Manifest, JsonOptions);

        HttpResponseMessage reportResponse = await client.GetAsync($"/v1/pilots/runs/{runId}/first-value-report");
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        string markdown = await reportResponse.Content.ReadAsStringAsync();

        HttpResponseMessage deltasResponse = await client.GetAsync($"/v1/pilots/runs/{runId}/pilot-run-deltas");
        deltasResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        string deltasJson = await deltasResponse.Content.ReadAsStringAsync();

        HttpResponseMessage zipResponse =
            await client.GetAsync($"/v1/architecture/run/{runId}/traceability-bundle.zip");
        zipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        byte[] zipBytes = await zipResponse.Content.ReadAsByteArrayAsync();
        IReadOnlyList<string> zipNames = ReadZipEntryNames(zipBytes);

        return new CohortArtifacts(runId, manifestTraceIdCount, zipNames, markdown, manifestJson, deltasJson);
    }

    private static IReadOnlyList<string> ReadZipEntryNames(byte[] zipBytes)
    {
        using MemoryStream ms = new(zipBytes);
        using ZipArchive zip = new(ms, ZipArchiveMode.Read, false);

        return zip.Entries
            .Select(static e => e.FullName)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private sealed record CohortArtifacts(
        [UsedImplicitly] string RunId,
        int CommitManifestDecisionTraceIdCount,
        IReadOnlyList<string> TraceabilityZipEntryNames,
        string FirstValueReportMarkdown,
        string CommitManifestJson,
        string PilotRunDeltasJson);
}
