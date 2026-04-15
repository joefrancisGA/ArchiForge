using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
/// Verifies <see cref="SqlRunRepository"/> bulk / by-id archival cascades <c>ArchivedUtc</c> to
/// <c>dbo.ContextSnapshots</c>, <c>dbo.GraphSnapshots</c>, and <c>dbo.DecisioningTraces</c> (migration 067 + repository batch SQL).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Persistence")]
public sealed class SqlRunRepositoryArchivalCascadeTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task ArchiveRunsCreatedBeforeAsync_cascades_ArchivedUtc_to_context_graph_decisioning_and_findings()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        await using SqlConnection probe = new(fixture.ConnectionString);
        await probe.OpenAsync(CancellationToken.None);

        Skip.IfNot(
            await RunArchiveCascadeArchivedUtcColumnsExistAsync(probe, CancellationToken.None),
            "dbo.FindingsSnapshots.ArchivedUtc (066) and context/graph/decisioning ArchivedUtc (067) are required.");

        TestSqlConnectionFactory sqlFactory = new(fixture.ConnectionString);
        TestAuthorityRunListConnectionFactory listFactory = new(sqlFactory);
        SqlRunRepository repo = new(sqlFactory, listFactory);

        ScopeContext scope = NewScope();
        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        string slug = "arch_cascade_" + Guid.NewGuid().ToString("N");

        RunRecord run = NewRun(scope, runId, slug, DateTime.UtcNow.AddDays(-10));
        await repo.SaveAsync(run, CancellationToken.None);

        await using SqlConnection seed = new(fixture.ConnectionString);
        await seed.OpenAsync(CancellationToken.None);

        await AuthorityRunChainTestSeed.SeedSnapshotChainForExistingRunAsync(
            seed,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            slug,
            CancellationToken.None);

        RunArchiveBatchResult batch =
            await repo.ArchiveRunsCreatedBeforeAsync(DateTimeOffset.UtcNow.AddDays(-1), CancellationToken.None);

        batch.UpdatedCount.Should().BeGreaterThanOrEqualTo(1);

        DateTime? contextArchived = await ReadArchivedUtcAsync(seed, "dbo.ContextSnapshots", "SnapshotId", contextId, CancellationToken.None);
        DateTime? graphArchived = await ReadArchivedUtcAsync(seed, "dbo.GraphSnapshots", "GraphSnapshotId", graphId, CancellationToken.None);
        DateTime? traceArchived = await ReadArchivedUtcAsync(seed, "dbo.DecisioningTraces", "DecisionTraceId", traceId, CancellationToken.None);
        DateTime? findingsArchived = await ReadArchivedUtcAsync(seed, "dbo.FindingsSnapshots", "FindingsSnapshotId", findingsId, CancellationToken.None);

        contextArchived.Should().NotBeNull("ContextSnapshots.ArchivedUtc should be set in the same batch as dbo.Runs.");
        graphArchived.Should().NotBeNull("GraphSnapshots.ArchivedUtc should be set in the same batch as dbo.Runs.");
        traceArchived.Should().NotBeNull("DecisioningTraces.ArchivedUtc should be set in the same batch as dbo.Runs.");
        findingsArchived.Should().NotBeNull("FindingsSnapshots.ArchivedUtc should be set when column exists (migration 066).");
    }

    [SkippableFact]
    public async Task ArchiveRunsByIdsAsync_cascades_ArchivedUtc_to_context_graph_decisioning_and_findings()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        await using SqlConnection probe = new(fixture.ConnectionString);
        await probe.OpenAsync(CancellationToken.None);

        Skip.IfNot(
            await RunArchiveCascadeArchivedUtcColumnsExistAsync(probe, CancellationToken.None),
            "dbo.FindingsSnapshots.ArchivedUtc (066) and context/graph/decisioning ArchivedUtc (067) are required.");

        TestSqlConnectionFactory sqlFactory = new(fixture.ConnectionString);
        TestAuthorityRunListConnectionFactory listFactory = new(sqlFactory);
        SqlRunRepository repo = new(sqlFactory, listFactory);

        ScopeContext scope = NewScope();
        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        string slug = "arch_byids_" + Guid.NewGuid().ToString("N");

        RunRecord run = NewRun(scope, runId, slug, DateTime.UtcNow);
        await repo.SaveAsync(run, CancellationToken.None);

        await using SqlConnection seed = new(fixture.ConnectionString);
        await seed.OpenAsync(CancellationToken.None);

        await AuthorityRunChainTestSeed.SeedSnapshotChainForExistingRunAsync(
            seed,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            slug,
            CancellationToken.None);

        RunArchiveByIdsResult result = await repo.ArchiveRunsByIdsAsync([runId], CancellationToken.None);

        result.SucceededRunIds.Should().ContainSingle().Which.Should().Be(runId);

        DateTime? contextArchived = await ReadArchivedUtcAsync(seed, "dbo.ContextSnapshots", "SnapshotId", contextId, CancellationToken.None);
        DateTime? graphArchived = await ReadArchivedUtcAsync(seed, "dbo.GraphSnapshots", "GraphSnapshotId", graphId, CancellationToken.None);
        DateTime? traceArchived = await ReadArchivedUtcAsync(seed, "dbo.DecisioningTraces", "DecisionTraceId", traceId, CancellationToken.None);
        DateTime? findingsArchived = await ReadArchivedUtcAsync(seed, "dbo.FindingsSnapshots", "FindingsSnapshotId", findingsId, CancellationToken.None);

        contextArchived.Should().NotBeNull();
        graphArchived.Should().NotBeNull();
        traceArchived.Should().NotBeNull();
        findingsArchived.Should().NotBeNull();
    }

    private static ScopeContext NewScope() =>
        new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

    private static RunRecord NewRun(ScopeContext scope, Guid runId, string projectSlug, DateTime createdUtc) =>
        new()
        {
            RunId = runId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = projectSlug,
            Description = "archival cascade test",
            CreatedUtc = createdUtc,
        };

    private static async Task<bool> RunArchiveCascadeArchivedUtcColumnsExistAsync(SqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT CASE
                WHEN COL_LENGTH(N'dbo.FindingsSnapshots', N'ArchivedUtc') IS NOT NULL
                 AND COL_LENGTH(N'dbo.ContextSnapshots', N'ArchivedUtc') IS NOT NULL
                 AND COL_LENGTH(N'dbo.GraphSnapshots', N'ArchivedUtc') IS NOT NULL
                 AND COL_LENGTH(N'dbo.DecisioningTraces', N'ArchivedUtc') IS NOT NULL
                THEN 1 ELSE 0 END;
            """;

        int flag = await connection.QuerySingleAsync<int>(new CommandDefinition(sql, cancellationToken: ct));

        return flag == 1;
    }

    private static async Task<DateTime?> ReadArchivedUtcAsync(
        SqlConnection connection,
        string tableSql,
        string idColumn,
        Guid id,
        CancellationToken ct)
    {
        // tableSql is a fixed literal from this test class only (e.g. dbo.ContextSnapshots).
        string sql = $"""
            SELECT ArchivedUtc FROM {tableSql}
            WHERE {idColumn} = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<DateTime?>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
