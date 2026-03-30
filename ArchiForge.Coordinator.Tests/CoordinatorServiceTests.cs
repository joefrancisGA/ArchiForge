using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.Coordinator.Tests;

/// <summary>
/// Tests for Coordinator Service.
/// </summary>

[Trait("Suite", "Core")]
public sealed class CoordinatorServiceTests
{
    [Fact]
    public async Task CreateRun_Should_CreateRunAndStarterTasks_When_RequestIsValid()
    {
        ArchitectureRequest request = new()
        {
            RequestId = "REQ-001",
            SystemName = "TestSystem",
            Description = "Design a secure Azure system."
        };

        CoordinatorService service = new(new FakeAuthorityRunOrchestrator(), NullLogger<CoordinatorService>.Instance);

        CoordinationResult result = await service.CreateRunAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Run);
        Assert.Equal(4, result.Tasks.Count);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Topology);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Cost);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Compliance);
    }
}
