using ArchLucid.ContextIngestion.Models;

namespace ArchLucid.Persistence.Tests.Orchestration;

/// <summary>Unit coverage for bounded error text persisted on deferred authority pipeline rows.</summary>
[Trait("Suite", "Core")]
public sealed class AuthorityPipelineWorkErrorSummaryTests
{
    [Fact]
    public void From_composes_exception_type_and_truncates_overflow()
    {
        string oversized = new('x', AuthorityPipelineWorkErrorSummary.MaxLength * 3);
        Exception ex = new InvalidOperationException(oversized);
        string result = AuthorityPipelineWorkErrorSummary.From(ex);

        result.Should().HaveLength(AuthorityPipelineWorkErrorSummary.MaxLength);
        result.Should().Contain("InvalidOperationException:");
        result.Should().MatchRegex(@"\.\.\.$");
    }

    [Fact]
    public void From_null_returns_empty()
    {
        AuthorityPipelineWorkErrorSummary.From(null!).Should().BeEmpty();
    }

    [Fact]
    public void TruncateNullable_appends_ellipsis_when_needed()
    {
        string oversized = new('y', AuthorityPipelineWorkErrorSummary.MaxLength + 10);
        string truncated = AuthorityPipelineWorkErrorSummary.TruncateNullable(oversized);

        truncated.Should().HaveLength(AuthorityPipelineWorkErrorSummary.MaxLength);
        truncated.Should().EndWith("...");
    }
}

/// <summary>Deterministic dequeue / lease / dead-letter parity for SQL outbox behavior.</summary>
[Trait("Suite", "Core")]
public sealed class InMemoryAuthorityPipelineWorkRepositoryLeaseAndRetryTests
{
    private static string ValidPayload(Guid runId) =>
        AuthorityPipelineWorkPayloadJson.Serialize(new AuthorityPipelineWorkPayload
        {
            ContextIngestionRequest = new ContextIngestionRequest
            {
                RunId = runId,
                ProjectId = "default",
            },
            EvidenceBundleId = "eb-1",
        });

    [Fact]
    public async Task DequeuePendingAsync_sets_exclusive_lease_blocking_second_worker_scan()
    {
        DateTime utc = new DateTime(2026, 5, 2, 14, 0, 0, DateTimeKind.Utc);
        InMemoryAuthorityPipelineWorkRepository sut = new(() => utc);
        Guid runId = Guid.NewGuid();
        await sut.EnqueueAsync(runId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidPayload(runId));

        IReadOnlyList<AuthorityPipelineWorkOutboxEntry> first =
            await sut.DequeuePendingAsync(10, 120, CancellationToken.None);

        first.Should().ContainSingle(e => e.RunId == runId);
        first[0].LockedUntilUtc.Should().Be(utc.AddSeconds(120));

        IReadOnlyList<AuthorityPipelineWorkOutboxEntry> second =
            await sut.DequeuePendingAsync(10, 120, CancellationToken.None);

        second.Should().BeEmpty();

        utc = utc.AddSeconds(121);

        IReadOnlyList<AuthorityPipelineWorkOutboxEntry> third =
            await sut.DequeuePendingAsync(10, 120, CancellationToken.None);

        third.Should().ContainSingle(e => e.RunId == runId);
        third[0].AttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task RecordDeadLetter_increments_dead_counters_and_blocks_dequeue()
    {
        DateTime utc = new DateTime(2026, 5, 2, 15, 0, 0, DateTimeKind.Utc);
        InMemoryAuthorityPipelineWorkRepository sut = new(() => utc);
        Guid runId = Guid.NewGuid();
        await sut.EnqueueAsync(runId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidPayload(runId));
        AuthorityPipelineWorkOutboxEntry claimed =
            (await sut.DequeuePendingAsync(5, 60, CancellationToken.None)).Should().ContainSingle().Subject;

        await sut.RecordDeadLetterAsync(claimed.OutboxId, "poison", CancellationToken.None);

        (await sut.CountDeadLetteredAsync()).Should().Be(1);
        (await sut.CountPendingAsync()).Should().Be(0);
        (await sut.CountActionablePendingAsync()).Should().Be(0);
        (await sut.DequeuePendingAsync(5, 60, CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task RecordBackoffAfterProcessingFailure_respects_next_attempt_and_releases_claim()
    {
        DateTime utc = new DateTime(2026, 5, 2, 16, 0, 0, DateTimeKind.Utc);
        InMemoryAuthorityPipelineWorkRepository sut = new(() => utc);
        Guid runId = Guid.NewGuid();
        await sut.EnqueueAsync(runId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidPayload(runId));

        AuthorityPipelineWorkOutboxEntry claimed =
            (await sut.DequeuePendingAsync(3, 300, CancellationToken.None)).Should().ContainSingle().Subject;

        DateTime resumeAt = utc.AddMinutes(30);
        await sut.RecordBackoffAfterProcessingFailureAsync(claimed.OutboxId, resumeAt, "transient", CancellationToken.None);

        (await sut.CountActionablePendingAsync()).Should().Be(0);

        utc = resumeAt.AddSeconds(1);

        (await sut.CountActionablePendingAsync()).Should().Be(1);
    }
}
