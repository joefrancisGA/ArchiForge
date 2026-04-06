using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Queries;
using ArchiForge.Persistence.Replay;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// Unit tests for <see cref="AuthorityReplayService"/> covering the four top-level paths:
/// unknown run → null, ReconstructOnly (no writes), RebuildManifest (decision engine only),
/// and RebuildArtifacts (decision engine + artifact synthesis).
/// </summary>
[Trait("Category", "Unit")]
public sealed class AuthorityReplayServiceTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static RunDetailDto MakeDetailDto(Guid runId) => new()
    {
        Run = new RunRecord
        {
            RunId = runId,
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ScopeProjectId = Guid.NewGuid()
        }
    };

    private static RunDetailDto MakeFullDetailDto(Guid runId)
    {
        RunDetailDto dto = MakeDetailDto(runId);
        dto.ContextSnapshot = new ContextIngestion.Models.ContextSnapshot { SnapshotId = Guid.NewGuid() };
        dto.GraphSnapshot = new GraphSnapshot { GraphSnapshotId = Guid.NewGuid() };
        dto.FindingsSnapshot = new FindingsSnapshot { FindingsSnapshotId = Guid.NewGuid() };
        return dto;
    }

    private static (AuthorityReplayService Sut,
        Mock<IAuthorityQueryService> Query,
        Mock<IDecisionEngine> DecisionEngine,
        Mock<IArtifactSynthesisService> ArtifactSvc,
        Mock<IManifestHashService> HashSvc)
        Build(RunDetailDto? returnedDto = null)
    {
        Mock<IAuthorityQueryService> query = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        Mock<IDecisionEngine> decisionEngine = new();
        Mock<IArtifactSynthesisService> artifactSvc = new();
        Mock<IManifestHashService> hashSvc = new();
        Mock<IDecisionTraceRepository> traceRepo = new();
        Mock<IGoldenManifestRepository> manifestRepo = new();
        Mock<IArtifactBundleRepository> artifactRepo = new();

        scopeProvider
            .Setup(x => x.GetCurrentScope())
            .Returns(new ScopeContext());

        query
            .Setup(x => x.GetRunDetailAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        hashSvc
            .Setup(x => x.ComputeHash(It.IsAny<GoldenManifest>()))
            .Returns("abc123");

        decisionEngine
            .Setup(x => x.DecideAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<GraphSnapshot>(),
                It.IsAny<FindingsSnapshot>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new GoldenManifest { ManifestId = Guid.NewGuid(), RunId = Guid.NewGuid() },
                DecisionTrace.FromRuleAudit(new RuleAuditTracePayload { DecisionTraceId = Guid.NewGuid() })));

        traceRepo
            .Setup(x => x.SaveAsync(It.IsAny<DecisionTrace>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        manifestRepo
            .Setup(x => x.SaveAsync(It.IsAny<GoldenManifest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        artifactSvc
            .Setup(x => x.SynthesizeAsync(It.IsAny<GoldenManifest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtifactBundle { BundleId = Guid.NewGuid() });

        artifactRepo
            .Setup(x => x.SaveAsync(It.IsAny<ArtifactBundle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AuthorityReplayService sut = new(
            query.Object,
            scopeProvider.Object,
            decisionEngine.Object,
            artifactSvc.Object,
            hashSvc.Object,
            traceRepo.Object,
            manifestRepo.Object,
            artifactRepo.Object);

        return (sut, query, decisionEngine, artifactSvc, hashSvc);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Unknown RunId → null
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReplayAsync_UnknownRunId_ReturnsNull()
    {
        (AuthorityReplayService sut, _, _, _, _) = Build(returnedDto: null);

        ReplayResult? result = await sut.ReplayAsync(
            new ReplayRequest { RunId = Guid.NewGuid(), Mode = ReplayMode.ReconstructOnly },
            CancellationToken.None);

        result.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ReconstructOnly — no decision-engine or artifact calls
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReplayAsync_ReconstructOnly_DoesNotCallDecisionEngine()
    {
        Guid runId = Guid.NewGuid();
        (AuthorityReplayService sut, _, Mock<IDecisionEngine> engine, Mock<IArtifactSynthesisService> artifactSvc, _)
            = Build(MakeDetailDto(runId));

        ReplayResult? result = await sut.ReplayAsync(
            new ReplayRequest { RunId = runId, Mode = ReplayMode.ReconstructOnly },
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Mode.Should().Be(ReplayMode.ReconstructOnly);
        result.RebuiltManifest.Should().BeNull();
        engine.Verify(
            x => x.DecideAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<GraphSnapshot>(), It.IsAny<FindingsSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        artifactSvc.Verify(
            x => x.SynthesizeAsync(It.IsAny<GoldenManifest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RebuildManifest — decision engine called, artifact synthesis NOT called
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReplayAsync_RebuildManifest_CallsDecisionEngineButNotArtifactSynthesis()
    {
        Guid runId = Guid.NewGuid();
        (AuthorityReplayService sut, _, Mock<IDecisionEngine> engine, Mock<IArtifactSynthesisService> artifactSvc, _)
            = Build(MakeFullDetailDto(runId));

        ReplayResult? result = await sut.ReplayAsync(
            new ReplayRequest { RunId = runId, Mode = ReplayMode.RebuildManifest },
            CancellationToken.None);

        result.Should().NotBeNull();
        result.RebuiltManifest.Should().NotBeNull();
        result.RebuiltArtifactBundle.Should().BeNull();
        engine.Verify(
            x => x.DecideAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<GraphSnapshot>(), It.IsAny<FindingsSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Once);
        artifactSvc.Verify(
            x => x.SynthesizeAsync(It.IsAny<GoldenManifest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RebuildArtifacts — both decision engine and artifact synthesis called
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReplayAsync_RebuildArtifacts_CallsBothDecisionEngineAndArtifactSynthesis()
    {
        Guid runId = Guid.NewGuid();
        (AuthorityReplayService sut, _, Mock<IDecisionEngine> engine, Mock<IArtifactSynthesisService> artifactSvc, _)
            = Build(MakeFullDetailDto(runId));

        ReplayResult? result = await sut.ReplayAsync(
            new ReplayRequest { RunId = runId, Mode = ReplayMode.RebuildArtifacts },
            CancellationToken.None);

        result.Should().NotBeNull();
        result.RebuiltManifest.Should().NotBeNull();
        result.RebuiltArtifactBundle.Should().NotBeNull();
        engine.Verify(
            x => x.DecideAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<GraphSnapshot>(), It.IsAny<FindingsSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Once);
        artifactSvc.Verify(
            x => x.SynthesizeAsync(It.IsAny<GoldenManifest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
