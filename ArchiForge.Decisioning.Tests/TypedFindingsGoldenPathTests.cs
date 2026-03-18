using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Manifest.Builders;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Repositories;
using ArchiForge.Decisioning.Rules;
using ArchiForge.Decisioning.Services;
using ArchiForge.KnowledgeGraph.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Decisioning.Tests;

public sealed class TypedFindingsGoldenPathTests
{
    [Fact]
    public async Task EndToEnd_SecurityGapProducesDecision_AndCostPayloadInManifest()
    {
        var runId = Guid.NewGuid();
        var ctxId = Guid.NewGuid();
        var graph = new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            RunId = runId,
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
                    Properties = new()
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

        IFindingEngine[] engines =
        [
            new RequirementFindingEngine(),
            new TopologySanityFindingEngine(),
            new SecurityBaselineFindingEngine(),
            new CostConstraintFindingEngine()
        ];

        var orchestrator = new FindingsOrchestrator(
            engines,
            new InMemoryFindingsSnapshotRepository(),
            new FindingPayloadValidator());

        var snapshot = await orchestrator.GenerateFindingsSnapshotAsync(runId, ctxId, graph, CancellationToken.None);

        snapshot.Findings.Should().Contain(f => f.FindingType == "CostConstraintFinding");
        snapshot.Findings.Should().Contain(f => f.FindingType == "SecurityControlFinding");

        var decisionEngine = new RuleBasedDecisionEngine(
            new InMemoryDecisionRuleProvider(),
            new DefaultGoldenManifestBuilder(),
            new GoldenManifestValidator(),
            new InMemoryGoldenManifestRepository(),
            new InMemoryDecisionTraceRepository());

        var (manifest, _) = await decisionEngine.DecideAsync(runId, ctxId, graph, snapshot, CancellationToken.None);

        manifest.Cost.MaxMonthlyCost.Should().Be(5000m);
        manifest.Security.Controls.Should().Contain(c => c.ControlId == "AC-2" && c.Status == "missing");
        manifest.Decisions.Should().Contain(d =>
            d.Category == "Security" && d.SelectedOption == "RequiredRemediation" && d.Title.Contains("MFA"));
        manifest.Assumptions.Should().Contain(a => a.Contains("Preferred:", StringComparison.OrdinalIgnoreCase) && a.Contains("Cost", StringComparison.OrdinalIgnoreCase));
    }
}
