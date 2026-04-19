using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Services;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Property checks for <see cref="FindingsOrchestrator"/> merge/dedup invariants on synthetic engines.
/// </summary>
[Trait("Suite", "Core")]
public sealed class FindingsOrchestratorPropertyTests
{
#pragma warning disable xUnit1031 // FsCheck properties are synchronous; orchestrator API is async.

    [Property(MaxTest = 40)]
    public void MultipleEnginesReturningEmpty_findings_snapshot_has_zero_rows(int rawCount)
    {
        int engineCount = (Math.Abs(rawCount) % 6) + 1;
        GraphSnapshot graph = EmptyGraph();
        Mock<IFindingPayloadValidator> validator = new();

        List<Mock<IFindingEngine>> engines = [];


        for (int i = 0; i < engineCount; i++)
        {
            Mock<IFindingEngine> engine = new(MockBehavior.Strict);
            string label = $"e{i}";
            engine.Setup(x => x.EngineType).Returns(label);
            engine.Setup(x => x.Category).Returns("Security");
            engine.Setup(x => x.AnalyzeAsync(graph, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Finding>());
            engines.Add(engine);
        }

        IEnumerable<IFindingEngine> engineObjects = engines.Select(static m => m.Object);
        FindingsOrchestrator sut = new(engineObjects, validator.Object, NullLogger<FindingsOrchestrator>.Instance);

        FindingsSnapshot snapshot = sut.GenerateFindingsSnapshotAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                graph,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        snapshot.Findings.Should().BeEmpty();

        foreach (Mock<IFindingEngine> engine in engines)
        {
            engine.Verify(x => x.AnalyzeAsync(graph, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    private static GraphSnapshot EmptyGraph()
    {
        return new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
        };
    }

#pragma warning restore xUnit1031
}
