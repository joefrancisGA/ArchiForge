using ArchLucid.Application.Analysis;
using ArchLucid.Application.Evolution;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Evolution;

[Trait("Category", "Unit")]
public sealed class ShadowExecutionServiceTests
{
    [SkippableFact]
    public async Task ExecuteAsync_missing_run_throws_RunNotFoundException()
    {
        Mock<IRunDetailQueryService> detail = new();
        detail.Setup(x => x.GetRunDetailAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        ShadowExecutionService sut = new(detail.Object, Mock.Of<IArchitectureAnalysisService>());

        ShadowExecutionRequest request = new() { BaselineArchitectureRunId = "missing", CandidateChangeSet = MinimalCandidate(), };

        Func<Task> act = async () => await sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<RunNotFoundException>();
    }

    [SkippableFact]
    public async Task ExecuteAsync_clones_detail_applies_steps_and_does_not_mutate_source_aggregate()
    {
        ArchitectureRunDetail source = new()
        {
            Run = new ArchitectureRun { RunId = "run1", CurrentManifestVersion = "v1" },
            Manifest = new GoldenManifest
            {
                RunId = "run1", SystemName = "Sys", Metadata = new ManifestMetadata { ChangeDescription = "original", ManifestVersion = "v1" },
            },
            DecisionTraces =
            [
                RunEventTrace.From(new RunEventTracePayload
                {
                    TraceId = "t0", RunId = "run1", EventType = "Real", EventDescription = "committed",
                }),
            ],
        };

        Mock<IRunDetailQueryService> detail = new();
        detail.Setup(x => x.GetRunDetailAsync("run1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        ArchitectureAnalysisRequest? captured = null;

        Mock<IArchitectureAnalysisService> analysis = new();
        analysis
            .Setup(x => x.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ArchitectureAnalysisRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new ArchitectureAnalysisReport { Run = new ArchitectureRun { RunId = "run1" }, Warnings = [] });

        ShadowExecutionService sut = new(detail.Object, analysis.Object);

        CandidateChangeSet candidate = new()
        {
            ChangeSetId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            SourcePlanId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Description = "shadow",
            CreatedUtc = DateTime.UtcNow,
            ApprovalStatus = ApprovalStatus.PendingReview,
            ProposedActions =
            [
                new CandidateChangeSetStep { Ordinal = 1, ActionType = "Tighten", Description = "add control" },
            ],
        };

        ShadowExecutionRequest request = new() { BaselineArchitectureRunId = "run1", CandidateChangeSet = candidate, };

        _ = await sut.ExecuteAsync(request, CancellationToken.None);

        source.DecisionTraces.Should().HaveCount(1);
        source.Manifest!.Metadata.ChangeDescription.Should().Be("original");

        captured.Should().NotBeNull();
        captured!.IncludeEvidence.Should().BeFalse();
        captured.IncludeDeterminismCheck.Should().BeFalse();
        captured.IncludeManifestCompare.Should().BeFalse();
        captured.IncludeAgentResultCompare.Should().BeFalse();
        captured.PreloadedRunDetail.Should().NotBeNull();
        captured.PreloadedRunDetail!.DecisionTraces.Should().HaveCount(2);

        captured.PreloadedRunDetail.DecisionTraces[^1].RequireRunEvent().EventType.Should().Be("Shadow.CandidateStep");
        captured.PreloadedRunDetail.Manifest!.Metadata.ChangeDescription.Should().Contain("[60R shadow]");
        captured.PreloadedRunDetail.Manifest.Metadata.ChangeDescription.Should().Contain("original");
    }

    [SkippableFact]
    public Task ArchitectureRunDetailIsolatingCloner_produces_detached_copy()
    {
        try
        {
            ArchitectureRunDetail source = new()
            {
                Run = new ArchitectureRun { RunId = "x" },
                Tasks =
                [
                    new AgentTask
                    {
                        RunId = "x", TaskId = "tk", AgentType = AgentType.Topology, Objective = "o",
                    },
                ],
            };

            ArchitectureRunDetail copy = ArchitectureRunDetailIsolatingCloner.Clone(source);

            copy.Run.RunId.Should().Be("x");
            copy.Tasks[0].Objective = "mutated";

            source.Tasks[0].Objective.Should().Be("o");
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    private static CandidateChangeSet MinimalCandidate()
    {
        return new CandidateChangeSet
        {
            ChangeSetId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            SourcePlanId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Description = "shadow",
            CreatedUtc = DateTime.UtcNow,
            ApprovalStatus = ApprovalStatus.PendingReview,
        };
    }
}
