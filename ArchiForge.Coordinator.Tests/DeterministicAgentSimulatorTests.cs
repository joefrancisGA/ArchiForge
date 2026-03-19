using ArchiForge.Contracts.Agents;
using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.DecisionEngine.Validation;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using FluentAssertions;

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

        var coordinator = CreateCoordinator();
        var coordination = coordinator.CreateRun(request);

        coordination.Success.Should().BeTrue();

        DeterministicAgentSimulator simulator = new();
        var evidence = CreateMinimalEvidence(coordination.Run.RunId, request);

        var results = await simulator.ExecuteAsync(
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

        var coordinator = CreateCoordinator();
        var coordination = coordinator.CreateRun(request);

        DeterministicAgentSimulator simulator = new();
        var evidence = CreateMinimalEvidence(coordination.Run.RunId, request);

        var results = await simulator.ExecuteAsync(
            coordination.Run.RunId,
            request,
            evidence,
            coordination.Tasks);

        var validationService = new SchemaValidationService(
            NullLogger<SchemaValidationService>.Instance,
            Options.Create(new SchemaValidationOptions()));

        var engine = new DecisionEngineService(validationService);

        var merge = engine.MergeResults(
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

    private sealed class NullContextIngestionService : IContextIngestionService
    {
        public Task<ContextSnapshot> IngestAsync(ContextIngestionRequest request, CancellationToken ct)
        {
            return Task.FromResult(new ContextSnapshot
            {
                SnapshotId = Guid.NewGuid(),
                RunId = request.RunId,
                CreatedUtc = DateTime.UtcNow
            });
        }
    }

    private sealed class NullKnowledgeGraphService : IKnowledgeGraphService
    {
        public Task<GraphSnapshot> BuildSnapshotAsync(ContextSnapshot contextSnapshot, CancellationToken ct)
        {
            return Task.FromResult(new GraphSnapshot
            {
                GraphSnapshotId = Guid.NewGuid(),
                ContextSnapshotId = contextSnapshot.SnapshotId,
                RunId = contextSnapshot.RunId,
                CreatedUtc = DateTime.UtcNow
            });
        }
    }

    private static CoordinatorService CreateCoordinator()
    {
        var findingsRepo = new Decisioning.Repositories.InMemoryFindingsSnapshotRepository();
        var manifestRepo = new Decisioning.Repositories.InMemoryGoldenManifestRepository();
        var traceRepo = new Decisioning.Repositories.InMemoryDecisionTraceRepository();
        var engines = new Decisioning.Interfaces.IFindingEngine[]
        {
            new ArchiForge.Decisioning.Services.RequirementFindingEngine(),
            new ArchiForge.Decisioning.Services.TopologySanityFindingEngine()
        };
        var findingsOrchestrator = new ArchiForge.Decisioning.Services.FindingsOrchestrator(engines, findingsRepo);
        var ruleProvider = new Decisioning.Rules.InMemoryDecisionRuleProvider();
        var decisionEngine = new ArchiForge.Decisioning.Services.RuleBasedDecisionEngine(
            ruleProvider,
            new Decisioning.Manifest.Builders.DefaultGoldenManifestBuilder(),
            new ArchiForge.Decisioning.Services.GoldenManifestValidator(),
            manifestRepo,
            traceRepo);

        return new CoordinatorService(
            new NullContextIngestionService(),
            new NullKnowledgeGraphService(),
            findingsOrchestrator,
            decisionEngine,
            TestArtifactSynthesisFactory.Create());
    }
}
