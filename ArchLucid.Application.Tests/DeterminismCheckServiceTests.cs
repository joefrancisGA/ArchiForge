using ArchLucid.Application.Determinism;
using ArchLucid.Application.Diffs;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests;

/// <summary>
/// <see cref="DeterminismCheckService"/> unit tests (validation, drift aggregation).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class DeterminismCheckServiceTests
{
    [SkippableFact]
    public async Task RunAsync_iterations_below_two_throws()
    {
        Mock<IReplayRunService> replay = new();
        Mock<IAgentResultDiffService> agentDiff = new();
        Mock<IManifestDiffService> manifestDiff = new();

        DeterminismCheckService sut = new(replay.Object, agentDiff.Object, manifestDiff.Object);

        DeterminismCheckRequest request = new() { RunId = "r1", Iterations = 1 };

        Func<Task> act = async () => await sut.RunAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [SkippableFact]
    public async Task RunAsync_when_replays_match_marks_deterministic()
    {
        List<AgentResult> results =
        [
            new()
            {
                RunId = "any",
                TaskId = "t1",
                AgentType = AgentType.Topology,
                Confidence = 0.5,
                ResultId = "res",
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        ReplayRunResult baseline = new()
        {
            OriginalRunId = "src", ReplayRunId = "base", ExecutionMode = ExecutionModes.Current, Results = results,
        };

        ReplayRunResult iteration1 = new()
        {
            OriginalRunId = "src", ReplayRunId = "iter1", ExecutionMode = ExecutionModes.Current, Results = results,
        };

        ReplayRunResult iteration2 = new()
        {
            OriginalRunId = "src", ReplayRunId = "iter2", ExecutionMode = ExecutionModes.Current, Results = results,
        };

        int replayCall = 0;
        Mock<IReplayRunService> replay = new();
        replay.Setup(x => x.ReplayAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                replayCall++;
                ReplayRunResult next = replayCall switch
                {
                    1 => baseline,
                    2 => iteration1,
                    3 => iteration2,
                    _ => throw new InvalidOperationException($"Unexpected ReplayAsync call #{replayCall}."),
                };

                return Task.FromResult(next);
            });

        Mock<IAgentResultDiffService> agentDiff = new();
        agentDiff
            .Setup(x => x.Compare(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<AgentResult>>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<AgentResult>>()))
            .Returns(
                new AgentResultDiffResult { LeftRunId = "base", RightRunId = "iter1", AgentDeltas = [], });

        Mock<IManifestDiffService> manifestDiff = new();

        DeterminismCheckService sut = new(replay.Object, agentDiff.Object, manifestDiff.Object);

        DeterminismCheckResult output = await sut.RunAsync(
            new DeterminismCheckRequest
            {
                RunId = "src", Iterations = 2, ExecutionMode = ExecutionModes.Current, CommitReplays = false,
            },
            CancellationToken.None);

        output.IsDeterministic.Should().BeTrue();
        output.BaselineReplayRunId.Should().Be("base");
        output.IterationResults.Should().HaveCount(2);
        output.IterationResults.Should().OnlyContain(x => x.MatchesBaselineAgentResults && x.MatchesBaselineManifest);
        output.Warnings.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task RunAsync_when_agent_diff_reports_drift_marks_non_deterministic_and_warns()
    {
        List<AgentResult> baselineResults =
        [
            new()
            {
                RunId = "b",
                TaskId = "t1",
                AgentType = AgentType.Topology,
                Confidence = 0.5,
                ResultId = "res",
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        List<AgentResult> driftResults =
        [
            new()
            {
                RunId = "i",
                TaskId = "t1",
                AgentType = AgentType.Topology,
                Confidence = 0.9,
                ResultId = "res",
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        ReplayRunResult baseline = new()
        {
            OriginalRunId = "src", ReplayRunId = "base", ExecutionMode = ExecutionModes.Current, Results = baselineResults,
        };

        ReplayRunResult iteration1 = new()
        {
            OriginalRunId = "src", ReplayRunId = "iter1", ExecutionMode = ExecutionModes.Current, Results = driftResults,
        };

        ReplayRunResult iteration2 = new()
        {
            OriginalRunId = "src", ReplayRunId = "iter2", ExecutionMode = ExecutionModes.Current, Results = driftResults,
        };

        int replayCall = 0;
        Mock<IReplayRunService> replay = new();
        replay.Setup(x => x.ReplayAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                replayCall++;
                ReplayRunResult next = replayCall switch
                {
                    1 => baseline,
                    2 => iteration1,
                    3 => iteration2,
                    _ => throw new InvalidOperationException($"Unexpected ReplayAsync call #{replayCall}."),
                };

                return Task.FromResult(next);
            });

        Mock<IAgentResultDiffService> agentDiff = new();
        agentDiff
            .Setup(x => x.Compare(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<AgentResult>>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<AgentResult>>()))
            .Returns(
                new AgentResultDiffResult
                {
                    AgentDeltas =
                    [
                        new AgentResultDelta { AgentType = AgentType.Topology, AddedClaims = ["new-claim"], },
                    ],
                });

        Mock<IManifestDiffService> manifestDiff = new();

        DeterminismCheckService sut = new(replay.Object, agentDiff.Object, manifestDiff.Object);

        DeterminismCheckResult output = await sut.RunAsync(
            new DeterminismCheckRequest { RunId = "src", Iterations = 2, ExecutionMode = ExecutionModes.Current },
            CancellationToken.None);

        output.IsDeterministic.Should().BeFalse();
        output.Warnings.Should().ContainSingle().Which.Should().Contain("Determinism check detected replay drift.");
        output.IterationResults[0].MatchesBaselineAgentResults.Should().BeFalse();
    }
}
