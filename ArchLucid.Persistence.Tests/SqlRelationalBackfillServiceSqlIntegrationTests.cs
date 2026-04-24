using ArchLucid.ContextIngestion.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Coordination.Backfill;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Serialization;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

using static ArchLucid.Persistence.Tests.Support.PersistenceIntegrationTestScope;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="SqlRelationalBackfillService" /> against SQL Server + DbUp.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlRelationalBackfillServiceSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [SkippableFact]
    public async Task RunAsync_populates_context_relational_from_json_and_second_run_is_noop()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid snapshotId = Guid.NewGuid();

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.Runs (RunId, ProjectId, CreatedUtc, TenantId, WorkspaceId, ScopeProjectId)
                VALUES (@RunId, @ProjectId, @CreatedUtc, @TenantId, @WorkspaceId, @ScopeProjectId);
                """,
                new
                {
                    RunId = runId,
                    ProjectId = "proj-bf",
                    CreatedUtc = DateTime.UtcNow,
                    TenantId,
                    WorkspaceId,
                    ScopeProjectId = ProjectId
                },
                cancellationToken: CancellationToken.None));

        List<CanonicalObject> objects =
        [
            new()
            {
                ObjectId = "o1",
                ObjectType = "Service",
                Name = "api",
                SourceType = "repo",
                SourceId = "src",
                Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["k"] = "v" }
            }
        ];

        string canonicalJson = JsonEntitySerializer.Serialize(objects);
        string emptyList = JsonEntitySerializer.Serialize(new List<string>());
        string emptyDict = JsonEntitySerializer.Serialize(new Dictionary<string, string>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.ContextSnapshots
                (
                    SnapshotId, RunId, ProjectId, CreatedUtc,
                    CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
                )
                VALUES
                (
                    @SnapshotId, @RunId, @ProjectId, @CreatedUtc,
                    @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson
                );
                """,
                new
                {
                    SnapshotId = snapshotId,
                    RunId = runId,
                    ProjectId = "proj-bf",
                    CreatedUtc = DateTime.UtcNow,
                    CanonicalObjectsJson = canonicalJson,
                    DeltaSummary = (string?)null,
                    WarningsJson = emptyList,
                    ErrorsJson = emptyList,
                    SourceHashesJson = emptyDict
                },
                cancellationToken: CancellationToken.None));

        int before = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM dbo.ContextSnapshotCanonicalObjects WHERE SnapshotId = @SnapshotId;",
                new { SnapshotId = snapshotId },
                cancellationToken: CancellationToken.None));

        before.Should().Be(0);

        SqlRelationalBackfillService backfill = CreateService(factory);

        SqlRelationalBackfillReport report1 = await backfill.RunAsync(
            new SqlRelationalBackfillOptions
            {
                ContextSnapshots = true,
                GraphSnapshots = false,
                FindingsSnapshots = false,
                GoldenManifestsPhase1 = false,
                ArtifactBundles = false
            },
            CancellationToken.None);

        report1.FailureCount.Should().Be(0);

        int afterFirst = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM dbo.ContextSnapshotCanonicalObjects WHERE SnapshotId = @SnapshotId;",
                new { SnapshotId = snapshotId },
                cancellationToken: CancellationToken.None));

        afterFirst.Should().BeGreaterThan(0);

        SqlRelationalBackfillReport report2 = await backfill.RunAsync(
            new SqlRelationalBackfillOptions
            {
                ContextSnapshots = true,
                GraphSnapshots = false,
                FindingsSnapshots = false,
                GoldenManifestsPhase1 = false,
                ArtifactBundles = false
            },
            CancellationToken.None);

        report2.FailureCount.Should().Be(0);

        int afterSecond = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM dbo.ContextSnapshotCanonicalObjects WHERE SnapshotId = @SnapshotId;",
                new { SnapshotId = snapshotId },
                cancellationToken: CancellationToken.None));

        afterSecond.Should().Be(afterFirst);
    }

    private static SqlRelationalBackfillService CreateService(SqlConnectionFactory factory)
    {
        return new SqlRelationalBackfillService(
            factory,
            new SqlContextSnapshotRepository(factory, Empty),
            new SqlGraphSnapshotRepository(factory),
            new SqlFindingsSnapshotRepository(factory, Empty),
            SqlPersistenceRepositoryFactory.CreateGoldenManifestRepository(factory),
            SqlPersistenceRepositoryFactory.CreateArtifactBundleRepository(factory),
            NullLogger<SqlRelationalBackfillService>.Instance);
    }
}
