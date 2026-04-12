using System.Data;

using ArchLucid.Persistence;

using FluentAssertions;

using Moq;

namespace ArchLucid.Persistence.Tests.Integration;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class InMemoryIntegrationEventOutboxRepositoryTests
{
    [Fact]
    public async Task EnqueueAsync_with_transaction_overload_throws_when_connection_null()
    {
        InMemoryIntegrationEventOutboxRepository sut = new();
        Mock<IDbTransaction> tx = new();

        Func<Task> act = async () => await sut.EnqueueAsync(
            null,
            "t",
            null,
            ReadOnlyMemory<byte>.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            connection: null!,
            transaction: tx.Object,
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task EnqueueAsync_with_transaction_overload_throws_when_transaction_null()
    {
        InMemoryIntegrationEventOutboxRepository sut = new();
        Mock<IDbConnection> cx = new();

        Func<Task> act = async () => await sut.EnqueueAsync(
            null,
            "t",
            null,
            ReadOnlyMemory<byte>.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            cx.Object,
            transaction: null!,
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("transaction");
    }

    [Fact]
    public async Task Full_lifecycle_enqueue_dequeue_mark_processed()
    {
        InMemoryIntegrationEventOutboxRepository sut = new();
        Guid tenant = Guid.NewGuid();
        Guid workspace = Guid.NewGuid();
        Guid project = Guid.NewGuid();
        ReadOnlyMemory<byte> payload = new byte[] { 1, 2, 3 };

        await sut.EnqueueAsync(null, "evt", "mid", payload, tenant, workspace, project, CancellationToken.None);

        IReadOnlyList<IntegrationEventOutboxEntry> batch =
            await sut.DequeuePendingAsync(10, CancellationToken.None);

        batch.Should().ContainSingle();
        IntegrationEventOutboxEntry e = batch[0];
        e.EventType.Should().Be("evt");
        e.MessageId.Should().Be("mid");
        e.PayloadUtf8.Should().Equal(1, 2, 3);

        await sut.MarkProcessedAsync(e.OutboxId, CancellationToken.None);

        (await sut.DequeuePendingAsync(10, CancellationToken.None)).Should().BeEmpty();
        (await sut.CountIntegrationOutboxPublishPendingAsync(CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task DequeuePendingAsync_respects_next_retry_utc_and_order()
    {
        InMemoryIntegrationEventOutboxRepository sut = new();
        Guid tenant = Guid.NewGuid();
        Guid workspace = Guid.NewGuid();
        Guid project = Guid.NewGuid();

        await sut.EnqueueAsync(null, "a", null, new byte[] { 1 }, tenant, workspace, project, CancellationToken.None);
        IReadOnlyList<IntegrationEventOutboxEntry> first = await sut.DequeuePendingAsync(10, CancellationToken.None);
        Guid idA = first[0].OutboxId;

        await sut.RecordPublishFailureAsync(
            idA,
            newRetryCount: 1,
            nextRetryUtc: DateTime.UtcNow.AddHours(1),
            deadLetteredUtc: null,
            "err",
            CancellationToken.None);

        await sut.EnqueueAsync(null, "b", null, new byte[] { 2 }, tenant, workspace, project, CancellationToken.None);

        IReadOnlyList<IntegrationEventOutboxEntry> pending =
            await sut.DequeuePendingAsync(10, CancellationToken.None);

        pending.Should().ContainSingle();
        pending[0].EventType.Should().Be("b");
    }

    [Fact]
    public async Task Dead_letter_list_count_and_reset()
    {
        InMemoryIntegrationEventOutboxRepository sut = new();
        Guid tenant = Guid.NewGuid();
        Guid workspace = Guid.NewGuid();
        Guid project = Guid.NewGuid();

        await sut.EnqueueAsync(null, "dl", null, new byte[] { 9 }, tenant, workspace, project, CancellationToken.None);
        IReadOnlyList<IntegrationEventOutboxEntry> batch = await sut.DequeuePendingAsync(10, CancellationToken.None);
        Guid id = batch[0].OutboxId;

        await sut.RecordPublishFailureAsync(
            id,
            newRetryCount: 3,
            nextRetryUtc: null,
            deadLetteredUtc: DateTime.UtcNow,
            "boom",
            CancellationToken.None);

        (await sut.CountIntegrationOutboxPublishPendingAsync(CancellationToken.None)).Should().Be(0);
        (await sut.CountIntegrationOutboxDeadLetterAsync(CancellationToken.None)).Should().Be(1);

        IReadOnlyList<IntegrationEventOutboxDeadLetterRow> letters =
            await sut.ListDeadLettersAsync(10, CancellationToken.None);

        letters.Should().ContainSingle();
        letters[0].OutboxId.Should().Be(id);
        letters[0].LastErrorMessage.Should().Be("boom");

        bool reset = await sut.ResetDeadLetterForRetryAsync(id, CancellationToken.None);
        reset.Should().BeTrue();

        (await sut.CountIntegrationOutboxDeadLetterAsync(CancellationToken.None)).Should().Be(0);
        (await sut.DequeuePendingAsync(10, CancellationToken.None)).Should().ContainSingle(x => x.OutboxId == id);

        bool missing = await sut.ResetDeadLetterForRetryAsync(Guid.NewGuid(), CancellationToken.None);
        missing.Should().BeFalse();
    }

    [Fact]
    public async Task RecordPublishFailureAsync_ignores_unknown_outbox_id()
    {
        InMemoryIntegrationEventOutboxRepository sut = new();

        await sut.RecordPublishFailureAsync(
            Guid.NewGuid(),
            1,
            null,
            null,
            "x",
            CancellationToken.None);

        (await sut.CountIntegrationOutboxDeadLetterAsync(CancellationToken.None)).Should().Be(0);
    }
}
