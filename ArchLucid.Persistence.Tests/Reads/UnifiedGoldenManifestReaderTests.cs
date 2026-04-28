using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Reads;

using Moq;

using Cm = ArchLucid.Contracts.Manifest;
using Dm = ArchLucid.Decisioning.Models;

namespace ArchLucid.Persistence.Tests.Reads;

/// <summary>
///     ADR 0030 PR A3 (2026-04-24): rewritten as authority-only after the legacy
///     <c>ICoordinatorGoldenManifestRepository</c> read path was removed (the SQL table
///     <c>dbo.GoldenManifestVersions</c> had already been dropped in PR A4 / migration 111).
/// </summary>
[Trait("Suite", "Core")]
public sealed class UnifiedGoldenManifestReaderTests
{
    private static UnifiedGoldenManifestReader CreateSut(
        Mock<IRunRepository> runs,
        Mock<IGoldenManifestRepository>? authority = null,
        Mock<IAuthorityCommitProjectionBuilder>? projection = null,
        Mock<IArchitectureRequestRepository>? requests = null,
        Mock<IScopeContextProvider>? scopeProvider = null)
    {
        if (authority is null)
        {
            authority = new Mock<IGoldenManifestRepository>();
            authority.Setup(a => a.GetByContractManifestVersionAsync(
                    It.IsAny<ScopeContext>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Dm.ManifestDocument?)null);
        }

        projection ??= new Mock<IAuthorityCommitProjectionBuilder>();
        requests ??= new Mock<IArchitectureRequestRepository>();
        scopeProvider ??= new Mock<IScopeContextProvider>();
        ScopeContext ambientScope = new()
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(ambientScope);

        return new UnifiedGoldenManifestReader(
            runs.Object,
            authority.Object,
            projection.Object,
            requests.Object,
            scopeProvider.Object);
    }

    [Fact]
    public async Task ReadByRunIdAsync_WhenRunMissing_ReturnsNullWithoutAuthorityCall()
    {
        Guid runId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        Mock<IGoldenManifestRepository> authority = new();
        UnifiedGoldenManifestReader sut = CreateSut(runs, authority);

        Cm.GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().BeNull();
        authority.Verify(
            a => a.GetByContractManifestVersionAsync(
                It.IsAny<ScopeContext>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReadByRunIdAsync_WhenNoCurrentVersion_ProbesAuthorityWithV1RunKeyConvention()
    {
        Guid runId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        RunRecord run = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            RunId = runId,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = null
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        string expectedVersion = $"v1-{runId:N}";
        Dm.ManifestDocument authorityByVersion = NewAuthorityRow(scope, runId);

        Mock<IGoldenManifestRepository> authority = new();
        authority.Setup(a => a.GetByContractManifestVersionAsync(
                It.IsAny<ScopeContext>(),
                expectedVersion,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorityByVersion);

        Cm.GoldenManifest projected = new()
        {
            RunId = runId.ToString("D"),
            SystemName = "default",
            Metadata = new Cm.ManifestMetadata { ManifestVersion = expectedVersion },
            Governance = new Cm.ManifestGovernance()
        };

        Mock<IAuthorityCommitProjectionBuilder> projection = new();
        projection
            .Setup(p => p.BuildAsync(
                It.IsAny<Dm.ManifestDocument>(),
                It.IsAny<AuthorityCommitProjectionInput>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projected);

        UnifiedGoldenManifestReader sut = CreateSut(runs, authority, projection);

        Cm.GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().NotBeNull();
        manifest!.RunId.Should().Be(runId.ToString("D"));
    }

    [Fact]
    public async Task ReadByRunIdAsync_WhenAuthorityHasNothing_ReturnsNull()
    {
        Guid runId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        RunRecord run = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            RunId = runId,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v2"
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        Mock<IGoldenManifestRepository> authority = new();
        authority.Setup(a => a.GetByContractManifestVersionAsync(
                It.IsAny<ScopeContext>(),
                "v2",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dm.ManifestDocument?)null);

        UnifiedGoldenManifestReader sut = CreateSut(runs, authority);

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
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
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
            ArchitectureRequestId = "req-z"
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
            Metadata = new Cm.ManifestMetadata { ManifestVersion = "v1" }
        };

        Dm.ManifestDocument authorityRow = NewAuthorityRow(scope, runId, manifestId);

        Mock<IGoldenManifestRepository> authority = new();
        authority.Setup(a => a.GetByIdAsync(scope, manifestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorityRow);

        Mock<IAuthorityCommitProjectionBuilder> projection = new();
        projection
            .Setup(p => p.BuildAsync(
                It.IsAny<Dm.ManifestDocument>(),
                It.Is<AuthorityCommitProjectionInput>(i => i.SystemName == "SysZ"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projected);

        Mock<IArchitectureRequestRepository> requests = new();
        requests.Setup(r => r.GetByIdAsync("req-z", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRequest
                {
                    RequestId = "req-z", SystemName = "SysZ", Description = "1234567890 description here"
                });

        UnifiedGoldenManifestReader sut = CreateSut(runs, authority, projection, requests);

        Cm.GoldenManifest? manifest = await sut.ReadByRunIdAsync(scope, runId);

        manifest.Should().NotBeNull();
        manifest!.SystemName.Should().Be("FromAuthority");
    }

    private static Dm.ManifestDocument NewAuthorityRow(ScopeContext scope, Guid runId, Guid? manifestId = null)
    {
        return new Dm.ManifestDocument
        {
            ManifestId = manifestId ?? Guid.NewGuid(),
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
            RuleSetHash = "rh"
        };
    }
}
