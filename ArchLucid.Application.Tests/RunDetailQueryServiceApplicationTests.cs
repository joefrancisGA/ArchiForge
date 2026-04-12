using ArchLucid.Application;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Application.Tests;

/// <summary>
/// <see cref="RunDetailQueryService"/> coverage in the Application test assembly (broken manifest flag, trace load guard).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RunDetailQueryServiceApplicationTests
{
    private static ScopeContext NewScope() =>
        new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

    [Fact]
    public async Task GetRunDetailAsync_when_manifest_version_set_but_manifest_missing_sets_HasBrokenManifestReference()
    {
        ScopeContext scope = NewScope();
        Guid runGuid = Guid.Parse("55555555-5555-5555-5555-555555555555");
        string runN = runGuid.ToString("N");

        Mock<IRunRepository> runRepo = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        Mock<IAgentTaskRepository> taskRepo = new();
        Mock<IAgentResultRepository> resultRepo = new();
        Mock<ICoordinatorGoldenManifestRepository> manifestRepo = new();
        Mock<ICoordinatorDecisionTraceRepository> traceRepo = new();

        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        RunRecord record = new()
        {
            RunId = runGuid,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = "p",
            ArchitectureRequestId = "req-x",
            LegacyRunStatus = ArchitectureRunStatus.Committed.ToString(),
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v-missing",
        };

        runRepo.Setup(r => r.GetByIdAsync(scope, runGuid, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        taskRepo.Setup(r => r.GetByRunIdAsync(runN, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        resultRepo.Setup(r => r.GetByRunIdAsync(runN, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        manifestRepo.Setup(r => r.GetByVersionAsync("v-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldenManifest?)null);

        RunDetailQueryService sut = new(
            runRepo.Object,
            scopeProvider.Object,
            taskRepo.Object,
            resultRepo.Object,
            manifestRepo.Object,
            traceRepo.Object,
            new Mock<ILogger<RunDetailQueryService>>().Object);

        ArchitectureRunDetail? detail = await sut.GetRunDetailAsync(runN);

        detail.Should().NotBeNull();
        detail.Manifest.Should().BeNull();
        detail.HasBrokenManifestReference.Should().BeTrue();
        traceRepo.Verify(t => t.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRunDetailAsync_when_manifest_resolved_HasBrokenManifestReference_is_false()
    {
        ScopeContext scope = NewScope();
        Guid runGuid = Guid.Parse("66666666-6666-6666-6666-666666666666");
        string runN = runGuid.ToString("N");

        Mock<IRunRepository> runRepo = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        Mock<IAgentTaskRepository> taskRepo = new();
        Mock<IAgentResultRepository> resultRepo = new();
        Mock<ICoordinatorGoldenManifestRepository> manifestRepo = new();
        Mock<ICoordinatorDecisionTraceRepository> traceRepo = new();

        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        RunRecord record = new()
        {
            RunId = runGuid,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = "p",
            ArchitectureRequestId = "req-y",
            LegacyRunStatus = ArchitectureRunStatus.Committed.ToString(),
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v1",
        };

        GoldenManifest manifest = new()
        {
            RunId = runN,
            SystemName = "S",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata { ManifestVersion = "v1" },
        };

        runRepo.Setup(r => r.GetByIdAsync(scope, runGuid, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        taskRepo.Setup(r => r.GetByRunIdAsync(runN, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        resultRepo.Setup(r => r.GetByRunIdAsync(runN, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        manifestRepo.Setup(r => r.GetByVersionAsync("v1", It.IsAny<CancellationToken>())).ReturnsAsync(manifest);
        traceRepo.Setup(r => r.GetByRunIdAsync(runN, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        RunDetailQueryService sut = new(
            runRepo.Object,
            scopeProvider.Object,
            taskRepo.Object,
            resultRepo.Object,
            manifestRepo.Object,
            traceRepo.Object,
            new Mock<ILogger<RunDetailQueryService>>().Object);

        ArchitectureRunDetail? detail = await sut.GetRunDetailAsync(runN);

        detail.Should().NotBeNull();
        detail.HasBrokenManifestReference.Should().BeFalse();
        traceRepo.Verify(t => t.GetByRunIdAsync(runN, It.IsAny<CancellationToken>()), Times.Once);
    }
}
