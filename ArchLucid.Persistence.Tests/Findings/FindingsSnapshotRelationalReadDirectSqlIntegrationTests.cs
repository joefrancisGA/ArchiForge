using ArchLucid.ContextIngestion.Models;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Findings;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Findings;

/// <summary>
/// Direct coverage for <see cref="FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync"/> (legacy JSON path
/// when <c>dbo.FindingRecords</c> is empty).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class FindingsSnapshotRelationalReadDirectSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task LoadRelationalSnapshotAsync_when_no_FindingRecords_deserializes_FindingsJson_blob()
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
        DateTime createdUtc = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Utc);

        await AuthorityRunChainTestSeed.SeedRunAndContextOnlyAsync(
            connection,
            tenantId,
            workspaceId,
            scopeProjectId,
            runId,
            contextId,
            "proj-findings-legacy-read",
            CancellationToken.None);

        string emptyNodes = JsonEntitySerializer.Serialize(new List<GraphNode>());
        string emptyEdges = JsonEntitySerializer.Serialize(new List<GraphEdge>());
        string emptyGraphWarnings = JsonEntitySerializer.Serialize(new List<string>());

        const string insertGraph = """
            INSERT INTO dbo.GraphSnapshots
            (
                GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc,
                NodesJson, EdgesJson, WarningsJson
            )
            VALUES
            (
                @GraphSnapshotId, @ContextSnapshotId, @RunId, @CreatedUtc,
                @NodesJson, @EdgesJson, @WarningsJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertGraph,
                new
                {
                    GraphSnapshotId = graphId,
                    ContextSnapshotId = contextId,
                    RunId = runId,
                    CreatedUtc = DateTime.UtcNow,
                    NodesJson = emptyNodes,
                    EdgesJson = emptyEdges,
                    WarningsJson = emptyGraphWarnings,
                },
                cancellationToken: CancellationToken.None));

        FindingsSnapshot legacyBlob = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = createdUtc,
            SchemaVersion = 1,
            Findings =
            [
                new Finding
                {
                    FindingId = "legacy-direct",
                    FindingType = "InfoFinding",
                    Category = "Test",
                    EngineType = "DirectRead",
                    Severity = FindingSeverity.Info,
                    Title = "from-json",
                    Rationale = "r",
                },
            ],
        };

        string findingsJson = JsonEntitySerializer.Serialize(legacyBlob);

        const string insertFindings = """
            INSERT INTO dbo.FindingsSnapshots
            (
                FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId,
                TenantId, WorkspaceId, ProjectId,
                CreatedUtc, SchemaVersion, FindingsJson
            )
            VALUES
            (
                @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId,
                @TenantId, @WorkspaceId, @ProjectId,
                @CreatedUtc, @SchemaVersion, @FindingsJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertFindings,
                new
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = scopeProjectId,
                    CreatedUtc = createdUtc,
                    SchemaVersion = 1,
                    FindingsJson = findingsJson,
                },
                cancellationToken: CancellationToken.None));

        FindingsSnapshotStorageRow row = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = createdUtc,
            SchemaVersion = 1,
            FindingsJson = findingsJson,
        };

        FindingsSnapshot loaded =
            await FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync(connection, row, CancellationToken.None);

        loaded.FindingsSnapshotId.Should().Be(findingsId);
        loaded.RunId.Should().Be(runId);
        loaded.Findings.Should().ContainSingle();
        loaded.Findings[0].FindingId.Should().Be("legacy-direct");
        loaded.Findings[0].Title.Should().Be("from-json");
    }
}
