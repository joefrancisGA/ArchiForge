using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArchLucid.Api.Tests.TestDtos;
using ArchLucid.Contracts.Pilots;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// PR A2 gate evidence: same simulator create → execute → commit payload under legacy vs authority commit paths
/// must yield identical traceability bundle entry names and equivalent stable fields on <c>GET …/pilot-run-deltas</c>
/// (<see cref="PilotRunDeltasResponse"/> — counts, severity histogram, demo flag). Volatile clock fields, wall-clock
/// seconds-to-commit, <c>topFindingId</c>, and <c>topFindingEvidenceChain</c> are excluded because tie-order or
/// read-path shape can pick a different top finding between paths while ROI buckets stay aligned — see
/// <c>artifacts/phase3/pr-a2-cohort-parity.md</c>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureRunCommitPathParityIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true) },
    };

    private static StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [Fact]
    public async Task RunCommit_path_legacy_true_cohort_matches_authority_default_cohort()
    {
        CohortArtifacts legacy = await CaptureCohortAsync(legacyRunCommitPath: true);
        CohortArtifacts authorityDefault = await CaptureCohortAsync(legacyRunCommitPath: false);

        legacy.TraceabilityZipEntryNames.Should().Equal(
            authorityDefault.TraceabilityZipEntryNames,
            "traceability bundle ZIP must expose the same logical file set");

        AssertPilotRunDeltasStableFieldParity(legacy.PilotRunDeltasJson, authorityDefault.PilotRunDeltasJson);
    }

    [Fact]
    public async Task RunCommit_path_legacy_true_cohort_produces_committed_demo_shape()
    {
        CohortArtifacts legacy = await CaptureCohortAsync(legacyRunCommitPath: true);
        legacy.TraceabilityZipEntryNames.Should().NotBeEmpty();
        legacy.FirstValueReportMarkdown.Should().NotBeNullOrWhiteSpace();
        legacy.CommitManifestDecisionTraceIdCount.Should().BePositive();
    }

    [Fact]
    public async Task RunCommit_path_authority_default_cohort_produces_committed_demo_shape()
    {
        CohortArtifacts authorityDefault = await CaptureCohortAsync(legacyRunCommitPath: false);
        authorityDefault.TraceabilityZipEntryNames.Should().NotBeEmpty();
        authorityDefault.FirstValueReportMarkdown.Should().NotBeNullOrWhiteSpace();
        authorityDefault.CommitManifestDecisionTraceIdCount.Should().BePositive();
    }

    private static async Task<CohortArtifacts> CaptureCohortAsync(bool legacyRunCommitPath)
    {
        await using RunCommitPathParityArchLucidApiFactory factory = new(legacyRunCommitPath);
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        const string requestId = "REQ-PR-A2-COHORT-DEMO-001";
        HttpResponseMessage createResponse = await client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest(requestId)));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        created.Should().NotBeNull();
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"commit should succeed (legacy={legacyRunCommitPath})");

        CommitRunResponseDto? commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        commitPayload.Should().NotBeNull();
        int manifestTraceIdCount = commitPayload!.Manifest.Metadata.DecisionTraceIds.Count;
        string manifestJson = JsonSerializer.Serialize(commitPayload.Manifest, JsonOptions);

        HttpResponseMessage reportResponse = await client.GetAsync($"/v1/pilots/runs/{runId}/first-value-report");
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        string markdown = await reportResponse.Content.ReadAsStringAsync();

        HttpResponseMessage deltasResponse = await client.GetAsync($"/v1/pilots/runs/{runId}/pilot-run-deltas");
        deltasResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        string deltasJson = await deltasResponse.Content.ReadAsStringAsync();

        HttpResponseMessage zipResponse = await client.GetAsync($"/v1/architecture/run/{runId}/traceability-bundle.zip");
        zipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        byte[] zipBytes = await zipResponse.Content.ReadAsByteArrayAsync();
        IReadOnlyList<string> zipNames = ReadZipEntryNames(zipBytes);

        return new CohortArtifacts(runId, manifestTraceIdCount, zipNames, markdown, manifestJson, deltasJson);
    }

    private static IReadOnlyList<string> ReadZipEntryNames(byte[] zipBytes)
    {
        using MemoryStream ms = new(zipBytes);
        using ZipArchive zip = new(ms, ZipArchiveMode.Read, leaveOpen: false);

        return zip.Entries
            .Select(static e => e.FullName)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static void AssertPilotRunDeltasStableFieldParity(string legacyJson, string authorityJson)
    {
        PilotRunDeltasResponse? legacyDelta = JsonSerializer.Deserialize<PilotRunDeltasResponse>(legacyJson, JsonOptions);
        PilotRunDeltasResponse? authorityDelta =
            JsonSerializer.Deserialize<PilotRunDeltasResponse>(authorityJson, JsonOptions);

        legacyDelta.Should().NotBeNull();
        authorityDelta.Should().NotBeNull();

        legacyDelta!.FindingsBySeverity.Should().BeEquivalentTo(
            authorityDelta!.FindingsBySeverity,
            static options => options.WithStrictOrdering());

        legacyDelta.AuditRowCount.Should().Be(authorityDelta.AuditRowCount);
        legacyDelta.AuditRowCountTruncated.Should().Be(authorityDelta.AuditRowCountTruncated);
        legacyDelta.LlmCallCount.Should().Be(authorityDelta.LlmCallCount);
        legacyDelta.IsDemoTenant.Should().Be(authorityDelta.IsDemoTenant);
        legacyDelta.TopFindingSeverity.Should().Be(authorityDelta.TopFindingSeverity);
    }

    private sealed record CohortArtifacts(
        string RunId,
        int CommitManifestDecisionTraceIdCount,
        IReadOnlyList<string> TraceabilityZipEntryNames,
        string FirstValueReportMarkdown,
        string CommitManifestJson,
        string PilotRunDeltasJson);
}
