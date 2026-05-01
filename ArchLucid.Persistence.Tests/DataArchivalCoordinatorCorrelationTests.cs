using System.Diagnostics;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Conversation;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Persistence.Tests;

[Collection(nameof(DataArchivalCoordinatorCollection))]
[Trait("Suite", "Core")]
public sealed class DataArchivalCoordinatorCorrelationTests
{
    [SkippableFact]
    public async Task RunOnceAsync_starts_activity_with_correlation_tag()
    {
        List<Activity> stopped = [];
        using ActivityListener listener = new();
        listener.ShouldListenTo = s => s.Name == "ArchLucid.DataArchival";
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = a => stopped.Add(a);
        ActivitySource.AddActivityListener(listener);

        Mock<IRunRepository> runs = new();
        runs
            .Setup(r => r.ArchiveRunsCreatedBeforeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunArchiveBatchResult { UpdatedCount = 0, ArchivedRuns = [] });
        Mock<IArchitectureDigestRepository> digests = new();
        Mock<IConversationThreadRepository> threads = new();

        DataArchivalCoordinator sut = new(
            runs.Object,
            digests.Object,
            threads.Object,
            NullLogger<DataArchivalCoordinator>.Instance);

        DataArchivalOptions options = new()
        {
            RunsRetentionDays = 1
        };

        await sut.RunOnceAsync(options, CancellationToken.None);

        stopped.Should().ContainSingle(a => a.OperationName == "DataArchival.RunOnce");
        Activity archivalActivity = stopped.Single(a => a.OperationName == "DataArchival.RunOnce");
        archivalActivity.GetTagItem(ActivityCorrelation.LogicalCorrelationIdTag).Should().BeOfType<string>().Which
            .Should().StartWith("data-archival:");
    }

    [SkippableFact]
    public async Task RunOnceAsync_when_runs_archived_logs_child_cascade_counts()
    {
        Mock<IRunRepository> runs = new();
        runs
            .Setup(r => r.ArchiveRunsCreatedBeforeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RunArchiveBatchResult
                {
                    UpdatedCount = 1,
                    ArchivedRuns =
                    [
                        new ArchivedRunScopeRow
                        {
                            RunId = Guid.NewGuid(),
                            TenantId = Guid.NewGuid(),
                            WorkspaceId = Guid.NewGuid(),
                            ScopeProjectId = Guid.NewGuid()
                        }
                    ],
                    ChildCascade = new RunArchiveChildCascadeCounts
                    {
                        FindingsSnapshots = 2,
                        GraphSnapshots = 1,
                        GoldenManifests = 0
                    }
                });
        Mock<IArchitectureDigestRepository> digests = new();
        Mock<IConversationThreadRepository> threads = new();
        Mock<ILogger<DataArchivalCoordinator>> logger = new();

        DataArchivalCoordinator sut = new(
            runs.Object,
            digests.Object,
            threads.Object,
            logger.Object);

        await sut.RunOnceAsync(
            new DataArchivalOptions { RunsRetentionDays = 1, DigestsRetentionDays = 0, ConversationsRetentionDays = 0 },
            CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("cascade counts", StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
