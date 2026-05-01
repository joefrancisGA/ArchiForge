using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IArchitectureRunIdempotencyRepository" />.
/// </summary>
public abstract class ArchitectureRunIdempotencyRepositoryContractTests
{
    private static readonly Guid TenantId = Guid.Parse("10101010-1010-1010-1010-101010101010");
    private static readonly Guid WorkspaceId = Guid.Parse("20202020-2020-2020-2020-202020202020");
    private static readonly Guid ProjectId = Guid.Parse("30303030-3030-3030-3030-303030303030");

    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IArchitectureRunIdempotencyRepository CreateRepository();

    /// <summary>SQL tests seed <c>dbo.Runs</c> for a logical run header; in-memory skips.</summary>
    protected virtual Task PrepareRunRowForIdempotencyAsync(string runId, CancellationToken ct)
    {
        _ = runId;
        _ = ct;

        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task TryInsert_then_TryGet_round_trips()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureRunIdempotencyRepository repo = CreateRepository();
        byte[] keyHash = [1, 2, 3, 4];
        byte[] fingerprint = [5, 6, 7, 8];
        string runId = Guid.NewGuid().ToString("N");

        await PrepareRunRowForIdempotencyAsync(runId, CancellationToken.None);

        bool inserted = await repo.TryInsertAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            keyHash,
            fingerprint,
            runId,
            CancellationToken.None);

        inserted.Should().BeTrue();

        ArchitectureRunIdempotencyLookup? lookup = await repo.TryGetAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            keyHash,
            CancellationToken.None);

        lookup.Should().NotBeNull();
        lookup.RunId.Should().Be(runId);
        lookup.RequestFingerprint.Should().Equal(fingerprint);
    }

    [SkippableFact]
    public async Task TryInsert_twice_same_key_second_fails()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureRunIdempotencyRepository repo = CreateRepository();
        byte[] keyHash = "\t\t\t\t"u8.ToArray();
        byte[] fp1 = [1];
        byte[] fp2 = [2];
        string runA = Guid.NewGuid().ToString("N");
        string runB = Guid.NewGuid().ToString("N");

        await PrepareRunRowForIdempotencyAsync(runA, CancellationToken.None);
        await PrepareRunRowForIdempotencyAsync(runB, CancellationToken.None);

        bool first = await repo.TryInsertAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            keyHash,
            fp1,
            runA,
            CancellationToken.None);
        bool second = await repo.TryInsertAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            keyHash,
            fp2,
            runB,
            CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse();

        ArchitectureRunIdempotencyLookup? winner = await repo.TryGetAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            keyHash,
            CancellationToken.None);

        winner.Should().NotBeNull();
        winner.RunId.Should().Be(runA);
    }
}
