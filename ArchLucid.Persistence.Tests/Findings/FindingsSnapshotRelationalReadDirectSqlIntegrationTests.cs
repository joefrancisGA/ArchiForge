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
[Trait("Suite", "Core")]
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

    [SkippableFact]
    public async Task LoadRelationalSnapshotAsync_hydrates_finding_with_ordered_children_and_explainability_trace()
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
            "proj-findings-rel-full",
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
                    FindingId = "rel-f-1",
                    FindingSchemaVersion = 1,
                    FindingType = "RelationalFinding",
                    Category = "Cat",
                    EngineType = "TestEngine",
                    Severity = "Warning",
                    Title = "Rel title",
                    Rationale = "Rel rationale",
                    PayloadType = (string?)null,
                    PayloadJson = (string?)null,
                },
                cancellationToken: CancellationToken.None));

        const string insertRelated = """
            INSERT INTO dbo.FindingRelatedNodes (FindingRecordId, SortOrder, NodeId)
            VALUES (@FindingRecordId, @SortOrder, @NodeId);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRelated,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 1,
                    NodeId = "n-first",
                },
                cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRelated,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 0,
                    NodeId = "n-second",
                },
                cancellationToken: CancellationToken.None));

        const string insertAction = """
            INSERT INTO dbo.FindingRecommendedActions (FindingRecordId, SortOrder, ActionText)
            VALUES (@FindingRecordId, @SortOrder, @ActionText);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertAction,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 0,
                    ActionText = "Do A",
                },
                cancellationToken: CancellationToken.None));

        const string insertProp = """
            INSERT INTO dbo.FindingProperties (FindingRecordId, PropertySortOrder, PropertyKey, PropertyValue)
            VALUES (@FindingRecordId, @PropertySortOrder, @PropertyKey, @PropertyValue);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertProp,
                new
                {
                    FindingRecordId = findingRecordId,
                    PropertySortOrder = 0,
                    PropertyKey = "pk1",
                    PropertyValue = "pv1",
                },
                cancellationToken: CancellationToken.None));

        const string insertTraceNode = """
            INSERT INTO dbo.FindingTraceGraphNodesExamined (FindingRecordId, SortOrder, NodeId)
            VALUES (@FindingRecordId, @SortOrder, @NodeId);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertTraceNode,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 0,
                    NodeId = "trace-node",
                },
                cancellationToken: CancellationToken.None));

        const string insertTraceRule = """
            INSERT INTO dbo.FindingTraceRulesApplied (FindingRecordId, SortOrder, RuleText)
            VALUES (@FindingRecordId, @SortOrder, @RuleText);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertTraceRule,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 0,
                    RuleText = "rule-a",
                },
                cancellationToken: CancellationToken.None));

        const string insertTraceDecision = """
            INSERT INTO dbo.FindingTraceDecisionsTaken (FindingRecordId, SortOrder, DecisionText)
            VALUES (@FindingRecordId, @SortOrder, @DecisionText);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertTraceDecision,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 0,
                    DecisionText = "dec-a",
                },
                cancellationToken: CancellationToken.None));

        const string insertTracePath = """
            INSERT INTO dbo.FindingTraceAlternativePaths (FindingRecordId, SortOrder, PathText)
            VALUES (@FindingRecordId, @SortOrder, @PathText);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertTracePath,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 0,
                    PathText = "path-a",
                },
                cancellationToken: CancellationToken.None));

        const string insertTraceNote = """
            INSERT INTO dbo.FindingTraceNotes (FindingRecordId, SortOrder, NoteText)
            VALUES (@FindingRecordId, @SortOrder, @NoteText);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertTraceNote,
                new
                {
                    FindingRecordId = findingRecordId,
                    SortOrder = 0,
                    NoteText = "note-a",
                },
                cancellationToken: CancellationToken.None));

        FindingsSnapshot loaded =
            await FindingsSnapshotRelationalRead.LoadRelationalSnapshotAsync(connection, headerRow!, CancellationToken.None);

        loaded.Findings.Should().ContainSingle();
        Finding f = loaded.Findings[0];
        f.FindingId.Should().Be("rel-f-1");
        f.Severity.Should().Be(FindingSeverity.Warning);
        f.Title.Should().Be("Rel title");
        f.Rationale.Should().Be("Rel rationale");
        f.RelatedNodeIds.Should().Equal("n-second", "n-first");
        f.RecommendedActions.Should().Equal("Do A");
        f.Properties.Should().ContainKey("pk1").WhoseValue.Should().Be("pv1");
        f.Trace.GraphNodeIdsExamined.Should().Equal("trace-node");
        f.Trace.RulesApplied.Should().Equal("rule-a");
        f.Trace.DecisionsTaken.Should().Equal("dec-a");
        f.Trace.AlternativePathsConsidered.Should().Equal("path-a");
        f.Trace.Notes.Should().Equal("note-a");
    }
}
