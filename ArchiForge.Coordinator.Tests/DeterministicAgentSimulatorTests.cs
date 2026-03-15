using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.DecisionEngine.Services;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Coordinator.Tests;

public sealed class DeterministicAgentSimulatorTests
{
    [Fact]
    public async Task Simulator_ShouldProduceDeterministicStarterResults()
    {
        var request = new ArchitectureRequest
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

        var coordinator = new CoordinatorService();
        var coordination = coordinator.CreateRun(request);

        coordination.Success.Should().BeTrue();

        IAgentExecutor simulator = new DeterministicAgentSimulator();

        var results = await simulator.ExecuteAsync(
            coordination.Run.RunId,
            request,
            coordination.Tasks);

        results.Should().HaveCount(3);
        results.Select(r => r.AgentType).Should().Contain(new[]
        {
            ArchiForge.Contracts.Common.AgentType.Topology,
            ArchiForge.Contracts.Common.AgentType.Cost,
            ArchiForge.Contracts.Common.AgentType.Compliance
        });
    }

    [Fact]
    public async Task Simulator_AndDecisionEngine_ShouldProduceManifest()
    {
        var request = new ArchitectureRequest
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

        var coordinator = new CoordinatorService();
        var coordination = coordinator.CreateRun(request);

        IAgentExecutor simulator = new DeterministicAgentSimulator();

        var results = await simulator.ExecuteAsync(
            coordination.Run.RunId,
            request,
            coordination.Tasks);

        var engine = new DecisionEngineService();

        var merge = engine.MergeResults(
            coordination.Run.RunId,
            request,
            "v1",
            results);

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
}
