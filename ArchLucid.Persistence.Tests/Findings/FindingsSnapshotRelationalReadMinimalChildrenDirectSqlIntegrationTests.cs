using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Findings;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Findings;

/// <summary>
///     Branch coverage for <see cref="FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync" /> when
///     <c>FindingRecords</c> exist but no child rows â€” <c>GetValueOrDefault</c> / empty collection paths.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class FindingsSnapshotRelationalReadMinimalChildrenDirectSqlIntegrationTests(
    SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task LoadRelationalSnapshotAsync_hydrates_relational_finding_with_empty_child_collections()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
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
            "proj-findings-min-children",
            CancellationToken.None);

        const string selectHeader = """
                                    SELECT FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc, SchemaVersion, FindingsJson
                                    FROM dbo.FindingsSnapshots
                                    WHERE FindingsSnapshotId = @FindingsSnapshotId;
                                    """;

        FindingsSnapshotStorageRow? headerRow = await connection.QuerySingleOrDefaultAsync<FindingsSnapshotStorageRow>(
            new CommandDefinition(selectHeader, new { FindingsSnapshotId = findingsId },
                cancellationToken: CancellationToken.None));

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
                    FindingId = "minimal-child-finding",
                    FindingSchemaVersion = 1,
                    FindingType = "MinimalRelational",
                    Category = "Cat",
                    EngineType = "TestEngine",
                    Severity = "Info",
                    Title = "No children",
                    Rationale = "R",
                    PayloadType = (string?)null,
                    PayloadJson = (string?)null
                },
                cancellationToken: CancellationToken.None));

        FindingsSnapshot loaded =
            await FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync(connection, headerRow,
                CancellationToken.None);

        loaded.Findings.Should().ContainSingle();
        Finding f = loaded.Findings[0];
        f.FindingId.Should().Be("minimal-child-finding");
        f.Title.Should().Be("No children");
        f.Severity.Should().Be(FindingSeverity.Info);
        f.RelatedNodeIds.Should().BeEmpty();
        f.RecommendedActions.Should().BeEmpty();
        f.Properties.Should().BeEmpty();
        f.Trace.GraphNodeIdsExamined.Should().BeEmpty();
        f.Trace.RulesApplied.Should().BeEmpty();
        f.Trace.DecisionsTaken.Should().BeEmpty();
        f.Trace.AlternativePathsConsidered.Should().BeEmpty();
        f.Trace.Notes.Should().BeEmpty();
    }
}
