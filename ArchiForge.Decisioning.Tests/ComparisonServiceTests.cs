using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// <see cref="ComparisonService"/> per-section deltas: decisions, requirements, security, topology, cost, and summary highlights.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ComparisonServiceTests
{
    private readonly ComparisonService _sut = new();

    [Fact]
    public void Compare_Sets_BaseRunId_And_TargetRunId()
    {
        Guid baseRun = Guid.NewGuid();
        Guid targetRun = Guid.NewGuid();
        GoldenManifest baseM = EmptyManifest(baseRun);
        GoldenManifest targetM = EmptyManifest(targetRun);

        ComparisonResult result = _sut.Compare(baseM, targetM);

        result.BaseRunId.Should().Be(baseRun);
        result.TargetRunId.Should().Be(targetRun);
        result.TotalDeltaCount.Should().Be(0);
    }

    [Fact]
    public void Compare_Decisions_EmitsAddedRemovedModified()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        baseM.Decisions.Add(
            new ResolvedArchitectureDecision
            {
                DecisionId = "only-base",
                Category = "c",
                Title = "t",
                SelectedOption = "opt-a",
                Rationale = "r"
            });
        baseM.Decisions.Add(
            new ResolvedArchitectureDecision
            {
                DecisionId = "both",
                Category = "c",
                Title = "t2",
                SelectedOption = "old",
                Rationale = "r"
            });

        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());
        targetM.Decisions.Add(
            new ResolvedArchitectureDecision
            {
                DecisionId = "only-target",
                Category = "c",
                Title = "t",
                SelectedOption = "opt-b",
                Rationale = "r"
            });
        targetM.Decisions.Add(
            new ResolvedArchitectureDecision
            {
                DecisionId = "both",
                Category = "c",
                Title = "t2",
                SelectedOption = "new",
                Rationale = "r"
            });

        ComparisonResult result = _sut.Compare(baseM, targetM);

        result.DecisionChanges.Should().HaveCount(3);
        result.DecisionChanges.Should().ContainSingle(d => d.ChangeType == "Removed" && d.DecisionKey == "only-base");
        result.DecisionChanges.Should().ContainSingle(d => d.ChangeType == "Added" && d.DecisionKey == "only-target");
        result.DecisionChanges.Should().ContainSingle(d =>
            d.ChangeType == "Modified"
            && d.DecisionKey == "both"
            && d.BaseValue == "old"
            && d.TargetValue == "new");
        result.TotalDeltaCount.Should().Be(
            result.DecisionChanges.Count
                + result.RequirementChanges.Count
                + result.SecurityChanges.Count
                + result.TopologyChanges.Count
                + result.CostChanges.Count);
    }

    [Fact]
    public void Compare_Decisions_UsesCategoryTitleKey_WhenDecisionIdIsWhitespace()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        baseM.Decisions.Add(
            new ResolvedArchitectureDecision
            {
                DecisionId = "   ",
                Category = "Arch",
                Title = "UseTLS",
                SelectedOption = "yes",
                Rationale = "r"
            });

        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());
        targetM.Decisions.Add(
            new ResolvedArchitectureDecision
            {
                DecisionId = "",
                Category = "Arch",
                Title = "UseTLS",
                SelectedOption = "no",
                Rationale = "r"
            });

        ComparisonResult result = _sut.Compare(baseM, targetM);

        DecisionDelta delta = result.DecisionChanges.Should().ContainSingle().Subject;
        delta.DecisionKey.Should().Be("Arch::UseTLS");
        delta.ChangeType.Should().Be("Modified");
    }

    [Fact]
    public void Compare_Requirements_EmitsCovered_WhenNewlyAppearsInTarget()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());
        targetM.Requirements.Covered.Add(
            new RequirementCoverageItem
            {
                RequirementName = "REQ-1",
                RequirementText = "text",
                IsMandatory = true,
                CoverageStatus = "ok"
            });

        ComparisonResult result = _sut.Compare(baseM, targetM);

        RequirementDelta d = result.RequirementChanges.Should().ContainSingle().Subject;
        d.RequirementName.Should().Be("REQ-1");
        d.ChangeType.Should().Be("Covered");
    }

    [Fact]
    public void Compare_Requirements_EmitsRemoved_WhenOnlyInBase()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        baseM.Requirements.Covered.Add(
            new RequirementCoverageItem
            {
                RequirementName = "REQ-X",
                RequirementText = "t",
                CoverageStatus = "ok"
            });
        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());

        ComparisonResult result = _sut.Compare(baseM, targetM);

        RequirementDelta d = result.RequirementChanges.Should().ContainSingle().Subject;
        d.ChangeType.Should().Be("Removed");
    }

    [Fact]
    public void Compare_Requirements_EmitsChanged_WhenCoverageStatusDiffers_SameBucket()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        baseM.Requirements.Covered.Add(
            new RequirementCoverageItem
            {
                RequirementName = "REQ-Y",
                RequirementText = "t",
                IsMandatory = false,
                CoverageStatus = "partial"
            });
        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());
        targetM.Requirements.Covered.Add(
            new RequirementCoverageItem
            {
                RequirementName = "REQ-Y",
                RequirementText = "t",
                IsMandatory = false,
                CoverageStatus = "full"
            });

        ComparisonResult result = _sut.Compare(baseM, targetM);

        result.RequirementChanges.Should().ContainSingle().Which.ChangeType.Should().Be("Changed");
    }

    [Fact]
    public void Compare_Security_EmitsDelta_OnStatusChange_UsingControlIdKey()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        baseM.Security.Controls.Add(
            new SecurityPostureItem
            {
                ControlId = "AC-1",
                ControlName = "Encryption",
                Status = "Planned",
                Impact = "high"
            });
        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());
        targetM.Security.Controls.Add(
            new SecurityPostureItem
            {
                ControlId = "AC-1",
                ControlName = "Encryption",
                Status = "Implemented",
                Impact = "high"
            });

        ComparisonResult result = _sut.Compare(baseM, targetM);

        SecurityDelta d = result.SecurityChanges.Should().ContainSingle().Subject;
        d.ControlName.Should().Be("Encryption");
        d.BaseStatus.Should().Be("Planned");
        d.TargetStatus.Should().Be("Implemented");
    }

    [Fact]
    public void Compare_Security_EmitsAdded_WhenControlOnlyInTarget()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());
        targetM.Security.Controls.Add(
            new SecurityPostureItem
            {
                ControlId = "",
                ControlName = "Firewall",
                Status = "On",
                Impact = "med"
            });

        ComparisonResult result = _sut.Compare(baseM, targetM);

        SecurityDelta d = result.SecurityChanges.Should().ContainSingle().Subject;
        d.BaseStatus.Should().BeNull();
        d.TargetStatus.Should().Be("On");
    }

    [Fact]
    public void Compare_Topology_EmitsAddedAndRemoved_Resources()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        baseM.Topology.Resources.Add("res-a");
        baseM.Topology.Resources.Add("res-b");
        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());
        targetM.Topology.Resources.Add("res-b");
        targetM.Topology.Resources.Add("res-c");

        ComparisonResult result = _sut.Compare(baseM, targetM);

        result.TopologyChanges.Should().HaveCount(2);
        result.TopologyChanges.Should().ContainSingle(t => t.ChangeType == "Added" && t.Resource == "res-c");
        result.TopologyChanges.Should().ContainSingle(t => t.ChangeType == "Removed" && t.Resource == "res-a");
    }

    [Fact]
    public void Compare_Cost_EmitsDelta_WhenMaxMonthlyCostDiffers()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        baseM.Cost.MaxMonthlyCost = 100m;
        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());
        targetM.Cost.MaxMonthlyCost = 250m;

        ComparisonResult result = _sut.Compare(baseM, targetM);

        CostDelta d = result.CostChanges.Should().ContainSingle().Subject;
        d.BaseCost.Should().Be(100m);
        d.TargetCost.Should().Be(250m);
    }

    [Fact]
    public void Compare_WhenNoSectionDiffers_SummarySaysNoMaterialDifferences()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());

        ComparisonResult result = _sut.Compare(baseM, targetM);

        result.SummaryHighlights.Should().ContainSingle().Which.Should().Be("No material differences detected in compared sections.");
    }

    [Fact]
    public void Compare_BuildsSummaryHighlights_PerNonEmptySection()
    {
        GoldenManifest baseM = EmptyManifest(Guid.NewGuid());
        GoldenManifest targetM = EmptyManifest(Guid.NewGuid());
        baseM.Decisions.Add(
            new ResolvedArchitectureDecision
            {
                DecisionId = "d",
                Category = "c",
                Title = "t",
                SelectedOption = "a",
                Rationale = "r"
            });
        targetM.Decisions.Add(
            new ResolvedArchitectureDecision
            {
                DecisionId = "d",
                Category = "c",
                Title = "t",
                SelectedOption = "b",
                Rationale = "r"
            });
        targetM.Topology.Resources.Add("x");
        targetM.Cost.MaxMonthlyCost = 1m;

        ComparisonResult result = _sut.Compare(baseM, targetM);

        result.SummaryHighlights.Should().Contain(s => s.Contains("decision", StringComparison.OrdinalIgnoreCase));
        result.SummaryHighlights.Should().Contain(s => s.Contains("topology", StringComparison.OrdinalIgnoreCase));
        result.SummaryHighlights.Should().Contain(s => s.Contains("cost", StringComparison.OrdinalIgnoreCase));
    }

    private static GoldenManifest EmptyManifest(Guid runId)
    {
        return new GoldenManifest
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RunId = runId,
            ManifestId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "h",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh"
        };
    }
}
