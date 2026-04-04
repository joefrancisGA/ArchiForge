using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchiForge.Application.Tests;

/// <summary>
/// <see cref="RunDetailQueryService"/> coverage in the Application test assembly (broken manifest flag, trace load guard).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RunDetailQueryServiceApplicationTests
{
    [Fact]
    public async Task GetRunDetailAsync_when_manifest_version_set_but_manifest_missing_sets_HasBrokenManifestReference()
    {
        Mock<IArchitectureRunRepository> runRepo = new();
        Mock<IAgentTaskRepository> taskRepo = new();
        Mock<IAgentResultRepository> resultRepo = new();
        Mock<IGoldenManifestRepository> manifestRepo = new();
        Mock<IDecisionTraceRepository> traceRepo = new();

        ArchitectureRun run = new()
        {
            RunId = "run-x",
            RequestId = "req-x",
            Status = ArchitectureRunStatus.Committed,
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v-missing",
        };

        runRepo.Setup(r => r.GetByIdAsync("run-x", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        taskRepo.Setup(r => r.GetByRunIdAsync("run-x", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        resultRepo.Setup(r => r.GetByRunIdAsync("run-x", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        manifestRepo.Setup(r => r.GetByVersionAsync("v-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldenManifest?)null);

        RunDetailQueryService sut = new(
            runRepo.Object,
            taskRepo.Object,
            resultRepo.Object,
            manifestRepo.Object,
            traceRepo.Object,
            new Mock<ILogger<RunDetailQueryService>>().Object);

        ArchitectureRunDetail? detail = await sut.GetRunDetailAsync("run-x");

        detail.Should().NotBeNull();
        detail.Manifest.Should().BeNull();
        detail.HasBrokenManifestReference.Should().BeTrue();
        traceRepo.Verify(t => t.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRunDetailAsync_when_manifest_resolved_HasBrokenManifestReference_is_false()
    {
        Mock<IArchitectureRunRepository> runRepo = new();
        Mock<IAgentTaskRepository> taskRepo = new();
        Mock<IAgentResultRepository> resultRepo = new();
        Mock<IGoldenManifestRepository> manifestRepo = new();
        Mock<IDecisionTraceRepository> traceRepo = new();

        ArchitectureRun run = new()
        {
            RunId = "run-y",
            RequestId = "req-y",
            Status = ArchitectureRunStatus.Committed,
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v1",
        };

        GoldenManifest manifest = new()
        {
            RunId = "run-y",
            SystemName = "S",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata { ManifestVersion = "v1" },
        };

        runRepo.Setup(r => r.GetByIdAsync("run-y", It.IsAny<CancellationToken>())).ReturnsAsync(run);
        taskRepo.Setup(r => r.GetByRunIdAsync("run-y", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        resultRepo.Setup(r => r.GetByRunIdAsync("run-y", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        manifestRepo.Setup(r => r.GetByVersionAsync("v1", It.IsAny<CancellationToken>())).ReturnsAsync(manifest);
        traceRepo.Setup(r => r.GetByRunIdAsync("run-y", It.IsAny<CancellationToken>())).ReturnsAsync([]);

        RunDetailQueryService sut = new(
            runRepo.Object,
            taskRepo.Object,
            resultRepo.Object,
            manifestRepo.Object,
            traceRepo.Object,
            new Mock<ILogger<RunDetailQueryService>>().Object);

        ArchitectureRunDetail? detail = await sut.GetRunDetailAsync("run-y");

        detail.Should().NotBeNull();
        detail.HasBrokenManifestReference.Should().BeFalse();
        traceRepo.Verify(t => t.GetByRunIdAsync("run-y", It.IsAny<CancellationToken>()), Times.Once);
    }
}
