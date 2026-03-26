using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Analysis;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class ImprovementSignalAnalyzerTests
{
    private readonly ImprovementSignalAnalyzer _sut = new();

    private static GoldenManifest EmptyManifest() => new()
    {
        ManifestId = Guid.NewGuid(),
        RunId = Guid.NewGuid(),
        RuleSetId = "default-v1"
    };

    private static FindingsSnapshot EmptySnapshot() => new()
    {
        FindingsSnapshotId = Guid.NewGuid(),
        RunId = Guid.NewGuid()
    };

    [Fact]
    public void Analyze_NullManifest_Throws()
    {
        Action act = () => _sut.Analyze(null!, EmptySnapshot());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_NullSnapshot_Throws()
    {
        Action act = () => _sut.Analyze(EmptyManifest(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_NoGaps_ReturnsEmptyList()
    {
        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(EmptyManifest(), EmptySnapshot());

        signals.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_UncoveredRequirement_ProducesRequirementSignal()
    {
        GoldenManifest manifest = EmptyManifest();
        manifest.Requirements.Uncovered.Add(new RequirementCoverageItem
        {
            RequirementName = "REQ-001",
            RequirementText = "Data must be encrypted at rest",
            IsMandatory = true,
            CoverageStatus = "Uncovered"
        });

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(manifest, EmptySnapshot());

        signals.Should().ContainSingle(s =>
            s.SignalType == "UncoveredRequirement" &&
            s.Category == "Requirement" &&
            s.Severity == "High" &&
            s.Title.Contains("REQ-001"));
    }

    [Fact]
    public void Analyze_SecurityGap_ProducesSecuritySignal()
    {
        GoldenManifest manifest = EmptyManifest();
        manifest.Security.Gaps.Add("Storage account not encrypted");

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(manifest, EmptySnapshot());

        signals.Should().ContainSingle(s =>
            s.SignalType == "SecurityGap" &&
            s.Category == "Security" &&
            s.Severity == "High");
    }

    [Fact]
    public void Analyze_ComplianceGap_ProducesComplianceSignal()
    {
        GoldenManifest manifest = EmptyManifest();
        manifest.Compliance.Gaps.Add("PCI-DSS control 6.1 not addressed");

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(manifest, EmptySnapshot());

        signals.Should().ContainSingle(s =>
            s.SignalType == "ComplianceGap" &&
            s.Category == "Compliance");
    }

    [Fact]
    public void Analyze_TopologyGap_ProducesTopologySignalWithMediumSeverity()
    {
        GoldenManifest manifest = EmptyManifest();
        manifest.Topology.Gaps.Add("Missing category: identity");

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(manifest, EmptySnapshot());

        signals.Should().ContainSingle(s =>
            s.SignalType == "TopologyGap" &&
            s.Severity == "Medium");
    }

    [Fact]
    public void Analyze_CostRisk_ProducesCostSignal()
    {
        GoldenManifest manifest = EmptyManifest();
        manifest.Cost.CostRisks.Add("Premium tier chosen without justification");

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(manifest, EmptySnapshot());

        signals.Should().ContainSingle(s => s.SignalType == "CostRisk");
    }

    [Fact]
    public void Analyze_UnresolvedIssue_ProducesRiskSignal()
    {
        GoldenManifest manifest = EmptyManifest();
        manifest.UnresolvedIssues.Items.Add(new ManifestIssue
        {
            IssueType = "DataRetention",
            Title = "Data retention policy missing",
            Description = "No retention schedule defined",
            Severity = "High"
        });

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(manifest, EmptySnapshot());

        signals.Should().ContainSingle(s =>
            s.SignalType == "UnresolvedIssue" &&
            s.Category == "Risk" &&
            s.Severity == "High");
    }

    [Fact]
    public void Analyze_SecurityRegression_ProducesSecurityRegressionSignal()
    {
        ComparisonResult comparison = new();
        comparison.SecurityChanges.Add(new SecurityDelta
        {
            ControlName = "Encryption-at-rest",
            BaseStatus = "Compliant",
            TargetStatus = "NonCompliant"
        });

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(EmptyManifest(), EmptySnapshot(), comparison);

        signals.Should().ContainSingle(s =>
            s.SignalType == "SecurityRegression" &&
            s.Category == "Security");
    }

    [Fact]
    public void Analyze_IdenticalSecurityControl_ProducesNoSignal()
    {
        ComparisonResult comparison = new();
        comparison.SecurityChanges.Add(new SecurityDelta
        {
            ControlName = "MFA",
            BaseStatus = "Compliant",
            TargetStatus = "Compliant"
        });

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(EmptyManifest(), EmptySnapshot(), comparison);

        signals.Should().NotContain(s => s.SignalType == "SecurityRegression");
    }

    [Fact]
    public void Analyze_CostIncrease_ProducesCostIncreaseSignal()
    {
        ComparisonResult comparison = new();
        comparison.CostChanges.Add(new CostDelta { BaseCost = 100m, TargetCost = 200m });

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(EmptyManifest(), EmptySnapshot(), comparison);

        signals.Should().ContainSingle(s => s.SignalType == "CostIncrease");
    }

    [Fact]
    public void Analyze_CostDecrease_ProducesNoSignal()
    {
        ComparisonResult comparison = new();
        comparison.CostChanges.Add(new CostDelta { BaseCost = 200m, TargetCost = 100m });

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(EmptyManifest(), EmptySnapshot(), comparison);

        signals.Should().NotContain(s => s.SignalType == "CostIncrease");
    }

    [Fact]
    public void Analyze_PolicyViolations_ProducesPolicyViolationSignals()
    {
        GoldenManifest manifest = EmptyManifest();
        manifest.Policy.Violations.Add(new PolicyControlItem
        {
            ControlId = "c1",
            ControlName = "Encryption",
            PolicyPack = "Internal",
            Description = "At rest encryption required"
        });

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(manifest, EmptySnapshot());

        signals.Should().ContainSingle(s =>
            s.SignalType == ImprovementSignalTypes.PolicyViolation &&
            s.Category == ImprovementSignalCategories.Compliance &&
            s.Severity == ImprovementSignalSeverities.High &&
            s.Title.Contains("Encryption", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_DecisionRemoved_ProducesDecisionRemovedSignal()
    {
        ComparisonResult comparison = new();
        comparison.DecisionChanges.Add(new DecisionDelta
        {
            DecisionKey = "messaging-protocol",
            ChangeType = "Removed",
            BaseValue = "Azure Service Bus"
        });

        IReadOnlyList<ImprovementSignal> signals = _sut.Analyze(EmptyManifest(), EmptySnapshot(), comparison);

        signals.Should().ContainSingle(s =>
            s.SignalType == "DecisionRemoved" &&
            s.Title.Contains("messaging-protocol"));
    }
}
