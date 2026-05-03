using ArchLucid.Application.Runs.Telemetry;
using ArchLucid.Contracts.Agents;
using ArchLucid.Persistence.Models;

using FluentAssertions;

using ManifestDocument = ArchLucid.Decisioning.Models.ManifestDocument;

namespace ArchLucid.Application.Tests.Runs.Telemetry;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class CommitRunTelemetryMetricsTests
{
    [Fact]
    public void FromCommitContext_maps_wall_clock_segments_and_estimated_hours_from_findings_and_warnings()
    {
        DateTime t0 = new(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        RunRecord runHeader = new() { CreatedUtc = t0 };

        AgentEvidencePackage evidence = new() { CreatedUtc = t0.AddMinutes(3) };

        AgentResult slower = new()
        {
            CreatedUtc = t0.AddMinutes(10),
            Findings =
            [
                new ArchitectureFinding { Severity = FindingSeverity.Warning, Message = "a" },
                new ArchitectureFinding { Severity = FindingSeverity.Info, Message = "b" }
            ]
        };

        AgentResult faster = new() { CreatedUtc = t0.AddMinutes(5), Findings = [] };

        List<AgentResult> agentResults = [faster, slower];
        DateTime commitUtc = t0.AddMinutes(25);

        ManifestDocument persisted = new() { Warnings = ["w1", "w2", "w3"] };

        CommitRunTelemetryMetrics metrics = CommitRunTelemetryMetrics.FromCommitContext(
            runHeader,
            evidence,
            agentResults,
            commitUtc,
            persisted);

        metrics.RequestDurationMs.Should().Be(180_000L);
        metrics.AgentExecutionDurationMs.Should().Be(300_000L);

        // reviewStart = max(slower, evidence) = t0+10m; commit t0+25m → 15 minutes
        metrics.ManualReviewDurationMs.Should().Be(900_000L);

        // findings 2 * 0.25 + warnings 3 * 0.5 = 0.5 + 1.5 = 2.0
        metrics.EstimatedHoursSaved.Should().Be(2.0m);
    }

    [Fact]
    public void FromCommitContext_clamps_minimum_and_maximum_estimated_hours()
    {
        DateTime t0 = DateTime.UtcNow;
        RunRecord runHeader = new() { CreatedUtc = t0 };
        AgentEvidencePackage evidence = new() { CreatedUtc = t0 };
        List<AgentResult> agentResults = [];

        ManifestDocument emptyWarnings = new() { Warnings = [] };

        CommitRunTelemetryMetrics low = CommitRunTelemetryMetrics.FromCommitContext(
            runHeader,
            evidence,
            agentResults,
            t0,
            emptyWarnings);

        low.EstimatedHoursSaved.Should().Be(0.25m);

        List<AgentResult> huge = [];

        for (int i = 0; i < 500; i++)

            huge.Add(new AgentResult { Findings = [new ArchitectureFinding { Message = $"f{i}" }] });

        ManifestDocument manyWarnings = new()
        {
            Warnings = [.. Enumerable.Range(0, 200).Select(static i => $"w{i}")]
        };

        CommitRunTelemetryMetrics high = CommitRunTelemetryMetrics.FromCommitContext(
            runHeader,
            evidence,
            huge,
            t0.AddHours(1),
            manyWarnings);

        high.EstimatedHoursSaved.Should().Be(80m);
    }

    [Fact]
    public void FromCommitContext_normalizes_non_utc_DateTime_kinds()
    {
        DateTime t0 = new(2026, 5, 2, 12, 0, 0, DateTimeKind.Local);
        RunRecord runHeader = new() { CreatedUtc = t0 };
        AgentEvidencePackage evidence = new() { CreatedUtc = DateTime.SpecifyKind(t0.AddMinutes(1), DateTimeKind.Local) };

        AgentResult r1 = new() { CreatedUtc = DateTime.SpecifyKind(t0.AddMinutes(2), DateTimeKind.Unspecified) };

        AgentResult r2 = new() { CreatedUtc = DateTime.SpecifyKind(t0.AddMinutes(4), DateTimeKind.Local) };

        CommitRunTelemetryMetrics metrics = CommitRunTelemetryMetrics.FromCommitContext(
            runHeader,
            evidence,
            [r1, r2],
            DateTime.SpecifyKind(t0.AddMinutes(10), DateTimeKind.Local),
            new ManifestDocument { Warnings = [] });

        metrics.RequestDurationMs.Should().BeGreaterThan(0L);
        metrics.AgentExecutionDurationMs.Should().BeGreaterThan(0L);
        metrics.ManualReviewDurationMs.Should().BeGreaterThan(0L);
    }
}
