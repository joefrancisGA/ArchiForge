using ArchLucid.Contracts.Requests;
using ArchLucid.Coordinator.Services;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Coordinator.Tests;

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

        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(r => r.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        });

        CoordinatorService service = new(
            new FakeAuthorityRunOrchestrator(),
            runRepo.Object,
            scopeProvider.Object,
            NullLogger<CoordinatorService>.Instance);

        CoordinationResult result = await service.CreateRunAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Run);
        Assert.Equal(4, result.Tasks.Count);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Topology);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Cost);
        Assert.Contains(result.Tasks, t => t.AgentType == Contracts.Common.AgentType.Compliance);
    }
}
