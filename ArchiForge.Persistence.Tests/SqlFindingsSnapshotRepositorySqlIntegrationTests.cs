using ArchiForge.ContextIngestion.Models;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Findings.Serialization;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Serialization;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// <see cref="SqlFindingsSnapshotRepository"/> against SQL Server + DbUp (relational findings + FindingsJson dual-write).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlFindingsSnapshotRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task Save_then_GetById_round_trips_relational_rows_and_payload_sidecar()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        SqlFindingsSnapshotRepository repository = new(factory);

        Guid findingsId = Guid.NewGuid();
        FindingsSnapshot snapshot = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            SchemaVersion = FindingsSchema.CurrentSnapshotVersion,
            Findings =
            [
                new Finding
                {
                    FindingId = "f1",
                    FindingType = "RequirementFinding",
                    Category = "Requirement",
                    EngineType = "TestEngine",
                    Severity = FindingSeverity.Warning,
                    Title = "T1",
                    Rationale = "Because",
                    RelatedNodeIds = ["n1", "n2"],
                    RecommendedActions = ["fix it"],
                    Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["k"] = "v" },
                    PayloadType = nameof(RequirementFindingPayload),
                    Payload = new RequirementFindingPayload
                    {
                        RequirementName = "R1",
                        RequirementText = "text",
                        IsMandatory = false,
                    },
                    Trace = new ExplainabilityTrace
                    {
                        GraphNodeIdsExamined = ["g1"],
                        RulesApplied = ["rule-a"],
                        DecisionsTaken = ["decided"],
                        AlternativePathsConsidered = ["alt"],
                        Notes = ["note1"],
                    },
                },
            ],
        };

        FindingsSnapshotMigrator.Apply(snapshot);
        await repository.SaveAsync(snapshot, CancellationToken.None);

        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.FindingsSnapshotId.Should().Be(findingsId);
        loaded.SchemaVersion.Should().Be(FindingsSchema.CurrentSnapshotVersion);
        loaded.Findings.Should().ContainSingle();
        Finding f = loaded.Findings[0];
        f.FindingId.Should().Be("f1");
        f.RelatedNodeIds.Should().Equal("n1", "n2");
        f.RecommendedActions.Should().Equal("fix it");
        f.Properties["k"].Should().Be("v");
        f.Payload.Should().BeOfType<RequirementFindingPayload>();
        ((RequirementFindingPayload)f.Payload!).RequirementName.Should().Be("R1");
        f.Trace.GraphNodeIdsExamined.Should().Equal("g1");
        f.Trace.RulesApplied.Should().Equal("rule-a");
        f.Trace.DecisionsTaken.Should().Equal("decided");
        f.Trace.AlternativePathsConsidered.Should().Equal("alt");
        f.Trace.Notes.Should().Equal("note1");
    }

    [SkippableFact]
    public async Task GetById_when_no_FindingRecords_falls_back_to_FindingsJson()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        Guid findingsId = Guid.NewGuid();
        FindingsSnapshot original = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = DateTime.UtcNow,
            SchemaVersion = 1,
            Findings =
            [
                new Finding
                {
                    FindingId = "legacy",
                    FindingType = "TopologyGap",
                    Category = "Topology",
                    EngineType = "E",
                    Severity = FindingSeverity.Info,
                    Title = "Legacy title",
                    Rationale = "Legacy",
                },
            ],
        };

        FindingsSnapshotMigrator.Apply(original);
        string findingsJson = JsonEntitySerializer.Serialize(original);

        const string insertHeader = """
            INSERT INTO dbo.FindingsSnapshots
            (
                FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc,
                SchemaVersion, FindingsJson
            )
            VALUES
            (
                @FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc,
                @SchemaVersion, @FindingsJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertHeader,
                new
                {
                    FindingsSnapshotId = findingsId,
                    RunId = runId,
                    ContextSnapshotId = contextId,
                    GraphSnapshotId = graphId,
                    original.CreatedUtc,
                    SchemaVersion = 1,
                    FindingsJson = findingsJson,
                },
                cancellationToken: CancellationToken.None));

        SqlFindingsSnapshotRepository repository = new(factory);
        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Findings.Should().ContainSingle(f => f.FindingId == "legacy");
        loaded.Findings[0].Title.Should().Be("Legacy title");
    }

    [SkippableFact]
    public async Task SaveAsync_with_explicit_transaction_commits_relational_rows()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        await SeedAuthorityParentsAsync(connection, runId, contextId, graphId, CancellationToken.None);

        SqlFindingsSnapshotRepository repository = new(factory);
        Guid findingsId = Guid.NewGuid();
        FindingsSnapshot snapshot = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = DateTime.UtcNow,
            Findings = [],
        };

        FindingsSnapshotMigrator.Apply(snapshot);
        await using SqlTransaction tx = connection.BeginTransaction();
        await repository.SaveAsync(snapshot, CancellationToken.None, connection, tx);
        tx.Commit();

        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Findings.Should().BeEmpty();
    }

    private static async Task SeedAuthorityParentsAsync(
        SqlConnection connection,
        Guid runId,
        Guid contextSnapshotId,
        Guid graphSnapshotId,
        CancellationToken ct)
    {
        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid scopeProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        const string insertRun = """
            INSERT INTO dbo.Runs (RunId, ProjectId, CreatedUtc, TenantId, WorkspaceId, ScopeProjectId)
            VALUES (@RunId, @ProjectId, @CreatedUtc, @TenantId, @WorkspaceId, @ScopeProjectId);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRun,
                new
                {
                    RunId = runId,
                    ProjectId = "proj-seed",
                    CreatedUtc = DateTime.UtcNow,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ScopeProjectId = scopeProjectId,
                },
                cancellationToken: ct));

        string emptyCanonical = JsonEntitySerializer.Serialize(new List<CanonicalObject>());
        string emptyStringList = JsonEntitySerializer.Serialize(new List<string>());

        const string insertContext = """
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
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertContext,
                new
                {
                    SnapshotId = contextSnapshotId,
                    RunId = runId,
                    ProjectId = "proj-seed",
                    CreatedUtc = DateTime.UtcNow,
                    CanonicalObjectsJson = emptyCanonical,
                    DeltaSummary = (string?)null,
                    WarningsJson = emptyStringList,
                    ErrorsJson = emptyStringList,
                    SourceHashesJson = JsonEntitySerializer.Serialize(new Dictionary<string, string>()),
                },
                cancellationToken: ct));

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
                    GraphSnapshotId = graphSnapshotId,
                    ContextSnapshotId = contextSnapshotId,
                    RunId = runId,
                    CreatedUtc = DateTime.UtcNow,
                    NodesJson = emptyNodes,
                    EdgesJson = emptyEdges,
                    WarningsJson = emptyGraphWarnings,
                },
                cancellationToken: ct));
    }
}
