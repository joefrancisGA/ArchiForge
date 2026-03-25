using ArchiForge.Application;
using ArchiForge.Application.Agents;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;

using FluentAssertions;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Ensures replay loads the source run through <see cref="IRunDetailQueryService"/> (canonical path)
/// rather than assembling run + tasks from separate repository calls.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReplayRunServiceTests
{
    private readonly Mock<IRunDetailQueryService> _runDetailQueryService = new();
    private readonly Mock<IArchitectureRunRepository> _runRepository = new();
    private readonly Mock<IArchitectureRequestRepository> _requestRepository = new();
    private readonly Mock<IAgentEvidencePackageRepository> _evidenceRepository = new();
    private readonly Mock<IAgentExecutorResolver> _executorResolver = new();
    private readonly Mock<IDecisionEngineService> _decisionEngine = new();
    private readonly Mock<IGoldenManifestRepository> _manifestRepository = new();
    private readonly Mock<IDecisionTraceRepository> _decisionTraceRepository = new();
    private readonly ReplayRunService _sut;

    public ReplayRunServiceTests()
    {
        _sut = new ReplayRunService(
            _executorResolver.Object,
            _decisionEngine.Object,
            _requestRepository.Object,
            _runDetailQueryService.Object,
            _runRepository.Object,
            _manifestRepository.Object,
            _decisionTraceRepository.Object,
            _evidenceRepository.Object);
    }

    [Fact]
    public async Task ReplayAsync_WhenRunDetailMissing_ThrowsRunNotFoundException()
    {
        _runDetailQueryService
            .Setup(s => s.GetRunDetailAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Func<Task<ReplayRunResult>> act = async () => await _sut.ReplayAsync("missing");

        await act.Should().ThrowAsync<RunNotFoundException>();
        _runRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplayAsync_WhenNoTasks_ThrowsInvalidOperationException()
    {
        _runDetailQueryService
            .Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchitectureRunDetail
            {
                Run = new ArchitectureRun { RunId = "run-1", RequestId = "req-1", Status = ArchitectureRunStatus.Created },
                Tasks = [],
                Results = []
            });

        Func<Task<ReplayRunResult>> act = async () => await _sut.ReplayAsync("run-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No tasks*");
    }
}
