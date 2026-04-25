using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Services;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Findings Orchestrator.
/// </summary>

[Trait("Suite", "Core")]
public sealed class FindingsOrchestratorTests
{
    private static GraphSnapshot EmptyGraph() => new()
    {
        GraphSnapshotId = Guid.NewGuid(),
        ContextSnapshotId = Guid.NewGuid(),
        RunId = Guid.NewGuid()
    };

    [Fact]
    public async Task GenerateFindingsSnapshotAsync_NullGraph_Throws()
    {
        Mock<IFindingEngine> engine = new(MockBehavior.Strict);
        Mock<IFindingPayloadValidator> validator = new(MockBehavior.Strict);
        FindingsOrchestrator sut = new([engine.Object], validator.Object, NullLogger<FindingsOrchestrator>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.GenerateFindingsSnapshotAsync(Guid.NewGuid(), Guid.NewGuid(), null!, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateFindingsSnapshotAsync_CallsEachEngineOnce()
    {
        GraphSnapshot graph = EmptyGraph();
        Mock<IFindingEngine> e1 = CreateEngine("e1", "Security", []);
        Mock<IFindingEngine> e2 = CreateEngine("e2", "Topology", []);

        Mock<IFindingPayloadValidator> validator = new();
        validator.Setup(v => v.Validate(It.IsAny<Finding>()));

        FindingsOrchestrator sut = new([e1.Object, e2.Object], validator.Object, NullLogger<FindingsOrchestrator>.Instance);

        await sut.GenerateFindingsSnapshotAsync(Guid.NewGuid(), Guid.NewGuid(), graph, CancellationToken.None);

        e1.Verify(x => x.AnalyzeAsync(graph, It.IsAny<CancellationToken>()), Times.Once);
        e2.Verify(x => x.AnalyzeAsync(graph, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateFindingsSnapshotAsync_SingleEngineThrow_throws_AggregateException()
    {
        GraphSnapshot graph = EmptyGraph();
        Mock<IFindingEngine> e1 = new(MockBehavior.Strict);
        e1.Setup(x => x.EngineType).Returns("bad");
        e1.Setup(x => x.Category).Returns("Security");
        e1.Setup(x => x.AnalyzeAsync(graph, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Mock<IFindingPayloadValidator> validator = new(MockBehavior.Strict);
        FindingsOrchestrator sut = new([e1.Object], validator.Object, NullLogger<FindingsOrchestrator>.Instance);

        AggregateException ax = await Assert.ThrowsAsync<AggregateException>(
            () => sut.GenerateFindingsSnapshotAsync(Guid.NewGuid(), Guid.NewGuid(), graph, CancellationToken.None));

        ax.InnerExceptions.Should().ContainSingle()
            .Which.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateFindingsSnapshotAsync_PartialEngineFailure_still_returns_snapshot()
    {
        GraphSnapshot graph = EmptyGraph();
        Finding ok = new()
        {
            FindingType = "T",
            Category = "Security",
            EngineType = "ok",
            Title = "ok-title",
            Rationale = "r",
            Severity = FindingSeverity.Info
        };

        Mock<IFindingEngine> bad = new(MockBehavior.Strict);
        bad.Setup(x => x.EngineType).Returns("bad");
        bad.Setup(x => x.Category).Returns("Security");
        bad.Setup(x => x.AnalyzeAsync(graph, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Mock<IFindingEngine> good = CreateEngine("good", "Security", [ok]);

        Mock<IFindingPayloadValidator> validator = new();
        validator.Setup(v => v.Validate(It.IsAny<Finding>()));

        FindingsOrchestrator sut = new([bad.Object, good.Object], validator.Object, NullLogger<FindingsOrchestrator>.Instance);

        FindingsSnapshot snapshot = await sut.GenerateFindingsSnapshotAsync(Guid.NewGuid(), Guid.NewGuid(), graph, CancellationToken.None);

        snapshot.Findings.Should().ContainSingle();
        snapshot.EngineFailures.Should().ContainSingle()
            .Which.EngineType.Should().Be("bad");
    }

    [Fact]
    public async Task GenerateFindingsSnapshotAsync_OperationCanceledException_propagates()
    {
        GraphSnapshot graph = EmptyGraph();
        Mock<IFindingEngine> e1 = new(MockBehavior.Strict);
        e1.Setup(x => x.EngineType).Returns("bad");
        e1.Setup(x => x.Category).Returns("Security");
        e1.Setup(x => x.AnalyzeAsync(graph, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        Mock<IFindingPayloadValidator> validator = new(MockBehavior.Strict);
        FindingsOrchestrator sut = new([e1.Object], validator.Object, NullLogger<FindingsOrchestrator>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.GenerateFindingsSnapshotAsync(Guid.NewGuid(), Guid.NewGuid(), graph, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateFindingsSnapshotAsync_CategoryMismatch_Throws()
    {
        GraphSnapshot graph = EmptyGraph();
        Finding badCategory = new()
        {
            FindingType = "X",
            Category = "Wrong",
            EngineType = "e1",
            Title = "t",
            Rationale = "r",
            Severity = FindingSeverity.Info
        };

        Mock<IFindingEngine> e1 = CreateEngine("e1", "Security", [badCategory]);
        Mock<IFindingPayloadValidator> validator = new();
        validator.Setup(v => v.Validate(It.IsAny<Finding>()));

        FindingsOrchestrator sut = new([e1.Object], validator.Object, NullLogger<FindingsOrchestrator>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GenerateFindingsSnapshotAsync(Guid.NewGuid(), Guid.NewGuid(), graph, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateFindingsSnapshotAsync_DeduplicatesByTypeAndTitle()
    {
        GraphSnapshot graph = EmptyGraph();
        Finding a = new()
        {
            FindingType = "T",
            Category = "Security",
            EngineType = "e1",
            Title = "Same",
            Rationale = "r1",
            Severity = FindingSeverity.Warning
        };
        Finding b = new()
        {
            FindingType = "T",
            Category = "Security",
            EngineType = "e1",
            Title = "Same",
            Rationale = "r2",
            Severity = FindingSeverity.Info
        };

        Mock<IFindingEngine> e1 = CreateEngine("e1", "Security", [a, b]);
        Mock<IFindingPayloadValidator> validator = new();
        validator.Setup(v => v.Validate(It.IsAny<Finding>()));

        FindingsOrchestrator sut = new([e1.Object], validator.Object, NullLogger<FindingsOrchestrator>.Instance);

        FindingsSnapshot snapshot = await sut.GenerateFindingsSnapshotAsync(Guid.NewGuid(), Guid.NewGuid(), graph, CancellationToken.None);

        snapshot.Findings.Should().ContainSingle(f => f.Title == "Same");
    }

    [Fact]
    public async Task GenerateFindingsSnapshotAsync_SetsEmptyCategoryFromEngine()
    {
        GraphSnapshot graph = EmptyGraph();
        Finding f = new()
        {
            FindingType = "T",
            Category = "",
            EngineType = "e1",
            Title = "t",
            Rationale = "r",
            Severity = FindingSeverity.Info
        };

        Mock<IFindingEngine> e1 = CreateEngine("e1", "Requirement", [f]);
        Mock<IFindingPayloadValidator> validator = new();
        validator.Setup(v => v.Validate(It.IsAny<Finding>()));

        FindingsOrchestrator sut = new([e1.Object], validator.Object, NullLogger<FindingsOrchestrator>.Instance);

        FindingsSnapshot snapshot = await sut.GenerateFindingsSnapshotAsync(Guid.NewGuid(), Guid.NewGuid(), graph, CancellationToken.None);

        snapshot.Findings.Single().Category.Should().Be("Requirement");
    }

    [Fact]
    public async Task GenerateFindingsSnapshotAsync_RecordsOccurredUtcFromTimeProviderOnEngineFailure()
    {
        GraphSnapshot graph = EmptyGraph();
        DateTimeOffset fixedUtc = new(2026, 4, 24, 18, 30, 0, TimeSpan.Zero);
        FakeTimeProviderForOrchestrator clock = new(fixedUtc);

        Mock<IFindingEngine> bad = new(MockBehavior.Strict);
        bad.Setup(x => x.EngineType).Returns("fail");
        bad.Setup(x => x.Category).Returns("Security");
        bad.Setup(x => x.AnalyzeAsync(graph, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Mock<IFindingEngine> good = CreateEngine("ok", "Security", []);

        Mock<IFindingPayloadValidator> validator = new();
        FindingsOrchestrator sut = new(
            [bad.Object, good.Object],
            validator.Object,
            NullLogger<FindingsOrchestrator>.Instance,
            clock);

        FindingsSnapshot snapshot = await sut.GenerateFindingsSnapshotAsync(Guid.NewGuid(), Guid.NewGuid(), graph, CancellationToken.None);

        snapshot.EngineFailures.Should().ContainSingle();
        snapshot.EngineFailures[0].OccurredUtc.Should().Be(fixedUtc.UtcDateTime);
    }

    private sealed class FakeTimeProviderForOrchestrator : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProviderForOrchestrator(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    private static Mock<IFindingEngine> CreateEngine(string engineType, string category, IReadOnlyList<Finding> findings)
    {
        Mock<IFindingEngine> mock = new(MockBehavior.Strict);
        mock.Setup(x => x.EngineType).Returns(engineType);
        mock.Setup(x => x.Category).Returns(category);
        mock.Setup(x => x.AnalyzeAsync(It.IsAny<GraphSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(findings);
        return mock;
    }
}
