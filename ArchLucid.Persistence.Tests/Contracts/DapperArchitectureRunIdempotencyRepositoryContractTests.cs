using ArchLucid.Persistence.Data.Repositories;

using ArchLucid.Persistence.Tests.Support;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperArchitectureRunIdempotencyRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : ArchitectureRunIdempotencyRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IArchitectureRunIdempotencyRepository CreateRepository()
    {
        return new ArchitectureRunIdempotencyRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareRunRowForIdempotencyAsync(string runId, CancellationToken ct)
    {
        string requestId = "idem-req-" + runId;
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await ArchitectureCommitTestSeed.InsertRequestAndRunAsync(connection, requestId, runId, ct);
    }

    /// <summary>
    /// Concurrent callers racing on the same idempotency key: SQL unique constraint / first-wins semantics
    /// must leave exactly one row (same as <see cref="ArchitectureRunCreateOrchestrator"/> persistence race).
    /// </summary>
    [SkippableFact]
    [Trait("Suite", "SqlServer")]
    public async Task TryInsert_parallel_same_key_only_one_wins()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        IArchitectureRunIdempotencyRepository repo = CreateRepository();
        byte[] keyHash = [0xC0, 0xDE, 0xFA, 0x11, 0xD0, 0x00];
        byte[] fingerprint = [0x01, 0x02];

        Guid tenantId = Guid.Parse("10101010-1010-1010-1010-101010101010");
        Guid workspaceId = Guid.Parse("20202020-2020-2020-2020-202020202020");
        Guid projectId = Guid.Parse("30303030-3030-3030-3030-303030303030");

        const int parallel = 8;
        string[] runIds = new string[parallel];

        for (int i = 0; i < parallel; i++)
        {
            runIds[i] = Guid.NewGuid().ToString("N");
            await PrepareRunRowForIdempotencyAsync(runIds[i], CancellationToken.None);
        }

        Task<bool>[] tasks = new Task<bool>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            string rid = runIds[i];
            tasks[i] = repo.TryInsertAsync(tenantId, workspaceId, projectId, keyHash, fingerprint, rid, CancellationToken.None);
        }

        bool[] outcomes = await Task.WhenAll(tasks);

        outcomes.Count(static x => x).Should().Be(1);
        outcomes.Count(static x => !x).Should().Be(parallel - 1);

        ArchitectureRunIdempotencyLookup? winner = await repo.TryGetAsync(
            tenantId,
            workspaceId,
            projectId,
            keyHash,
            CancellationToken.None);

        winner.Should().NotBeNull();
        runIds.Should().Contain(winner!.RunId);
    }
}
