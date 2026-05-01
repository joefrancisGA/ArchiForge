using System.Diagnostics;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Integration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Persistence.Tests;

[Trait("Suite", "Core")]
public sealed class IntegrationEventOutboxProcessorCorrelationTests
{
    [SkippableFact]
    public async Task ProcessPendingBatchAsync_starts_activity_with_run_correlation_when_run_id_present()
    {
        List<Activity> stopped = [];
        using ActivityListener listener = new();
        listener.ShouldListenTo = s => s.Name == "ArchLucid.IntegrationEvent.Outbox";
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = a => stopped.Add(a);
        ActivitySource.AddActivityListener(listener);

        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Guid outboxId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        Mock<IIntegrationEventOutboxRepository> outbox = new();
        outbox
            .Setup(o => o.DequeuePendingAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new IntegrationEventOutboxEntry
                {
                    OutboxId = outboxId,
                    RunId = runId,
                    EventType = "t",
                    MessageId = null,
                    PayloadUtf8 = [1],
                    TenantId = Guid.NewGuid(),
                    WorkspaceId = Guid.NewGuid(),
                    ProjectId = Guid.NewGuid(),
                    CreatedUtc = DateTime.UtcNow,
                    RetryCount = 0
                }
            ]);

        IntegrationEventOutboxProcessor sut = CreateProcessor(outbox.Object, publisher.Object);
        await sut.ProcessPendingBatchAsync(CancellationToken.None);

        string outboxIdTag = outboxId.ToString("D");
        List<Activity> entryActivities =
            stopped
                .Where(a =>
                    a.OperationName == "IntegrationEventOutbox.ProcessEntry"
                    && string.Equals(a.GetTagItem("archlucid.outbox_id") as string, outboxIdTag,
                        StringComparison.Ordinal))
                .ToList();

        entryActivities.Should().ContainSingle();
        Activity entryActivity = entryActivities[0];
        entryActivity.GetTagItem(ActivityCorrelation.LogicalCorrelationIdTag).Should().Be($"run:{runId:D}");
        entryActivity.GetTagItem("archlucid.outbox_id").Should().Be(outboxId.ToString("D"));
        entryActivity.GetTagItem("archlucid.run_id").Should().Be(runId.ToString("D"));
        entryActivity.GetTagItem("archlucid.event_type").Should().Be("t");
    }

    [SkippableFact]
    public async Task ProcessPendingBatchAsync_starts_activity_with_outbox_correlation_when_run_id_absent()
    {
        List<Activity> stopped = [];
        using ActivityListener listener = new();
        listener.ShouldListenTo = s => s.Name == "ArchLucid.IntegrationEvent.Outbox";
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = a => stopped.Add(a);
        ActivitySource.AddActivityListener(listener);

        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Guid outboxId = Guid.NewGuid();
        Mock<IIntegrationEventOutboxRepository> outbox = new();
        outbox
            .Setup(o => o.DequeuePendingAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new IntegrationEventOutboxEntry
                {
                    OutboxId = outboxId,
                    RunId = null,
                    EventType = "t2",
                    MessageId = null,
                    PayloadUtf8 = [2],
                    TenantId = Guid.NewGuid(),
                    WorkspaceId = Guid.NewGuid(),
                    ProjectId = Guid.NewGuid(),
                    CreatedUtc = DateTime.UtcNow,
                    RetryCount = 0
                }
            ]);

        IntegrationEventOutboxProcessor sut = CreateProcessor(outbox.Object, publisher.Object);
        await sut.ProcessPendingBatchAsync(CancellationToken.None);

        string outboxIdTag = outboxId.ToString("D");
        List<Activity> entryActivities =
            stopped
                .Where(a =>
                    a.OperationName == "IntegrationEventOutbox.ProcessEntry"
                    && string.Equals(a.GetTagItem("archlucid.outbox_id") as string, outboxIdTag,
                        StringComparison.Ordinal))
                .ToList();

        entryActivities.Should().ContainSingle();
        Activity entryActivity = entryActivities[0];
        entryActivity.GetTagItem(ActivityCorrelation.LogicalCorrelationIdTag).Should()
            .Be($"integration-outbox:{outboxId:D}");
        entryActivity.GetTagItem("archlucid.outbox_id").Should().Be(outboxId.ToString("D"));
        entryActivity.GetTagItem("archlucid.run_id").Should().BeNull();
    }

    private static IntegrationEventOutboxProcessor CreateProcessor(
        IIntegrationEventOutboxRepository outbox,
        IIntegrationEventPublisher publisher)
    {
        ServiceCollection services = [];
        services.AddScoped(_ => outbox);
        services.AddScoped(_ => publisher);
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory factory = provider.GetRequiredService<IServiceScopeFactory>();
        IOptions<IntegrationEventsOptions> opt =
            Microsoft.Extensions.Options.Options.Create(new IntegrationEventsOptions());

        return new IntegrationEventOutboxProcessor(factory, opt, NullLogger<IntegrationEventOutboxProcessor>.Instance);
    }
}
