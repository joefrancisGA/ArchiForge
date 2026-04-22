using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Reads;

using FluentAssertions;

using Moq;

using Cm = ArchLucid.Contracts.Manifest;
using Dm = ArchLucid.Decisioning.Models;

namespace ArchLucid.Persistence.Tests.Reads;

[Trait("Suite", "Core")]
public sealed class UnifiedGoldenManifestReaderTests
{
    private static UnifiedGoldenManifestReader CreateSut(
        Mock<ICoordinatorGoldenManifestRepository> coordinator,
        Mock<IRunRepository> runs,
        Mock<IGoldenManifestRepository>? authority = null,
        Mock<IAuthorityCommitProjectionBuilder>? projection = null,
        Mock<IArchitectureRequestRepository>? requests = null)
    {
        authority ??= new Mock<IGoldenManifestRepository>();
        projection ??= new Mock<IAuthorityCommitProjectionBuilder>();
        requests ??= new Mock<IArchitectureRequestRepository>();

        return new UnifiedGoldenManifestReader(
            coordinator.Object,
            runs.Object,
            authority.Object,
            projection.Object,
            requests.Object);
    }

    [Fact]
    public async Task ReadByRunIdAsync_WhenRunMissing_ReturnsNullWithoutCoordinatorCall()
    {
        Guid runId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        Mock<ICoordinatorGoldenManifestRepository> coordinator = new();
        UnifiedGoldenManifestReader sut = CreateSut(coordinator, runs);

        Cm.GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().BeNull();
        coordinator.Verify(
            c => c.GetByVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReadByRunIdAsync_WhenNoCurrentVersion_UsesV1RunKeyConvention()
    {
        Guid runId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

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

        Cm.GoldenManifest expected = new()
        {
            RunId = runId.ToString("D"),
            SystemName = "S",
            Metadata = new Cm.ManifestMetadata(),
            Governance = new Cm.ManifestGovernance(),
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        Mock<ICoordinatorGoldenManifestRepository> coordinator = new();
        string expectedKey = $"v1-{runId:N}";
        coordinator.Setup(c => c.GetByVersionAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        UnifiedGoldenManifestReader sut = CreateSut(coordinator, runs);

        Cm.GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().NotBeNull();
        manifest.RunId.Should().Be(runId.ToString("D"));
    }

    [Fact]
    public async Task ReadByRunIdAsync_WhenManifestRunIdMismatch_ReturnsNull()
    {
        Guid runId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

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

        Cm.GoldenManifest wrongRun = new()
        {
            RunId = Guid.NewGuid().ToString("D"),
            SystemName = "S",
            Metadata = new Cm.ManifestMetadata(),
            Governance = new Cm.ManifestGovernance(),
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        Mock<ICoordinatorGoldenManifestRepository> coordinator = new();
        coordinator.Setup(c => c.GetByVersionAsync("v2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(wrongRun);

        UnifiedGoldenManifestReader sut = CreateSut(coordinator, runs);

        Cm.GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().BeNull();
    }

    [Fact]
    public async Task ReadByRunIdAsync_WhenGoldenManifestIdPresent_ReturnsProjectedContract()
    {
        Guid runId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        Guid manifestId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

        RunRecord run = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            RunId = runId,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = null,
            GoldenManifestId = manifestId,
            ArchitectureRequestId = "req-z",
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        Cm.GoldenManifest projected = new()
        {
            RunId = runId.ToString("N"),
            SystemName = "FromAuthority",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new Cm.ManifestGovernance(),
            Metadata = new Cm.ManifestMetadata { ManifestVersion = "v1" },
        };

        Dm.GoldenManifest authorityRow = new()
        {
            ManifestId = manifestId,
            RunId = runId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "h",
            RuleSetId = "r",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
        };

        Mock<IGoldenManifestRepository> authority = new();
        authority.Setup(a => a.GetByIdAsync(scope, manifestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorityRow);

        Mock<IAuthorityCommitProjectionBuilder> projection = new();
        projection
            .Setup(
                p => p.BuildAsync(
                    It.IsAny<Dm.GoldenManifest>(),
                    It.Is<AuthorityCommitProjectionInput>(i => i.SystemName == "SysZ"),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(projected);

        Mock<IArchitectureRequestRepository> requests = new();
        requests.Setup(r => r.GetByIdAsync("req-z", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRequest
                {
                    RequestId = "req-z",
                    SystemName = "SysZ",
                    Description = "1234567890 description here",
                });

        Mock<ICoordinatorGoldenManifestRepository> coordinator = new();

        UnifiedGoldenManifestReader sut = CreateSut(coordinator, runs, authority, projection, requests);

        Cm.GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().NotBeNull();
        manifest.SystemName.Should().Be("FromAuthority");
        coordinator.Verify(
            c => c.GetByVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
