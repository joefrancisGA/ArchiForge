using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Moq;

namespace ArchLucid.Persistence.Tests.Queries;

public sealed class AuthorityQueryServiceGetRunDetailArtifactBundleTests
{
    [Theory]
    [InlineData(typeof(InMemoryAuthorityQueryService))]
    [InlineData(typeof(DapperAuthorityQueryService))]
    public async Task GetRunDetailAsync_loads_bundle_by_manifest_when_golden_manifest_set_but_bundle_row_id_null(Type implementationType)
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId };

        RunRecord run = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ScopeProjectId = projectId,
            RunId = runId,
            ProjectId = "default",
            GoldenManifestId = manifestId,
            ArtifactBundleId = null
        };

        ArtifactBundle bundle = new() { ManifestId = manifestId, BundleId = Guid.NewGuid() };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        Mock<IContextSnapshotRepository> contextSnapshots = new();
        Mock<IGraphSnapshotRepository> graphSnapshots = new();
        Mock<IFindingsSnapshotRepository> findingsSnapshots = new();
        Mock<IDecisionTraceRepository> traces = new();
        Mock<IGoldenManifestRepository> manifests = new();
        Mock<IArtifactBundleRepository> bundles = new();
        bundles
            .Setup(b => b.GetByManifestIdAsync(scope, manifestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bundle);

        IAuthorityQueryService sut = CreateQueryService(
            implementationType,
            runs.Object,
            contextSnapshots.Object,
            graphSnapshots.Object,
            findingsSnapshots.Object,
            traces.Object,
            manifests.Object,
            bundles.Object);

        RunDetailDto? detail = await sut.GetRunDetailAsync(scope, runId, CancellationToken.None);

        detail.Should().NotBeNull();
        detail!.ArtifactBundle.Should().BeSameAs(bundle);
        bundles.Verify(b => b.GetByManifestIdAsync(scope, manifestId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(typeof(InMemoryAuthorityQueryService))]
    [InlineData(typeof(DapperAuthorityQueryService))]
    public async Task GetRunDetailAsync_does_not_query_bundle_when_no_golden_manifest(Type implementationType)
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        ScopeContext scope = new() { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId };

        RunRecord run = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ScopeProjectId = projectId,
            RunId = runId,
            ProjectId = "default",
            GoldenManifestId = null,
            ArtifactBundleId = Guid.NewGuid()
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        Mock<IContextSnapshotRepository> contextSnapshots = new();
        Mock<IGraphSnapshotRepository> graphSnapshots = new();
        Mock<IFindingsSnapshotRepository> findingsSnapshots = new();
        Mock<IDecisionTraceRepository> traces = new();
        Mock<IGoldenManifestRepository> manifests = new();
        Mock<IArtifactBundleRepository> bundles = new();

        IAuthorityQueryService sut = CreateQueryService(
            implementationType,
            runs.Object,
            contextSnapshots.Object,
            graphSnapshots.Object,
            findingsSnapshots.Object,
            traces.Object,
            manifests.Object,
            bundles.Object);

        RunDetailDto? detail = await sut.GetRunDetailAsync(scope, runId, CancellationToken.None);

        detail.Should().NotBeNull();
        detail!.ArtifactBundle.Should().BeNull();
        bundles.Verify(
            b => b.GetByManifestIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static IAuthorityQueryService CreateQueryService(
        Type implementationType,
        IRunRepository runRepository,
        IContextSnapshotRepository contextSnapshotRepository,
        IGraphSnapshotRepository graphSnapshotRepository,
        IFindingsSnapshotRepository findingsSnapshotRepository,
        IDecisionTraceRepository decisionTraceRepository,
        IGoldenManifestRepository goldenManifestRepository,
        IArtifactBundleRepository artifactBundleRepository)
    {
        if (implementationType == typeof(InMemoryAuthorityQueryService))
        {
            return new InMemoryAuthorityQueryService(
                runRepository,
                contextSnapshotRepository,
                graphSnapshotRepository,
                findingsSnapshotRepository,
                decisionTraceRepository,
                goldenManifestRepository,
                artifactBundleRepository);
        }


        if (implementationType == typeof(DapperAuthorityQueryService))
        {
            return new DapperAuthorityQueryService(
                runRepository,
                contextSnapshotRepository,
                graphSnapshotRepository,
                findingsSnapshotRepository,
                decisionTraceRepository,
                goldenManifestRepository,
                artifactBundleRepository);
        }


        throw new ArgumentException($"Unsupported query service type: {implementationType.Name}", nameof(implementationType));
    }
}
