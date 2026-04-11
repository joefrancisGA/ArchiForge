using System.Diagnostics;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Archival;
using ArchLucid.Persistence.Conversation;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Persistence.Tests;

[Collection(nameof(DataArchivalCoordinatorCollection))]
[Trait("Suite", "Core")]
public sealed class DataArchivalCoordinatorCorrelationTests
{
    [Fact]
    public async Task RunOnceAsync_starts_activity_with_correlation_tag()
    {
        List<Activity> stopped = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = s => s.Name == "ArchLucid.DataArchival",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => stopped.Add(a)
        };
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
        archivalActivity.GetTagItem(ActivityCorrelation.LogicalCorrelationIdTag).Should().BeOfType<string>().Which.Should().StartWith("data-archival:");
    }
}
