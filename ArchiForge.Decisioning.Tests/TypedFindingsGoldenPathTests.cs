using ArchiForge.Decisioning.Analysis;
using ArchiForge.Decisioning.Compliance.Evaluators;
using ArchiForge.Decisioning.Compliance.Loaders;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Manifest.Builders;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Rules;
using ArchiForge.Decisioning.Services;
using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.Decisioning.Tests;

public sealed class TypedFindingsGoldenPathTests
{
    [Fact]
    public async Task EndToEnd_SecurityCostAndComplianceFlowThroughFindingsAndManifest()
    {
        Guid runId = Guid.NewGuid();
        Guid ctxId = Guid.NewGuid();
        GraphSnapshot graph = new()
        {
            GraphSnapshotId = Guid.NewGuid(),
            RunId = runId,
            Edges =
            {
                new GraphEdge
                {
                    EdgeId = "rel1",
                    FromNodeId = "r1",
                    ToNodeId = "t1",
                    EdgeType = "RELATES_TO",
                    Label = "relates to"
                },
                new GraphEdge { EdgeId = "p1", FromNodeId = "s1", ToNodeId = "t1", EdgeType = "PROTECTS", Label = "protects" },
                new GraphEdge { EdgeId = "p2", FromNodeId = "s1", ToNodeId = "t2", EdgeType = "PROTECTS", Label = "protects" },
                new GraphEdge { EdgeId = "p3", FromNodeId = "s1", ToNodeId = "t3", EdgeType = "PROTECTS", Label = "protects" },
                new GraphEdge { EdgeId = "p4", FromNodeId = "s1", ToNodeId = "t4", EdgeType = "PROTECTS", Label = "protects" }
            },
            Nodes =
            {
                new GraphNode
                {
                    NodeId = "r1",
                    NodeType = "Requirement",
                    Label = "Auth",
                    Properties = new Dictionary<string, string> { ["text"] = "Authenticate users" }
                },
                new GraphNode
                {
                    NodeId = "t1",
                    NodeType = "TopologyResource",
                    Label = "vpc",
                    Category = "network",
                    Properties = []
                },
                new GraphNode
                {
                    NodeId = "t2",
                    NodeType = "TopologyResource",
                    Label = "vm",
                    Category = "compute",
                    Properties = []
                },
                new GraphNode
                {
                    NodeId = "t3",
                    NodeType = "TopologyResource",
                    Label = "blob",
                    Category = "storage",
                    Properties = []
                },
                new GraphNode
                {
                    NodeId = "t4",
                    NodeType = "TopologyResource",
                    Label = "db",
                    Category = "data",
                    Properties = []
                },
                new GraphNode
                {
                    NodeId = "s1",
                    NodeType = "SecurityBaseline",
                    Label = "MFA",
                    Properties = new Dictionary<string, string>
                    {
                        ["controlId"] = "AC-2",
                        ["status"] = "missing"
                    }
                },
                new GraphNode
                {
                    NodeId = "c1",
                    NodeType = "CostConstraint",
                    Label = "Prod",
                    Properties = new Dictionary<string, string>
                    {
                        ["budgetName"] = "Prod",
                        ["maxMonthlyCost"] = "5000",
                        ["costRisk"] = "high"
                    }
                }
            }
        };

        GraphCoverageAnalyzer analyzer = new();
        string complianceRulePackPath = Path.Combine(
            AppContext.BaseDirectory,
            "Compliance",
            "RulePacks",
            "default-compliance.rules.json");
        FileComplianceRulePackLoader complianceLoader = new(complianceRulePackPath);
        FileComplianceRulePackProvider complianceProvider = new(complianceLoader);
        ComplianceRulePackValidator complianceValidator = new();
        GraphComplianceEvaluator complianceEvaluator = new();

        IFindingEngine[] engines =
        [
            new RequirementFindingEngine(),
            new TopologyCoverageFindingEngine(analyzer),
            new SecurityBaselineFindingEngine(),
            new SecurityCoverageFindingEngine(analyzer),
            new ComplianceFindingEngine(complianceProvider, complianceValidator, complianceEvaluator),
            new CostConstraintFindingEngine()
        ];

        FindingsOrchestrator orchestrator = new(engines, new FindingPayloadValidator(), NullLogger<FindingsOrchestrator>.Instance);

        FindingsSnapshot snapshot = await orchestrator.GenerateFindingsSnapshotAsync(runId, ctxId, graph, CancellationToken.None);

        snapshot.Findings.Should().Contain(f =>
            f.FindingType == "RequirementFinding" && f.RelatedNodeIds.Contains("t1"));

        snapshot.Findings.Should().Contain(f => f.FindingType == "CostConstraintFinding");
        snapshot.Findings.Should().Contain(f =>
            f.FindingType == "SecurityControlFinding" && f.RelatedNodeIds.Contains("t1"));

        RuleBasedDecisionEngine decisionEngine = new(
            new InMemoryDecisionRuleProvider(),
            new DefaultGoldenManifestBuilder(),
            new GoldenManifestValidator(),
            new ManifestHashService());

        (GoldenManifest manifest, _) = await decisionEngine.DecideAsync(runId, ctxId, graph, snapshot, CancellationToken.None);

        manifest.Cost.MaxMonthlyCost.Should().Be(5000m);
        manifest.Security.Controls.Should().Contain(c => c.ControlId == "AC-2" && c.Status == "missing");
        manifest.Compliance.Controls.Should().Contain(c => c.ControlId == "STO-001" && c.Status == "Gap");
        manifest.Compliance.Controls.Should().Contain(c => c.ControlId == "DAT-001" && c.Status == "Gap");
        manifest.Compliance.Gaps.Should().Contain(g => g.Contains("blob", StringComparison.OrdinalIgnoreCase));
        manifest.Compliance.Gaps.Should().Contain(g => g.Contains("db", StringComparison.OrdinalIgnoreCase));
        manifest.UnresolvedIssues.Items.Should().Contain(i => i.IssueType == "ComplianceGap");
        manifest.Decisions.Should().Contain(d =>
            d.Category == "Security" && d.SelectedOption == "RequiredRemediation" && d.Title.Contains("MFA"));
        manifest.Assumptions.Should().Contain(a => a.Contains("Preferred:", StringComparison.OrdinalIgnoreCase) && a.Contains("Cost", StringComparison.OrdinalIgnoreCase));
    }
}
