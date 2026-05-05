using ArchLucid.Application.Architecture;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

using FluentAssertions;

namespace ArchLucid.Application.Tests;

public sealed class CommittedManifestTraceabilityRulesTests
{
    [SkippableFact]
    public void GetLinkageGaps_WhenManifestAndTracesAlign_ReturnsEmpty()
    {
        RunEventTracePayload ev = new() { TraceId = "aa", RunId = "run1", EventType = "Test", EventDescription = "d" };

        GoldenManifest manifest = new()
        {
            RunId = "run1", SystemName = "Sys", Metadata = new ManifestMetadata { ManifestVersion = "v1", DecisionTraceIds = ["aa"] }
        };

        IReadOnlyList<string> gaps = CommittedManifestTraceabilityRules.GetLinkageGaps(
            manifest,
            [RunEventTrace.From(ev)]);

        gaps.Should().BeEmpty();
    }

    [SkippableFact]
    public void GetLinkageGaps_WhenTraceMissingFromManifest_ReturnsGap()
    {
        RunEventTracePayload ev = new() { TraceId = "missing", RunId = "run1", EventType = "Test", EventDescription = "d" };

        GoldenManifest manifest = new()
        {
            RunId = "run1", SystemName = "Sys", Metadata = new ManifestMetadata { ManifestVersion = "v1", DecisionTraceIds = [] }
        };

        IReadOnlyList<string> gaps = CommittedManifestTraceabilityRules.GetLinkageGaps(
            manifest,
            [RunEventTrace.From(ev)]);

        gaps.Should().ContainSingle()
            .Which.Should().Contain("missing");
    }

    [SkippableFact]
    public void GetLinkageGaps_FromDetail_WhenNotCommitted_ReturnsEmpty()
    {
        ArchitectureRunDetail detail = new() { Run = new ArchitectureRun { RunId = "r", RequestId = "q", Status = ArchitectureRunStatus.ReadyForCommit } };

        CommittedManifestTraceabilityRules.GetLinkageGaps(detail).Should().BeEmpty();
    }
}
