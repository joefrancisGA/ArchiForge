using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;

namespace ArchiForge.Coordinator.Tests;

public sealed class CoordinatorServiceTests
{
    [Fact]
    public async Task CreateRun_Should_CreateRunAndStarterTasks_When_RequestIsValid()
    {
        var request = new ArchitectureRequest
        {
            RequestId = "REQ-001",
            SystemName = "TestSystem",
            Description = "Design a secure Azure system."
        };

        var service = new CoordinatorService(new FakeAuthorityRunOrchestrator());

        var result = await service.CreateRunAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Run);
        Assert.Equal(4, result.Tasks.Count);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Topology);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Cost);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Compliance);
    }
}