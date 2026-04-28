using System.Diagnostics;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Queries;
using ArchLucid.Provenance;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Persistence.Tests;

[Trait("Suite", "Core")]
public sealed class RetrievalIndexingOutboxProcessorCorrelationTests
{
    [Fact]
    public async Task ProcessPendingBatchAsync_starts_activity_with_correlation_tags()
    {
        List<Activity> stopped = [];
        using ActivityListener listener = new();
        listener.ShouldListenTo = s => s.Name == "ArchLucid.RetrievalIndexing.Outbox";
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = a => stopped.Add(a);
        ActivitySource.AddActivityListener(listener);

        Guid outboxId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        Mock<IRetrievalIndexingOutboxRepository> outbox = new();
        outbox
            .Setup(o => o.DequeuePendingAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new RetrievalIndexingOutboxEntry
                {
                    OutboxId = outboxId,
                    RunId = runId,
                    TenantId = Guid.NewGuid(),
                    WorkspaceId = Guid.NewGuid(),
                    ProjectId = Guid.NewGuid(),
                    CreatedUtc = DateTime.UtcNow
                }
            ]);
        outbox.Setup(o => o.MarkProcessedAsync(outboxId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IAuthorityQueryService> query = new();
        query
            .Setup(q => q.GetRunDetailAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunDetailDto?)null);

        ServiceCollection services = [];
        services.AddScoped(_ => outbox.Object);
        services.AddScoped(_ => query.Object);
        services.AddScoped(_ => Mock.Of<IRetrievalRunCompletionIndexer>());
        services.AddScoped(_ => Mock.Of<IProvenanceBuilder>());
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory factory = provider.GetRequiredService<IServiceScopeFactory>();

        RetrievalIndexingOutboxProcessor sut = new(factory, NullLogger<RetrievalIndexingOutboxProcessor>.Instance);
        await sut.ProcessPendingBatchAsync(CancellationToken.None);

        stopped.Should().ContainSingle(a => a.OperationName == "RetrievalIndexingOutbox.ProcessEntry");
        Activity entryActivity = stopped.Single(a => a.OperationName == "RetrievalIndexingOutbox.ProcessEntry");
        entryActivity.GetTagItem(ActivityCorrelation.LogicalCorrelationIdTag).Should()
            .Be($"retrieval-outbox:{outboxId:D}");
        entryActivity.GetTagItem("archlucid.outbox_id").Should().Be(outboxId.ToString("D"));
        entryActivity.GetTagItem("archlucid.run_id").Should().Be(runId.ToString("D"));
    }
}
