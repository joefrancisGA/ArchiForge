using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Findings;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Findings;

/// <summary>
/// Covers <see cref="FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync"/> ordering for
/// <c>dbo.FindingTraceAlternativePaths</c> (<c>ORDER BY FindingRecordId, SortOrder</c> via <c>LoadOrderedPairsAsync</c>).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class FindingsSnapshotRelationalReadOrderedAlternativePathsDirectSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task LoadRelationalSnapshotAsync_returns_alternative_paths_in_SortOrder()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid scopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid findingRecordId = Guid.NewGuid();

        await AuthorityRunChainTestSeed.SeedFullChainAsync(
            connection,
            tenantId,
            workspaceId,
            scopeProjectId,
            runId,
            contextId,
            graphId,
            findingsId,
            Guid.NewGuid(),
            "proj-findings-path-order",
            CancellationToken.None);

        const string selectHeader = """
            SELECT FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc, SchemaVersion, FindingsJson
            FROM dbo.FindingsSnapshots
            WHERE FindingsSnapshotId = @FindingsSnapshotId;
            """;

        FindingsSnapshotStorageRow? headerRow = await connection.QuerySingleOrDefaultAsync<FindingsSnapshotStorageRow>(
            new CommandDefinition(selectHeader, new { FindingsSnapshotId = findingsId }, cancellationToken: CancellationToken.None));

        headerRow.Should().NotBeNull();

        const string insertRecord = """
            INSERT INTO dbo.FindingRecords
            (
                FindingRecordId, FindingsSnapshotId, SortOrder,
                FindingId, FindingSchemaVersion, FindingType, Category, EngineType,
                Severity, Title, Rationale, PayloadType, PayloadJson
            )
            VALUES
            (
                @FindingRecordId, @FindingsSnapshotId, @SortOrder,
                @FindingId, @FindingSchemaVersion, @FindingType, @Category, @EngineType,
                @Severity, @Title, @Rationale, @PayloadType, @PayloadJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRecord,
                new
                {
                    FindingRecordId = findingRecordId,
                    FindingsSnapshotId = findingsId,
                    SortOrder = 0,
                    FindingId = "path-order-finding",
                    FindingSchemaVersion = 1,
                    FindingType = "TraceOrder",
                    Category = "Cat",
                    EngineType = "TestEngine",
                    Severity = "Info",
                    Title = "Paths",
                    Rationale = "R",
                    PayloadType = (string?)null,
                    PayloadJson = (string?)null,
                },
                cancellationToken: CancellationToken.None));

        const string insertPath = """
            INSERT INTO dbo.FindingTraceAlternativePaths (FindingRecordId, SortOrder, PathText)
            VALUES (@FindingRecordId, @SortOrder, @PathText);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertPath,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 1,
                    PathText = "second-path-by-sort",
                },
                cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertPath,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 0,
                    PathText = "first-path-by-sort",
                },
                cancellationToken: CancellationToken.None));

        FindingsSnapshot loaded =
            await FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync(connection, headerRow!, CancellationToken.None);

        loaded.Findings.Should().ContainSingle();
        Finding f = loaded.Findings[0];
        f.FindingId.Should().Be("path-order-finding");
        f.Trace.AlternativePathsConsidered.Should().Equal("first-path-by-sort", "second-path-by-sort");
    }
}
