using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using Xunit;

namespace ArchiForge.Coordinator.Tests;

public sealed class CoordinatorServiceTests
{
    [Fact]
    public void CreateRun_Should_CreateRunAndThreeTasks_When_RequestIsValid()
    {
        var request = new ArchitectureRequest
        {
            RequestId = "REQ-001",
            SystemName = "TestSystem",
            Description = "Design a secure Azure system."
        };

        var service = new CoordinatorService();

        var result = service.CreateRun(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Run);
        Assert.Equal(3, result.Tasks.Count);
        Assert.Contains(result.Tasks, t => t.AgentType == ArchiForge.Contracts.Common.AgentType.Topology);
        Assert.Contains(result.Tasks, t => t.AgentType == ArchiForge.Contracts.Common.AgentType.Cost);
        Assert.Contains(result.Tasks, t => t.AgentType == ArchiForge.Contracts.Common.AgentType.Compliance);
    }
}