using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Core.Audit;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     End-to-end test for <c>POST /v1/governance/policy-packs/{id}/dry-run</c>: asserts the redaction
///     marker appears in the persisted audit row when the request body contains a known PII pattern
///     (PENDING_QUESTIONS Q37). Also verifies the endpoint returns <see cref="HttpStatusCode.OK" /> with
///     a populated <c>proposedThresholdsRedactedJson</c> field.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PolicyPackDryRunIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null, true) }
    };

    [Fact]
    public async Task DryRun_WithPiiInProposedThresholds_PersistsRedactedAuditRow()
    {
        await using PolicyPackDryRunIntegrationApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        Guid policyPackId = Guid.Parse("11112222-3333-4444-5555-666677778888");

        var body = new
        {
            ProposedThresholds = new Dictionary<string, string>
            {
                { "maxCriticalFindings", "0" },
                { "operatorNote", "ping me at alice@example.com about ssn 111-22-3333" }
            },
            EvaluateAgainstRunIds = new[] { "ghost-run-id-not-in-db" }
        };

        StringContent json = new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(
            $"/v1/governance/policy-packs/{policyPackId:D}/dry-run",
            json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        DryRunResponseDto? payload = await response.Content.ReadFromJsonAsync<DryRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.PageSize.Should().Be(20, "default page size is 20 per PENDING_QUESTIONS Q38");
        payload.ProposedThresholdsRedactedJson.Should().Contain("[REDACTED]");
        payload.ProposedThresholdsRedactedJson.Should().NotContain("alice@example.com");
        payload.ProposedThresholdsRedactedJson.Should().NotContain("111-22-3333");

        IReadOnlyList<AuditEvent> rows = factory.Audit.Snapshot();
        AuditEvent? dryRunRow = rows.FirstOrDefault(e => e.EventType == AuditEventTypes.GovernanceDryRunRequested);
        dryRunRow.Should().NotBeNull();
        dryRunRow!.DataJson.Should().Contain("[REDACTED]");
        dryRunRow.DataJson.Should().NotContain("alice@example.com");
        dryRunRow.DataJson.Should().NotContain("111-22-3333");

        using JsonDocument doc = JsonDocument.Parse(dryRunRow.DataJson);
        doc.RootElement.GetProperty("policyPackId").GetGuid().Should().Be(policyPackId);
        doc.RootElement.GetProperty("evaluatedRunIds").EnumerateArray()
            .Select(e => e.GetString()).Should().BeEquivalentTo("ghost-run-id-not-in-db");
        doc.RootElement.GetProperty("deltaCounts").GetProperty("evaluated").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("deltaCounts").GetProperty("runMissing").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task DryRun_WithEmptyRunIdList_Returns400()
    {
        await using PolicyPackDryRunIntegrationApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        Guid policyPackId = Guid.NewGuid();

        var body = new
        {
            ProposedThresholds = new Dictionary<string, string>(), EvaluateAgainstRunIds = Array.Empty<string>()
        };

        StringContent json = new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(
            $"/v1/governance/policy-packs/{policyPackId:D}/dry-run",
            json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DryRun_WithoutScopeHeaders_DoesNotLeakAuthInternals()
    {
        await using PolicyPackDryRunIntegrationApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        Guid policyPackId = Guid.NewGuid();

        var body = new
        {
            ProposedThresholds = new Dictionary<string, string> { { "maxCriticalFindings", "0" } },
            EvaluateAgainstRunIds = new[] { "any" }
        };

        StringContent json = new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(
            $"/v1/governance/policy-packs/{policyPackId:D}/dry-run",
            json);

        // Response is either OK (DevelopmentBypass succeeds) or 4xx (auth/scoping fails). Either way the
        // dry-run path must not leak a 5xx server error back to the caller.
        ((int)response.StatusCode).Should().BeLessThan(500);
    }

    private sealed class DryRunResponseDto
    {
        public Guid PolicyPackId
        {
            get;
            set;
        }

        public int Page
        {
            get;
            set;
        }

        public int PageSize
        {
            get;
            set;
        }

        public int TotalRequestedRuns
        {
            get;
            set;
        }

        public int ReturnedRuns
        {
            get;
            set;
        }

        public string ProposedThresholdsRedactedJson
        {
            get;
            set;
        } = string.Empty;
    }
}
