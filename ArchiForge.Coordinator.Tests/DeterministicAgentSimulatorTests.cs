using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.DecisionEngine.Validation;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArchiForge.Coordinator.Tests;

/// <summary>
/// Tests for Deterministic Agent Simulator.
/// </summary>

[Trait("Suite", "Core")]
[Trait("Category", "Slow")]
public sealed class DeterministicAgentSimulatorTests
{
    [Fact]
    public async Task Simulator_ShouldProduceDeterministicStarterResultsAsync()
    {
        ArchitectureRequest request = new()
        {
            RequestId = "REQ-001",
            SystemName = "EnterpriseRag",
            Description = "Design a secure Azure RAG system for internal enterprise documents.",
            Environment = "prod",
            Constraints =
            [
                "Private endpoints required",
                "Use managed identity",
                "Encryption at rest required"
            ],
            RequiredCapabilities =
            [
                "Azure AI Search",
                "SQL",
                "Managed Identity",
                "Private Networking"
            ]
        };

        CoordinatorService coordinator = new(new FakeAuthorityRunOrchestrator(), NullLogger<CoordinatorService>.Instance);
        CoordinationResult coordination = await coordinator.CreateRunAsync(request);

        coordination.Success.Should().BeTrue();

        DeterministicAgentSimulator simulator = new();
        AgentEvidencePackage evidence = CreateMinimalEvidence(coordination.Run.RunId, request);

        IReadOnlyList<AgentResult> results = await simulator.ExecuteAsync(
            coordination.Run.RunId,
            request,
            evidence,
            coordination.Tasks);

        results.Should().HaveCount(4);
        results.Select(r => r.AgentType).Should().Contain([
            Contracts.Common.AgentType.Topology,
            Contracts.Common.AgentType.Cost,
            Contracts.Common.AgentType.Compliance,
            Contracts.Common.AgentType.Critic
        ]);
    }

    [Fact]
    public async Task Simulator_AndDecisionEngine_ShouldProduceManifestAsync()
    {
        ArchitectureRequest request = new()
        {
            RequestId = "REQ-001",
            SystemName = "EnterpriseRag",
            Description = "Design a secure Azure RAG system for internal enterprise documents.",
            Environment = "prod",
            Constraints =
            [
                "Private endpoints required",
                "Use managed identity",
                "Encryption at rest required"
            ],
            RequiredCapabilities =
            [
                "Azure AI Search",
                "SQL",
                "Managed Identity",
                "Private Networking"
            ]
        };

        CoordinatorService coordinator = new(new FakeAuthorityRunOrchestrator(), NullLogger<CoordinatorService>.Instance);
        CoordinationResult coordination = await coordinator.CreateRunAsync(request);

        DeterministicAgentSimulator simulator = new();
        AgentEvidencePackage evidence = CreateMinimalEvidence(coordination.Run.RunId, request);

        IReadOnlyList<AgentResult> results = await simulator.ExecuteAsync(
            coordination.Run.RunId,
            request,
            evidence,
            coordination.Tasks);

        SchemaValidationService validationService = new(
            NullLogger<SchemaValidationService>.Instance,
            Options.Create(new SchemaValidationOptions()));

        DecisionEngineService engine = new(validationService);

        DecisionMergeResult merge = engine.MergeResults(
            coordination.Run.RunId,
            request,
            "v1",
            results,
            evaluations: [],
            decisionNodes: []);

        merge.Success.Should().BeTrue();
        merge.Manifest.SystemName.Should().Be("EnterpriseRag");
        merge.Manifest.Services.Should().Contain(s => s.ServiceName == "rag-api");
        merge.Manifest.Services.Should().Contain(s => s.ServiceName == "rag-search");
        merge.Manifest.Datastores.Should().Contain(d => d.DatastoreName == "rag-metadata");
        merge.Manifest.Governance.RequiredControls.Should().Contain(c =>
            c.Equals("Managed Identity", StringComparison.OrdinalIgnoreCase));
        merge.Manifest.Governance.RequiredControls.Should().Contain(c =>
            c.Equals("Private Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    private static AgentEvidencePackage CreateMinimalEvidence(string runId, ArchitectureRequest request)
    {
        return new AgentEvidencePackage
        {
            RunId = runId,
            RequestId = request.RequestId,
            SystemName = request.SystemName,
            Environment = request.Environment,
            CloudProvider = request.CloudProvider.ToString(),
            Request = new RequestEvidence
            {
                Description = request.Description,
                Constraints = request.Constraints.ToList(),
                RequiredCapabilities = request.RequiredCapabilities.ToList(),
                Assumptions = request.Assumptions.ToList()
            }
        };
    }

}
