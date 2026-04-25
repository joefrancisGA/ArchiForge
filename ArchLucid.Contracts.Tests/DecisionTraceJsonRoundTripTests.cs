using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;

using FluentAssertions;

namespace ArchLucid.Contracts.Tests;

/// <summary>
///     <see cref="DecisionTrace" /> uses <see cref="DecisionTraceJsonConverter" />; coordinator and authority payloads
///     must round-trip.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class DecisionTraceJsonRoundTripTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(ContractJson.Default);

    static DecisionTraceJsonRoundTripTests()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    [Fact]
    public void RunEventTrace_round_trips_json_with_legacy_null_sibling()
    {
        DateTime created = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);
        RunEventTrace original = RunEventTrace.From(
            new RunEventTracePayload
            {
                TraceId = "trace-rt-1",
                RunId = "run-rt-1",
                EventType = "Merge.Completed",
                EventDescription = "Merged agent results.",
                CreatedUtc = created
            });

        string json = JsonSerializer.Serialize<DecisionTrace>(original, JsonOptions);
        JsonDocument.Parse(json).RootElement.GetProperty("kind").GetInt32().Should()
            .Be((int)DecisionTraceKind.RunEvent);

        DecisionTrace? back = JsonSerializer.Deserialize<DecisionTrace>(json, JsonOptions);
        back.Should().NotBeNull().And.BeOfType<RunEventTrace>();
        RunEventTracePayload ev = back.RequireRunEvent();
        ev.TraceId.Should().Be("trace-rt-1");
        ev.RunId.Should().Be("run-rt-1");
        ev.EventType.Should().Be("Merge.Completed");
        ev.CreatedUtc.Should().Be(created);
    }

    [Fact]
    public void RuleAuditTrace_round_trips_json()
    {
        Guid traceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid runId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        RuleAuditTrace original = RuleAuditTrace.From(
            new RuleAuditTracePayload
            {
                TenantId = Guid.Parse("22222222-3333-4444-5555-666666666666"),
                WorkspaceId = Guid.Parse("33333333-4444-5555-6666-777777777777"),
                ProjectId = Guid.Parse("44444444-5555-6666-7777-888888888888"),
                DecisionTraceId = traceId,
                RunId = runId,
                CreatedUtc = new DateTime(2026, 4, 6, 13, 0, 0, DateTimeKind.Utc),
                RuleSetId = "rs1",
                RuleSetVersion = "1.0.0",
                RuleSetHash = "abc",
                AppliedRuleIds = ["r1"],
                AcceptedFindingIds = ["f1"],
                RejectedFindingIds = [],
                Notes = []
            });

        string json = JsonSerializer.Serialize<DecisionTrace>(original, JsonOptions);
        JsonDocument.Parse(json).RootElement.GetProperty("kind").GetInt32().Should()
            .Be((int)DecisionTraceKind.RuleAudit);

        DecisionTrace? back = JsonSerializer.Deserialize<DecisionTrace>(json, JsonOptions);
        back.Should().NotBeNull().And.BeOfType<RuleAuditTrace>();
        RuleAuditTracePayload audit = back.RequireRuleAudit();
        audit.DecisionTraceId.Should().Be(traceId);
        audit.RunId.Should().Be(runId);
        audit.AppliedRuleIds.Should().Equal("r1");
    }

    [Fact]
    public void Legacy_json_with_explicit_null_ruleAudit_deserializes_as_RunEventTrace()
    {
        const string json = """
                            {
                              "kind": 0,
                              "runEvent": {
                                "traceId": "t-legacy",
                                "runId": "run-legacy",
                                "eventType": "Test",
                                "eventDescription": "Legacy shape",
                                "createdUtc": "2026-04-06T14:00:00Z"
                              },
                              "ruleAudit": null
                            }
                            """;

        DecisionTrace? back = JsonSerializer.Deserialize<DecisionTrace>(json, JsonOptions);
        back.Should().NotBeNull().And.BeOfType<RunEventTrace>();
        back.RequireRunEvent().TraceId.Should().Be("t-legacy");
    }
}
