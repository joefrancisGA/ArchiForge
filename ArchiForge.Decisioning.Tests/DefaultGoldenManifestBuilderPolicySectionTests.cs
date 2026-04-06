using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Decisioning.Findings;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Manifest.Builders;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Rules;
using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// Tests for Default Golden Manifest Builder Policy Section.
/// </summary>

[Trait("Category", "Unit")]
public sealed class DefaultGoldenManifestBuilderPolicySectionTests
{
    [Fact]
    public async Task Build_PolicyApplicabilityInfo_AddsSatisfiedControl()
    {
        GoldenManifest manifest = await BuildWithFindingsAsync(
        [
            new Finding
            {
                FindingType = FindingTypes.PolicyApplicabilityFinding,
                Category = "Policy",
                EngineType = "test",
                Severity = FindingSeverity.Info,
                Title = "Applies",
                Rationale = "ok",
                Payload = new PolicyApplicabilityFindingPayload
                {
                    PolicyName = "SOC2",
                    PolicyReference = "SOC2-CC",
                    ApplicableTopologyResourceCount = 2
                }
            }
        ]);

        manifest.Policy.SatisfiedControls.Should().ContainSingle(c => c.ControlName == "SOC2");
        manifest.Policy.Violations.Should().BeEmpty();
    }

    [Fact]
    public async Task Build_PolicyApplicabilityWarning_AddsViolation()
    {
        GoldenManifest manifest = await BuildWithFindingsAsync(
        [
            new Finding
            {
                FindingType = FindingTypes.PolicyApplicabilityFinding,
                Category = "Policy",
                EngineType = "test",
                Severity = FindingSeverity.Warning,
                Title = "Gap",
                Rationale = "not linked",
                Payload = new PolicyApplicabilityFindingPayload { PolicyName = "HIPAA" }
            }
        ]);

        manifest.Policy.Violations.Should().ContainSingle(v => v.ControlName == "HIPAA");
    }

    [Fact]
    public async Task Build_PolicyCoverageUncoveredResources_AddsViolationsPerResource()
    {
        GoldenManifest manifest = await BuildWithFindingsAsync(
        [
            new Finding
            {
                FindingType = FindingTypes.PolicyCoverageFinding,
                Category = "Policy",
                EngineType = "test",
                Severity = FindingSeverity.Warning,
                Title = "Coverage",
                Rationale = "gap",
                Payload = new PolicyCoverageFindingPayload
                {
                    UncoveredResources = ["r1", "r2"]
                }
            }
        ]);

        manifest.Policy.Violations.Should().HaveCount(2);
        manifest.Policy.Violations.Should().Contain(v => v.ControlName.Contains("r1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Build_PolicyCoverageEmptyResources_AddsGenericViolation()
    {
        GoldenManifest manifest = await BuildWithFindingsAsync(
        [
            new Finding
            {
                FindingType = FindingTypes.PolicyCoverageFinding,
                Category = "Policy",
                EngineType = "test",
                Severity = FindingSeverity.Warning,
                Title = "Coverage",
                Rationale = "no resources listed",
                Payload = new PolicyCoverageFindingPayload { UncoveredResources = [] }
            }
        ]);

        manifest.Policy.Violations.Should().ContainSingle(v => v.ControlId == "policy-coverage");
    }

    private static async Task<GoldenManifest> BuildWithFindingsAsync(List<Finding> findings)
    {
        Guid runId = Guid.NewGuid();
        Guid ctxId = Guid.NewGuid();
        GraphSnapshot graph = new()
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = ctxId,
            RunId = runId,
            Nodes = [],
            Edges = []
        };

        FindingsSnapshot snapshot = new()
        {
            FindingsSnapshotId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = ctxId,
            GraphSnapshotId = graph.GraphSnapshotId,
            Findings = findings
        };

        DecisionTrace trace = DecisionTrace.FromRuleAudit(new RuleAuditTracePayload
        {
            DecisionTraceId = Guid.NewGuid(),
            RunId = runId
        });
        DecisionRuleSet ruleSet = await new InMemoryDecisionRuleProvider().GetRuleSetAsync(CancellationToken.None);

        return new DefaultGoldenManifestBuilder().Build(runId, ctxId, graph, snapshot, trace, ruleSet);
    }
}
