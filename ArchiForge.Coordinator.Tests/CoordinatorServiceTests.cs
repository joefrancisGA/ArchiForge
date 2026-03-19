using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Decisioning.Repositories;
using ArchiForge.Decisioning.Rules;
using ArchiForge.Decisioning.Services;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Manifest.Builders;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Coordinator.Tests;

public sealed class CoordinatorServiceTests
{
    [Fact]
    public void CreateRun_Should_CreateRunAndStarterTasks_When_RequestIsValid()
    {
        var request = new ArchitectureRequest
        {
            RequestId = "REQ-001",
            SystemName = "TestSystem",
            Description = "Design a secure Azure system."
        };

        var findingsRepo = new InMemoryFindingsSnapshotRepository();
        var manifestRepo = new InMemoryGoldenManifestRepository();
        var traceRepo = new InMemoryDecisionTraceRepository();
        IEnumerable<IFindingEngine> engines =
        [
            new RequirementFindingEngine(),
            new TopologySanityFindingEngine()
        ];
        var findingsOrchestrator = new FindingsOrchestrator(engines, findingsRepo);
        var ruleProvider = new InMemoryDecisionRuleProvider();
        var decisionEngine = new RuleBasedDecisionEngine(
            ruleProvider,
            new DefaultGoldenManifestBuilder(),
            new GoldenManifestValidator(),
            manifestRepo,
            traceRepo);

        var service = new CoordinatorService(
            new NullContextIngestionService(),
            new NullKnowledgeGraphService(),
            findingsOrchestrator,
            decisionEngine);

        var result = service.CreateRun(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Run);
        Assert.Equal(4, result.Tasks.Count);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Topology);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Cost);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Compliance);
    }

    private sealed class NullContextIngestionService : IContextIngestionService
    {
        public Task<ContextSnapshot> IngestAsync(ContextIngestionRequest request, CancellationToken ct)
        {
            // No-op for tests
            return Task.FromResult(new ContextSnapshot
            {
                SnapshotId = Guid.NewGuid(),
                RunId = Guid.Empty,
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
}