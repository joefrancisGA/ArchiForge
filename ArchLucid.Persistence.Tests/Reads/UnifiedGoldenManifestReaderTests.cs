using ArchLucid.Contracts.Manifest;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Reads;

using FluentAssertions;

using Moq;

namespace ArchLucid.Persistence.Tests.Reads;

[Trait("Suite", "Core")]
public sealed class UnifiedGoldenManifestReaderTests
{
    [Fact]
    public async Task ReadByRunIdAsync_WhenRunMissing_ReturnsNullWithoutCoordinatorCall()
    {
        Guid runId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        ScopeContext scope = new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        Mock<ICoordinatorGoldenManifestRepository> coordinator = new();
        UnifiedGoldenManifestReader sut = new(coordinator.Object, runs.Object);

        GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().BeNull();
        coordinator.Verify(
            c => c.GetByVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReadByRunIdAsync_WhenNoCurrentVersion_UsesV1RunKeyConvention()
    {
        Guid runId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        ScopeContext scope = new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        RunRecord run = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            RunId = runId,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = null,
        };

        GoldenManifest expected = new()
        {
            RunId = runId.ToString("D"),
            SystemName = "S",
            Metadata = new ManifestMetadata(),
            Governance = new ManifestGovernance(),
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        Mock<ICoordinatorGoldenManifestRepository> coordinator = new();
        string expectedKey = $"v1-{runId:N}";
        coordinator.Setup(c => c.GetByVersionAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        UnifiedGoldenManifestReader sut = new(coordinator.Object, runs.Object);

        GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().NotBeNull();
        manifest!.RunId.Should().Be(runId.ToString("D"));
    }

    [Fact]
    public async Task ReadByRunIdAsync_WhenManifestRunIdMismatch_ReturnsNull()
    {
        Guid runId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        ScopeContext scope = new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        RunRecord run = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            RunId = runId,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v2",
        };

        GoldenManifest wrongRun = new()
        {
            RunId = Guid.NewGuid().ToString("D"),
            SystemName = "S",
            Metadata = new ManifestMetadata(),
            Governance = new ManifestGovernance(),
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        Mock<ICoordinatorGoldenManifestRepository> coordinator = new();
        coordinator.Setup(c => c.GetByVersionAsync("v2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(wrongRun);

        UnifiedGoldenManifestReader sut = new(coordinator.Object, runs.Object);

        GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().BeNull();
    }
}
