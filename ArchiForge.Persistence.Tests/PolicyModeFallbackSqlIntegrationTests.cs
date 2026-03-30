using ArchiForge.ContextIngestion.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Findings.Serialization;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.RelationalRead;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Serialization;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// 53R-5 SQL integration tests: verify that each <see cref="PersistenceReadMode"/> produces the
/// expected behavior when reading entities with JSON-only rows (no relational children) vs
/// fully-relationalized rows.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class PolicyModeFallbackSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    // ── A. ContextSnapshot ─────────────────────────────────────────

    [SkippableFact]
    public async Task ContextSnapshot_JsonOnlyRow_AllowMode_LoadsFromJson()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        (Guid snapshotId, _) = await SeedJsonOnlyContextSnapshotAsync(factory);

        SqlContextSnapshotRepository repository = new(factory);
        ContextSnapshot? loaded = await repository.GetByIdAsync(snapshotId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.CanonicalObjects.Should().ContainSingle(o => o.ObjectId == "json-obj");
        loaded.Warnings.Should().Equal("json-warn");
    }

    [SkippableFact]
    public async Task ContextSnapshot_JsonOnlyRow_WarnMode_LoadsFromJson_EmitsWarning()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        (Guid snapshotId, _) = await SeedJsonOnlyContextSnapshotAsync(factory);

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.WarnOnJsonFallback, logger);

        ContextSnapshot? row = await LoadContextSnapshotWithPolicyAsync(connection, snapshotId, policy, CancellationToken.None);

        row.Should().NotBeNull();
        row!.CanonicalObjects.Should().ContainSingle(o => o.ObjectId == "json-obj");
        logger.WarningCount.Should().BeGreaterOrEqualTo(1);
    }

    [SkippableFact]
    public async Task ContextSnapshot_JsonOnlyRow_RequireMode_Throws()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        (Guid snapshotId, _) = await SeedJsonOnlyContextSnapshotAsync(factory);

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        JsonFallbackPolicy policy = new(PersistenceReadMode.RequireRelational, NullLogger.Instance);

        Func<Task> act = () => LoadContextSnapshotWithPolicyAsync(connection, snapshotId, policy, CancellationToken.None);

        (await act.Should().ThrowAsync<RelationalDataMissingException>())
            .Which.EntityType.Should().Be("ContextSnapshot");
    }

    [SkippableFact]
    public async Task ContextSnapshot_RelationalRows_RequireMode_LoadsSuccessfully()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        SqlContextSnapshotRepository repository = new(factory);

        Guid snapshotId = Guid.NewGuid();
        ContextSnapshot snapshot = new()
        {
            SnapshotId = snapshotId,
            RunId = Guid.NewGuid(),
            ProjectId = "proj-strict-ctx",
            CreatedUtc = DateTime.UtcNow,
            CanonicalObjects =
            [
                new CanonicalObject
                {
                    ObjectId = "strict-obj",
                    ObjectType = "Service",
                    Name = "Svc",
                    SourceType = "R",
                    SourceId = "s",
                    Properties = [],
                },
            ],
            Warnings = ["w"],
            Errors = ["e"],
            SourceHashes = new Dictionary<string, string>(StringComparer.Ordinal) { ["f"] = "h" },
        };

        await repository.SaveAsync(snapshot, CancellationToken.None);

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);
        JsonFallbackPolicy policy = new(PersistenceReadMode.RequireRelational, NullLogger.Instance);

        ContextSnapshot? loaded = await LoadContextSnapshotWithPolicyAsync(connection, snapshotId, policy, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.CanonicalObjects.Should().ContainSingle(o => o.ObjectId == "strict-obj");
        loaded.Warnings.Should().Equal("w");
        loaded.Errors.Should().Equal("e");
        loaded.SourceHashes["f"].Should().Be("h");
    }

    // ── B. FindingsSnapshot ────────────────────────────────────────

    [SkippableFact]
    public async Task FindingsSnapshot_JsonOnlyRow_AllowMode_LoadsFromJson()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        Guid findingsId = await SeedJsonOnlyFindingsSnapshotAsync(factory);

        SqlFindingsSnapshotRepository repository = new(factory, fallbackPolicy: null);
        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Findings.Should().ContainSingle(f => f.FindingId == "json-finding");
    }

    [SkippableFact]
    public async Task FindingsSnapshot_JsonOnlyRow_WarnMode_LoadsFromJson()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        Guid findingsId = await SeedJsonOnlyFindingsSnapshotAsync(factory);

        FakeLogger logger = new();
        JsonFallbackPolicy policy = new(PersistenceReadMode.WarnOnJsonFallback, logger);
        SqlFindingsSnapshotRepository repository = new(factory, policy);

        FindingsSnapshot? loaded = await repository.GetByIdAsync(findingsId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Findings.Should().ContainSingle(f => f.FindingId == "json-finding");
        logger.WarningCount.Should().BeGreaterOrEqualTo(1);
    }

    [SkippableFact]
    public async Task FindingsSnapshot_JsonOnlyRow_RequireMode_Throws()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        Guid findingsId = await SeedJsonOnlyFindingsSnapshotAsync(factory);

        JsonFallbackPolicy policy = new(PersistenceReadMode.RequireRelational, NullLogger.Instance);
        SqlFindingsSnapshotRepository repository = new(factory, policy);

        Func<Task> act = () => repository.GetByIdAsync(findingsId, CancellationToken.None);

        (await act.Should().ThrowAsync<RelationalDataMissingException>())
            .Which.SliceName.Should().Be("FindingsSnapshot.Findings");
    }

    // ── C. GraphSnapshot (including edge metadata merge seam) ──────

    [SkippableFact]
    public async Task GraphSnapshot_JsonOnlyRow_AllowMode_LoadsFromJson()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        Guid graphId = await SeedJsonOnlyGraphSnapshotAsync(factory);

        SqlGraphSnapshotRepository repository = new(factory);
        GraphSnapshot? loaded = await repository.GetByIdAsync(graphId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Nodes.Should().ContainSingle(n => n.NodeId == "json-node");
        loaded.Warnings.Should().Equal("json-gw");
    }

    [SkippableFact]
    public async Task GraphSnapshot_EdgeMetadataMerge_AllowMode_MergesFromJson()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid graphId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();

        List<GraphEdge> edges =
        [
            new()
            {
                EdgeId = "e1",
                FromNodeId = "n1",
                ToNodeId = "n2",
                EdgeType = "CALLS",
                Label = "json-label",
                Weight = 1d,
                Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["proto"] = "grpc" },
            },
        ];

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.GraphSnapshots
            (GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc, NodesJson, EdgesJson, WarningsJson)
            VALUES (@GraphSnapshotId, @ContextSnapshotId, @RunId, @CreatedUtc, @NodesJson, @EdgesJson, @WarningsJson);
            """,
            new
            {
                GraphSnapshotId = graphId,
                ContextSnapshotId = contextId,
                RunId = runId,
                CreatedUtc = DateTime.UtcNow,
                NodesJson = JsonEntitySerializer.Serialize(new List<GraphNode>()),
                EdgesJson = JsonEntitySerializer.Serialize(edges),
                WarningsJson = JsonEntitySerializer.Serialize(new List<string>()),
            },
            cancellationToken: CancellationToken.None));

        // Insert relational edge rows but NO edge properties
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId, FromNodeId, ToNodeId, EdgeType, Weight)
            VALUES (@GraphSnapshotId, @EdgeId, @FromNodeId, @ToNodeId, @EdgeType, @Weight);
            """,
            new
            {
                GraphSnapshotId = graphId,
                EdgeId = "e1",
                FromNodeId = "n1",
                ToNodeId = "n2",
                EdgeType = "CALLS",
                Weight = 1d,
            },
            cancellationToken: CancellationToken.None));

        SqlGraphSnapshotRepository repository = new(factory);
        GraphSnapshot? loaded = await repository.GetByIdAsync(graphId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Edges.Should().ContainSingle();
        loaded.Edges[0].Label.Should().Be("json-label");
        loaded.Edges[0].Properties["proto"].Should().Be("grpc");
    }

    // ── D. GoldenManifest ──────────────────────────────────────────

    [SkippableFact]
    public async Task GoldenManifest_JsonOnlyRow_AllowMode_LoadsFromJson()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        Guid manifestId = await SeedJsonOnlyGoldenManifestAsync(factory);

        ScopeContext scope = new() { TenantId = TenantId, WorkspaceId = WorkspaceId, ProjectId = ProjectId };
        SqlGoldenManifestRepository repository = new(factory);
        GoldenManifest? loaded = await repository.GetByIdAsync(scope, manifestId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Assumptions.Should().Equal("json-assumption");
    }

    [SkippableFact]
    public async Task GoldenManifest_RelationalRows_RequireMode_LoadsSuccessfully()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);

        (Guid manifestId, Guid runId) = await SeedRelationalGoldenManifestAsync(factory);

        ScopeContext scope = new() { TenantId = TenantId, WorkspaceId = WorkspaceId, ProjectId = ProjectId };
        SqlGoldenManifestRepository repository = new(factory);
        GoldenManifest? loaded = await repository.GetByIdAsync(scope, manifestId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Assumptions.Should().Equal("relational-assumption");
        loaded.Warnings.Should().Equal("relational-warn");
        loaded.Provenance.SourceFindingIds.Should().Equal("pf-rel");
        loaded.Decisions.Should().ContainSingle();
        loaded.Decisions[0].DecisionId.Should().Be("d-rel");
    }

    // ── Shared seed helpers ────────────────────────────────────────

    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static async Task<(Guid SnapshotId, Guid RunId)> SeedJsonOnlyContextSnapshotAsync(SqlConnectionFactory factory)
    {
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid snapshotId = Guid.NewGuid();

        await InsertRunAsync(connection, runId, "proj-policy-ctx");

        List<CanonicalObject> canonical =
        [
            new()
            {
                ObjectId = "json-obj",
                ObjectType = "T",
                Name = "JsonObj",
                SourceType = "S",
                SourceId = "sid",
                Properties = [],
            },
        ];

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.ContextSnapshots
            (SnapshotId, RunId, ProjectId, CreatedUtc, CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson)
            VALUES
            (@SnapshotId, @RunId, @ProjectId, @CreatedUtc, @CanonicalObjectsJson, @DeltaSummary, @WarningsJson, @ErrorsJson, @SourceHashesJson);
            """,
            new
            {
                SnapshotId = snapshotId,
                RunId = runId,
                ProjectId = "proj-policy-ctx",
                CreatedUtc = DateTime.UtcNow,
                CanonicalObjectsJson = JsonEntitySerializer.Serialize(canonical),
                DeltaSummary = (string?)null,
                WarningsJson = JsonEntitySerializer.Serialize(new List<string> { "json-warn" }),
                ErrorsJson = JsonEntitySerializer.Serialize(new List<string>()),
                SourceHashesJson = JsonEntitySerializer.Serialize(new Dictionary<string, string>()),
            },
            cancellationToken: CancellationToken.None));

        return (snapshotId, runId);
    }

    private static async Task<ContextSnapshot?> LoadContextSnapshotWithPolicyAsync(
        SqlConnection connection, Guid snapshotId, JsonFallbackPolicy policy, CancellationToken ct)
    {
        ContextSnapshots.ContextSnapshotStorageRow? row = await connection.QuerySingleOrDefaultAsync<ContextSnapshots.ContextSnapshotStorageRow>(
            new CommandDefinition(
                """
                SELECT SnapshotId, RunId, ProjectId, CreatedUtc,
                       CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson
                FROM dbo.ContextSnapshots WHERE SnapshotId = @SnapshotId;
                """,
                new { SnapshotId = snapshotId },
                cancellationToken: ct));

        if (row is null) return null;

        return await ContextSnapshots.ContextSnapshotRelationalRead.HydrateAsync(connection, transaction: null, row, ct, policy);
    }

    private static async Task<Guid> SeedJsonOnlyFindingsSnapshotAsync(SqlConnectionFactory factory)
    {
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();

        await InsertRunAsync(connection, runId, "proj-policy-find");
        await InsertMinimalContextAndGraphAsync(connection, runId, contextId, graphId);

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
                    FindingId = "json-finding",
                    FindingType = "Gap",
                    Category = "Topology",
                    EngineType = "E",
                    Severity = FindingSeverity.Info,
                    Title = "Json finding",
                    Rationale = "Legacy",
                },
            ],
        };

        FindingsSnapshotMigrator.Apply(original);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.FindingsSnapshots
            (FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc, SchemaVersion, FindingsJson)
            VALUES (@FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc, @SchemaVersion, @FindingsJson);
            """,
            new
            {
                FindingsSnapshotId = findingsId,
                RunId = runId,
                ContextSnapshotId = contextId,
                GraphSnapshotId = graphId,
                original.CreatedUtc,
                SchemaVersion = 1,
                FindingsJson = JsonEntitySerializer.Serialize(original),
            },
            cancellationToken: CancellationToken.None));

        return findingsId;
    }

    private static async Task<Guid> SeedJsonOnlyGraphSnapshotAsync(SqlConnectionFactory factory)
    {
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid graphId = Guid.NewGuid();

        List<GraphNode> nodes =
        [
            new()
            {
                NodeId = "json-node",
                NodeType = "Service",
                Label = "LegacyNode",
                Properties = [],
            },
        ];

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.GraphSnapshots
            (GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc, NodesJson, EdgesJson, WarningsJson)
            VALUES (@GraphSnapshotId, @ContextSnapshotId, @RunId, @CreatedUtc, @NodesJson, @EdgesJson, @WarningsJson);
            """,
            new
            {
                GraphSnapshotId = graphId,
                ContextSnapshotId = Guid.NewGuid(),
                RunId = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow,
                NodesJson = JsonEntitySerializer.Serialize(nodes),
                EdgesJson = JsonEntitySerializer.Serialize(new List<GraphEdge>()),
                WarningsJson = JsonEntitySerializer.Serialize(new List<string> { "json-gw" }),
            },
            cancellationToken: CancellationToken.None));

        return graphId;
    }

    private static async Task<Guid> SeedJsonOnlyGoldenManifestAsync(SqlConnectionFactory factory)
    {
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        await InsertRunAsync(connection, runId, "proj-policy-gm");
        await InsertMinimalContextAndGraphAsync(connection, runId, contextId, graphId);
        await InsertMinimalFindingsAsync(connection, runId, contextId, graphId, findingsId);
        await InsertMinimalTraceAsync(connection, runId, traceId);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.GoldenManifests
            (
                TenantId, WorkspaceId, ProjectId,
                ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson, WarningsJson, ProvenanceJson
            )
            VALUES
            (
                @TenantId, @WorkspaceId, @ProjectId,
                @ManifestId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId, @DecisionTraceId,
                @CreatedUtc, @ManifestHash, @RuleSetId, @RuleSetVersion, @RuleSetHash,
                @MetadataJson, @RequirementsJson, @TopologyJson, @SecurityJson, @ComplianceJson, @CostJson,
                @ConstraintsJson, @UnresolvedIssuesJson, @DecisionsJson, @AssumptionsJson, @WarningsJson, @ProvenanceJson
            );
            """,
            new
            {
                TenantId,
                WorkspaceId,
                ProjectId,
                ManifestId = manifestId,
                RunId = runId,
                ContextSnapshotId = contextId,
                GraphSnapshotId = graphId,
                FindingsSnapshotId = findingsId,
                DecisionTraceId = traceId,
                CreatedUtc = DateTime.UtcNow,
                ManifestHash = "h",
                RuleSetId = "r",
                RuleSetVersion = "1",
                RuleSetHash = "rh",
                MetadataJson = JsonEntitySerializer.Serialize(new ManifestMetadata()),
                RequirementsJson = JsonEntitySerializer.Serialize(new RequirementsCoverageSection()),
                TopologyJson = JsonEntitySerializer.Serialize(new TopologySection()),
                SecurityJson = JsonEntitySerializer.Serialize(new SecuritySection()),
                ComplianceJson = JsonEntitySerializer.Serialize(new ComplianceSection()),
                CostJson = JsonEntitySerializer.Serialize(new CostSection()),
                ConstraintsJson = JsonEntitySerializer.Serialize(new ConstraintSection()),
                UnresolvedIssuesJson = JsonEntitySerializer.Serialize(new UnresolvedIssuesSection()),
                DecisionsJson = JsonEntitySerializer.Serialize(new List<ResolvedArchitectureDecision>()),
                AssumptionsJson = JsonEntitySerializer.Serialize(new List<string> { "json-assumption" }),
                WarningsJson = JsonEntitySerializer.Serialize(new List<string>()),
                ProvenanceJson = JsonEntitySerializer.Serialize(new ManifestProvenance()),
            },
            cancellationToken: CancellationToken.None));

        return manifestId;
    }

    private static async Task<(Guid ManifestId, Guid RunId)> SeedRelationalGoldenManifestAsync(SqlConnectionFactory factory)
    {
        SqlGoldenManifestRepository repository = new(factory);

        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        await InsertRunAsync(connection, runId, "proj-policy-gm-rel");
        await InsertMinimalContextAndGraphAsync(connection, runId, contextId, graphId);
        await InsertMinimalFindingsAsync(connection, runId, contextId, graphId, findingsId);
        await InsertMinimalTraceAsync(connection, runId, traceId);

        GoldenManifest manifest = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            FindingsSnapshotId = findingsId,
            DecisionTraceId = traceId,
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "hrel",
            RuleSetId = "rrel",
            RuleSetVersion = "1",
            RuleSetHash = "rhrel",
            Metadata = new ManifestMetadata(),
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Assumptions = ["relational-assumption"],
            Warnings = ["relational-warn"],
            Provenance = new ManifestProvenance
            {
                SourceFindingIds = ["pf-rel"],
                SourceGraphNodeIds = [],
                AppliedRuleIds = [],
            },
            Decisions =
            [
                new ResolvedArchitectureDecision
                {
                    DecisionId = "d-rel",
                    Category = "Cat",
                    Title = "T",
                    SelectedOption = "Opt",
                    Rationale = "Why",
                    SupportingFindingIds = [],
                    RelatedNodeIds = [],
                    RawDecisionJson = "{}",
                },
            ],
        };

        await repository.SaveAsync(manifest, CancellationToken.None);
        return (manifestId, runId);
    }

    // ── Minimal authority chain inserts ─────────────────────────────

    private static async Task InsertRunAsync(SqlConnection connection, Guid runId, string projectId)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.Runs (RunId, ProjectId, CreatedUtc, TenantId, WorkspaceId, ScopeProjectId)
            VALUES (@RunId, @ProjectId, @CreatedUtc, @TenantId, @WorkspaceId, @ScopeProjectId);
            """,
            new
            {
                RunId = runId,
                ProjectId = projectId,
                CreatedUtc = DateTime.UtcNow,
                TenantId,
                WorkspaceId,
                ScopeProjectId = ProjectId,
            },
            cancellationToken: CancellationToken.None));
    }

    private static async Task InsertMinimalContextAndGraphAsync(
        SqlConnection connection, Guid runId, Guid contextId, Guid graphId)
    {
        string emptyCanonical = JsonEntitySerializer.Serialize(new List<CanonicalObject>());
        string emptyList = JsonEntitySerializer.Serialize(new List<string>());
        string emptyDict = JsonEntitySerializer.Serialize(new Dictionary<string, string>());

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.ContextSnapshots
            (SnapshotId, RunId, ProjectId, CreatedUtc, CanonicalObjectsJson, DeltaSummary, WarningsJson, ErrorsJson, SourceHashesJson)
            VALUES (@SnapshotId, @RunId, @ProjectId, @CreatedUtc, @CanonicalObjectsJson, NULL, @WarningsJson, @ErrorsJson, @SourceHashesJson);
            """,
            new
            {
                SnapshotId = contextId,
                RunId = runId,
                ProjectId = "proj-seed",
                CreatedUtc = DateTime.UtcNow,
                CanonicalObjectsJson = emptyCanonical,
                WarningsJson = emptyList,
                ErrorsJson = emptyList,
                SourceHashesJson = emptyDict,
            },
            cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.GraphSnapshots
            (GraphSnapshotId, ContextSnapshotId, RunId, CreatedUtc, NodesJson, EdgesJson, WarningsJson)
            VALUES (@GraphSnapshotId, @ContextSnapshotId, @RunId, @CreatedUtc, @NodesJson, @EdgesJson, @WarningsJson);
            """,
            new
            {
                GraphSnapshotId = graphId,
                ContextSnapshotId = contextId,
                RunId = runId,
                CreatedUtc = DateTime.UtcNow,
                NodesJson = JsonEntitySerializer.Serialize(new List<GraphNode>()),
                EdgesJson = JsonEntitySerializer.Serialize(new List<GraphEdge>()),
                WarningsJson = emptyList,
            },
            cancellationToken: CancellationToken.None));
    }

    private static async Task InsertMinimalFindingsAsync(
        SqlConnection connection, Guid runId, Guid contextId, Guid graphId, Guid findingsId)
    {
        FindingsSnapshot empty = new()
        {
            FindingsSnapshotId = findingsId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            CreatedUtc = DateTime.UtcNow,
            Findings = [],
        };

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.FindingsSnapshots
            (FindingsSnapshotId, RunId, ContextSnapshotId, GraphSnapshotId, CreatedUtc, SchemaVersion, FindingsJson)
            VALUES (@FindingsSnapshotId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @CreatedUtc, 1, @FindingsJson);
            """,
            new
            {
                FindingsSnapshotId = findingsId,
                RunId = runId,
                ContextSnapshotId = contextId,
                GraphSnapshotId = graphId,
                empty.CreatedUtc,
                FindingsJson = JsonEntitySerializer.Serialize(empty),
            },
            cancellationToken: CancellationToken.None));
    }

    private static async Task InsertMinimalTraceAsync(SqlConnection connection, Guid runId, Guid traceId)
    {
        string emptyList = JsonEntitySerializer.Serialize(new List<string>());

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO dbo.DecisioningTraces
            (
                DecisionTraceId, RunId, CreatedUtc,
                RuleSetId, RuleSetVersion, RuleSetHash,
                AppliedRuleIdsJson, AcceptedFindingIdsJson, RejectedFindingIdsJson, NotesJson,
                TenantId, WorkspaceId, ProjectId
            )
            VALUES
            (
                @DecisionTraceId, @RunId, @CreatedUtc,
                'rs', '1', 'h',
                @EmptyList, @EmptyList, @EmptyList, @EmptyList,
                @TenantId, @WorkspaceId, @ProjectId
            );
            """,
            new
            {
                DecisionTraceId = traceId,
                RunId = runId,
                CreatedUtc = DateTime.UtcNow,
                EmptyList = emptyList,
                TenantId,
                WorkspaceId,
                ProjectId,
            },
            cancellationToken: CancellationToken.None));
    }

    // ── FakeLogger (shared) ────────────────────────────────────────

    private sealed class FakeLogger : ILogger
    {
        public int WarningCount { get; private set; }

        public int TotalLogCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            TotalLogCount++;

            if (logLevel == LogLevel.Warning)
                WarningCount++;
        }
    }
}
