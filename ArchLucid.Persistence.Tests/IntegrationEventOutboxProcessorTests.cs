using ArchLucid.Core.Integration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Persistence.Tests;

[Trait("Suite", "Core")]
public sealed class IntegrationEventOutboxProcessorTests
{
    [Fact]
    public async Task ProcessPendingBatchAsync_processes_multiple_entries_in_one_batch()
    {
        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventOutboxRepository> outbox = new();
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();

        outbox
            .Setup(o => o.DequeuePendingAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new IntegrationEventOutboxEntry
                {
                    OutboxId = id1,
                    RunId = Guid.NewGuid(),
                    EventType = "t1",
                    MessageId = null,
                    PayloadUtf8 = [1],
                    TenantId = Guid.NewGuid(),
                    WorkspaceId = Guid.NewGuid(),
                    ProjectId = Guid.NewGuid(),
                    CreatedUtc = DateTime.UtcNow,
                    RetryCount = 0
                },
                new IntegrationEventOutboxEntry
                {
                    OutboxId = id2,
                    RunId = Guid.NewGuid(),
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

        outbox.Verify(o => o.MarkProcessedAsync(id1, It.IsAny<CancellationToken>()), Times.Once);
        outbox.Verify(o => o.MarkProcessedAsync(id2, It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(
            p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessPendingBatchAsync_on_success_marks_processed()
    {
        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventOutboxRepository> outbox = new();
        Guid id = Guid.NewGuid();

        outbox
            .Setup(o => o.DequeuePendingAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new IntegrationEventOutboxEntry
                {
                    OutboxId = id,
                    RunId = Guid.NewGuid(),
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

        outbox.Verify(o => o.MarkProcessedAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        outbox.Verify(
            o => o.RecordPublishFailureAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPendingBatchAsync_on_failure_schedules_retry_when_under_cap()
    {
        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sb down"));

        Mock<IIntegrationEventOutboxRepository> outbox = new();
        Guid id = Guid.NewGuid();

        outbox
            .Setup(o => o.DequeuePendingAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new IntegrationEventOutboxEntry
                {
                    OutboxId = id,
                    RunId = Guid.NewGuid(),
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

        IntegrationEventsOptions opts = new()
        {
            OutboxMaxPublishAttempts = 6,
            OutboxMaxBackoffSeconds = 300
        };
        IntegrationEventOutboxProcessor sut = CreateProcessor(outbox.Object, publisher.Object, opts);

        await sut.ProcessPendingBatchAsync(CancellationToken.None);

        outbox.Verify(o => o.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        outbox.Verify(
            o => o.RecordPublishFailureAsync(
                id,
                1,
                It.Is<DateTime?>(n => n.HasValue),
                null,
                // Moq captures this as an expression tree; `is` / `is not` is not allowed (CS8122).
                It.Is<string?>(s => s != null && s.Contains("sb down", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingBatchAsync_on_failure_dead_letters_when_at_cap()
    {
        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fail"));

        Mock<IIntegrationEventOutboxRepository> outbox = new();
        Guid id = Guid.NewGuid();

        outbox
            .Setup(o => o.DequeuePendingAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new IntegrationEventOutboxEntry
                {
                    OutboxId = id,
                    RunId = Guid.NewGuid(),
                    EventType = "t",
                    MessageId = null,
                    PayloadUtf8 = [1],
                    TenantId = Guid.NewGuid(),
                    WorkspaceId = Guid.NewGuid(),
                    ProjectId = Guid.NewGuid(),
                    CreatedUtc = DateTime.UtcNow,
                    RetryCount = 5
                }
            ]);

        IntegrationEventsOptions opts = new()
        {
            OutboxMaxPublishAttempts = 6,
            OutboxMaxBackoffSeconds = 300
        };
        IntegrationEventOutboxProcessor sut = CreateProcessor(outbox.Object, publisher.Object, opts);

        await sut.ProcessPendingBatchAsync(CancellationToken.None);

        outbox.Verify(
            o => o.RecordPublishFailureAsync(
                id,
                6,
                null,
                It.Is<DateTime?>(d => d.HasValue),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingBatchAsync_on_failure_truncates_error_message_to_2048_chars()
    {
        string huge = new('x', 3000);
        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(huge));

        Mock<IIntegrationEventOutboxRepository> outbox = new();
        Guid id = Guid.NewGuid();

        outbox
            .Setup(o => o.DequeuePendingAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new IntegrationEventOutboxEntry
                {
                    OutboxId = id,
                    RunId = null,
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

        IntegrationEventsOptions opts = new()
        {
            OutboxMaxPublishAttempts = 2,
            OutboxMaxBackoffSeconds = 60
        };
        IntegrationEventOutboxProcessor sut = CreateProcessor(outbox.Object, publisher.Object, opts);

        await sut.ProcessPendingBatchAsync(CancellationToken.None);

        outbox.Verify(
            o => o.RecordPublishFailureAsync(
                id,
                1,
                It.IsAny<DateTime?>(),
                null,
                // Moq captures this as an expression tree; `is` / `is not` is not allowed (CS8122).
                It.Is<string?>(s => s != null && s.Length == 2048),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static IntegrationEventOutboxProcessor CreateProcessor(
        IIntegrationEventOutboxRepository outbox,
        IIntegrationEventPublisher publisher,
        IntegrationEventsOptions? options = null)
    {
        ServiceCollection services = [];
        services.AddScoped(_ => outbox);
        services.AddScoped(_ => publisher);
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory factory = provider.GetRequiredService<IServiceScopeFactory>();
        IOptions<IntegrationEventsOptions> opt =
            Microsoft.Extensions.Options.Options.Create(options ?? new IntegrationEventsOptions());

        return new IntegrationEventOutboxProcessor(factory, opt, NullLogger<IntegrationEventOutboxProcessor>.Instance);
    }
}
