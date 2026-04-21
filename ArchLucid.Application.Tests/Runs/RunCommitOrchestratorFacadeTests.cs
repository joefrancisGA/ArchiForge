using ArchLucid.Application;
using ArchLucid.Application.Runs;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Manifest;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Runs;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RunCommitOrchestratorFacadeTests
{
    [Fact]
    public async Task CommitRunAsync_delegates_to_inner_orchestrator()
    {
        Mock<IArchitectureRunCommitOrchestrator> inner = new();
        CommitRunResult expected = new()
        {
            Manifest = new GoldenManifest(),
            DecisionTraces = [],
            Warnings = [],
        };

        inner.Setup(x => x.CommitRunAsync("run-1", It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        IRunCommitOrchestrator sut = new RunCommitOrchestratorFacade(inner.Object);

        CommitRunResult actual = await sut.CommitRunAsync("run-1");

        actual.Should().BeSameAs(expected);
        inner.Verify(x => x.CommitRunAsync("run-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
