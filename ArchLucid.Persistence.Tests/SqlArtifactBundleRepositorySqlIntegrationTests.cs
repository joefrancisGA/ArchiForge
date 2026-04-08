using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Serialization;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
/// <see cref="SqlArtifactBundleRepository"/> against SQL Server + DbUp (relational slices + JSON dual-write).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlArtifactBundleRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid WorkspaceId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ProjectId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [SkippableFact]
    public async Task Save_then_GetByManifestId_round_trips_relational_slices()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        await SeedAuthorityChainAsync(
            connection,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            CancellationToken.None);

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
            CreatedUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
            ManifestHash = "mh",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh",
            Metadata = new ManifestMetadata(),
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Assumptions = [],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = [],
        };

        SqlGoldenManifestRepository manifestRepository = SqlPersistenceRepositoryFactory.CreateGoldenManifestRepository(factory);
        await manifestRepository.SaveAsync(manifest, CancellationToken.None);

        DateTime bundleCreated = new(2026, 8, 1, 12, 5, 0, DateTimeKind.Utc);
        Guid bundleId = Guid.NewGuid();
        Guid artifactId = Guid.NewGuid();
        Guid synthTraceId = Guid.NewGuid();

        ArtifactBundle bundle = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            BundleId = bundleId,
            RunId = runId,
            ManifestId = manifestId,
            CreatedUtc = bundleCreated,
            Artifacts =
            [
                new SynthesizedArtifact
                {
                    ArtifactId = artifactId,
                    RunId = runId,
                    ManifestId = manifestId,
                    CreatedUtc = bundleCreated,
                    ArtifactType = "ArchitectureNarrative",
                    Name = "narrative.md",
                    Format = "markdown",
                    Content = "Plain text body — not JSON-encoded.",
                    ContentHash = "sha256:abc",
                    Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["Section"] = "Overview",
                        ["Lang"] = "en-US",
                    },
                    ContributingDecisionIds = ["dec-a", "dec-b"],
                },
            ],
            Trace = new SynthesisTrace
            {
                TraceId = synthTraceId,
                RunId = runId,
                ManifestId = manifestId,
                CreatedUtc = bundleCreated,
                GeneratorsUsed = ["ArchitectureNarrativeArtifactGenerator"],
                SourceDecisionIds = ["sd-1"],
                Notes = ["Synthesis complete."],
            },
        };

        SqlArtifactBundleRepository repository = SqlPersistenceRepositoryFactory.CreateArtifactBundleRepository(factory);
        await repository.SaveAsync(bundle, CancellationToken.None);

        ScopeContext scope = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
        };

        ArtifactBundle? loaded = await repository.GetByManifestIdAsync(scope, manifestId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.BundleId.Should().Be(bundleId);
        loaded.Artifacts.Should().ContainSingle();
        SynthesizedArtifact a = loaded.Artifacts[0];
        a.Content.Should().Be("Plain text body — not JSON-encoded.");
        a.Metadata.Should().HaveCount(2);
        a.Metadata["Section"].Should().Be("Overview");
        a.ContributingDecisionIds.Should().Equal("dec-a", "dec-b");
        loaded.Trace.GeneratorsUsed.Should().Equal("ArchitectureNarrativeArtifactGenerator");
        loaded.Trace.SourceDecisionIds.Should().Equal("sd-1");
        loaded.Trace.Notes.Should().Equal("Synthesis complete.");
        loaded.Trace.TraceId.Should().Be(synthTraceId);
    }

    [SkippableFact]
    public async Task GetByManifestId_when_no_relational_rows_falls_back_to_json_columns()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        await SeedAuthorityChainAsync(
            connection,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            CancellationToken.None);

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
            ManifestHash = "mh",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh",
            Metadata = new ManifestMetadata(),
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Assumptions = [],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = [],
        };

        SqlGoldenManifestRepository manifestRepository = SqlPersistenceRepositoryFactory.CreateGoldenManifestRepository(factory);
        await manifestRepository.SaveAsync(manifest, CancellationToken.None);

        Guid bundleId = Guid.NewGuid();
        DateTime created = DateTime.UtcNow;
        Guid expectedArtifactId = Guid.NewGuid();

        List<SynthesizedArtifact> artifacts =
        [
            new()
            {
                ArtifactId = expectedArtifactId,
                RunId = runId,
                ManifestId = manifestId,
                CreatedUtc = created,
                ArtifactType = "Legacy",
                Name = "legacy.txt",
                Format = "text",
                Content = "json-only path",
                ContentHash = "h",
                Metadata = new Dictionary<string, string> { ["k"] = "v" },
                ContributingDecisionIds = ["x"],
            },
        ];

        SynthesisTrace trace = new()
        {
            TraceId = Guid.NewGuid(),
            RunId = runId,
            ManifestId = manifestId,
            CreatedUtc = created,
            GeneratorsUsed = ["G"],
            SourceDecisionIds = ["Y"],
            Notes = ["n"],
        };

        const string insertSql = """
            INSERT INTO dbo.ArtifactBundles
            (
                BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson,
                TenantId, WorkspaceId, ProjectId
            )
            VALUES
            (
                @BundleId, @RunId, @ManifestId, @CreatedUtc, @ArtifactsJson, @TraceJson,
                @TenantId, @WorkspaceId, @ProjectId
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertSql,
                new
                {
                    BundleId = bundleId,
                    RunId = runId,
                    ManifestId = manifestId,
                    CreatedUtc = created,
                    ArtifactsJson = JsonEntitySerializer.Serialize(artifacts),
                    TraceJson = JsonEntitySerializer.Serialize(trace),
                    TenantId,
                    WorkspaceId,
                    ProjectId,
                },
                cancellationToken: CancellationToken.None));

        ScopeContext scope = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
        };

        SqlArtifactBundleRepository repository = SqlPersistenceRepositoryFactory.CreateArtifactBundleRepository(factory);
        ArtifactBundle? loaded = await repository.GetByManifestIdAsync(scope, manifestId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Artifacts.Should().ContainSingle();
        SynthesizedArtifact artifact = loaded.Artifacts[0];
        artifact.ArtifactId.Should().Be(expectedArtifactId);
        artifact.RunId.Should().Be(runId);
        artifact.ManifestId.Should().Be(manifestId);
        artifact.CreatedUtc.Should().Be(created);
        artifact.ArtifactType.Should().Be("Legacy");
        artifact.Name.Should().Be("legacy.txt");
        artifact.Format.Should().Be("text");
        artifact.ContentHash.Should().Be("h");
        artifact.Content.Should().Be("json-only path");
        artifact.Metadata.Should().ContainKey("k");
        artifact.Metadata["k"].Should().Be("v");
        artifact.ContributingDecisionIds.Should().Equal("x");
        loaded.Trace.TraceId.Should().Be(trace.TraceId);
        loaded.Trace.GeneratorsUsed.Should().Equal("G");
        loaded.Trace.SourceDecisionIds.Should().Equal("Y");
        loaded.Trace.Notes.Should().Equal("n");
    }

    [SkippableFact]
    public async Task GetByManifestId_json_fallback_with_multiple_artifacts_preserves_order_and_metadata()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        await SeedAuthorityChainAsync(
            connection,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            CancellationToken.None);

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
            ManifestHash = "mh",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh",
            Metadata = new ManifestMetadata(),
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Assumptions = [],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = [],
        };

        SqlGoldenManifestRepository manifestRepository = SqlPersistenceRepositoryFactory.CreateGoldenManifestRepository(factory);
        await manifestRepository.SaveAsync(manifest, CancellationToken.None);

        Guid bundleId = Guid.NewGuid();
        DateTime created = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        Guid artifactIdFirst = Guid.NewGuid();
        Guid artifactIdSecond = Guid.NewGuid();

        List<SynthesizedArtifact> artifacts =
        [
            new()
            {
                ArtifactId = artifactIdFirst,
                RunId = runId,
                ManifestId = manifestId,
                CreatedUtc = created,
                ArtifactType = "TypeAlpha",
                Name = "first.bin",
                Format = "binary",
                Content = "body-one",
                ContentHash = "hash-one",
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["m1a"] = "v1a",
                    ["m1b"] = "v1b",
                },
                ContributingDecisionIds = ["d1a", "d1b"],
            },
            new()
            {
                ArtifactId = artifactIdSecond,
                RunId = runId,
                ManifestId = manifestId,
                CreatedUtc = created,
                ArtifactType = "TypeBeta",
                Name = "second.txt",
                Format = "utf8",
                Content = "body-two",
                ContentHash = "hash-two",
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["m2a"] = "v2a",
                    ["m2b"] = "v2b",
                },
                ContributingDecisionIds = ["d2a", "d2b", "d2c"],
            },
        ];

        SynthesisTrace trace = new()
        {
            TraceId = Guid.NewGuid(),
            RunId = runId,
            ManifestId = manifestId,
            CreatedUtc = created,
            GeneratorsUsed = ["GeneratorOne", "GeneratorTwo"],
            SourceDecisionIds = ["src-dec-a", "src-dec-b"],
            Notes = ["trace-note-a", "trace-note-b"],
        };

        const string insertSql = """
            INSERT INTO dbo.ArtifactBundles
            (
                BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson,
                TenantId, WorkspaceId, ProjectId
            )
            VALUES
            (
                @BundleId, @RunId, @ManifestId, @CreatedUtc, @ArtifactsJson, @TraceJson,
                @TenantId, @WorkspaceId, @ProjectId
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertSql,
                new
                {
                    BundleId = bundleId,
                    RunId = runId,
                    ManifestId = manifestId,
                    CreatedUtc = created,
                    ArtifactsJson = JsonEntitySerializer.Serialize(artifacts),
                    TraceJson = JsonEntitySerializer.Serialize(trace),
                    TenantId,
                    WorkspaceId,
                    ProjectId,
                },
                cancellationToken: CancellationToken.None));

        ScopeContext scope = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
        };

        SqlArtifactBundleRepository repository = SqlPersistenceRepositoryFactory.CreateArtifactBundleRepository(factory);
        ArtifactBundle? loaded = await repository.GetByManifestIdAsync(scope, manifestId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Artifacts.Should().HaveCount(2);

        SynthesizedArtifact first = loaded.Artifacts[0];
        first.ArtifactType.Should().Be("TypeAlpha");
        first.Name.Should().Be("first.bin");
        first.Format.Should().Be("binary");
        first.ContentHash.Should().Be("hash-one");
        first.Content.Should().Be("body-one");
        first.Metadata.Should().HaveCount(2);
        first.Metadata["m1a"].Should().Be("v1a");
        first.Metadata["m1b"].Should().Be("v1b");
        first.ContributingDecisionIds.Should().Equal("d1a", "d1b");

        SynthesizedArtifact second = loaded.Artifacts[1];
        second.ArtifactType.Should().Be("TypeBeta");
        second.Name.Should().Be("second.txt");
        second.Format.Should().Be("utf8");
        second.ContentHash.Should().Be("hash-two");
        second.Content.Should().Be("body-two");
        second.Metadata.Should().HaveCount(2);
        second.Metadata["m2a"].Should().Be("v2a");
        second.Metadata["m2b"].Should().Be("v2b");
        second.ContributingDecisionIds.Should().Equal("d2a", "d2b", "d2c");

        loaded.Trace.GeneratorsUsed.Should().Equal("GeneratorOne", "GeneratorTwo");
        loaded.Trace.SourceDecisionIds.Should().Equal("src-dec-a", "src-dec-b");
        loaded.Trace.Notes.Should().Equal("trace-note-a", "trace-note-b");
        loaded.Trace.TraceId.Should().Be(trace.TraceId);
    }

    [SkippableFact]
    public async Task GetByManifestId_when_ArtifactsJson_is_null_and_no_relational_rows_returns_empty_artifacts()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        Guid runId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        await SeedAuthorityChainAsync(
            connection,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            CancellationToken.None);

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
            ManifestHash = "mh",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh",
            Metadata = new ManifestMetadata(),
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Assumptions = [],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = [],
        };

        SqlGoldenManifestRepository manifestRepository = SqlPersistenceRepositoryFactory.CreateGoldenManifestRepository(factory);
        await manifestRepository.SaveAsync(manifest, CancellationToken.None);

        Guid bundleId = Guid.NewGuid();
        DateTime created = new(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc);
        SynthesisTrace emptyTrace = new()
        {
            TraceId = Guid.NewGuid(),
            RunId = runId,
            ManifestId = manifestId,
            CreatedUtc = created,
            GeneratorsUsed = [],
            SourceDecisionIds = [],
            Notes = [],
        };

        const string insertSql = """
            INSERT INTO dbo.ArtifactBundles
            (
                BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson,
                TenantId, WorkspaceId, ProjectId
            )
            VALUES
            (
                @BundleId, @RunId, @ManifestId, @CreatedUtc, @ArtifactsJson, @TraceJson,
                @TenantId, @WorkspaceId, @ProjectId
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertSql,
                new
                {
                    BundleId = bundleId,
                    RunId = runId,
                    ManifestId = manifestId,
                    CreatedUtc = created,
                    ArtifactsJson = (string?)null,
                    TraceJson = JsonEntitySerializer.Serialize(emptyTrace),
                    TenantId,
                    WorkspaceId,
                    ProjectId,
                },
                cancellationToken: CancellationToken.None));

        ScopeContext scope = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
        };

        SqlArtifactBundleRepository repository = SqlPersistenceRepositoryFactory.CreateArtifactBundleRepository(factory);
        ArtifactBundle? loaded = await repository.GetByManifestIdAsync(scope, manifestId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Artifacts.Should().BeEmpty();
        loaded.Trace.Should().NotBeNull();
        loaded.Trace.TraceId.Should().Be(emptyTrace.TraceId);
        loaded.Trace.GeneratorsUsed.Should().BeEmpty();
        loaded.Trace.SourceDecisionIds.Should().BeEmpty();
        loaded.Trace.Notes.Should().BeEmpty();
    }

    private static async Task SeedAuthorityChainAsync(
        SqlConnection connection,
        Guid runId,
        Guid contextSnapshotId,
        Guid graphSnapshotId,
        Guid findingsSnapshotId,
        Guid decisionTraceId,
        CancellationToken ct)
    {
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
                    ProjectId = "proj-ab",
                    CreatedUtc = DateTime.UtcNow,
                    TenantId,
                    WorkspaceId,
                    ScopeProjectId = ProjectId,
                },
                cancellationToken: ct));

        string emptyCanonical = JsonEntitySerializer.Serialize(new List<CanonicalObject>());
        string emptyList = JsonEntitySerializer.Serialize(new List<string>());

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
                    ProjectId = "proj-ab",
                    CreatedUtc = DateTime.UtcNow,
                    CanonicalObjectsJson = emptyCanonical,
                    DeltaSummary = (string?)null,
                    WarningsJson = emptyList,
                    ErrorsJson = emptyList,
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

        const string insertFindings = """
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
                insertFindings,
                new
                {
                    FindingsSnapshotId = findingsSnapshotId,
                    RunId = runId,
                    ContextSnapshotId = contextSnapshotId,
                    GraphSnapshotId = graphSnapshotId,
                    CreatedUtc = DateTime.UtcNow,
                    SchemaVersion = 1,
                    FindingsJson = JsonEntitySerializer.Serialize(new FindingsSnapshot
                    {
                        FindingsSnapshotId = findingsSnapshotId,
                        RunId = runId,
                        ContextSnapshotId = contextSnapshotId,
                        GraphSnapshotId = graphSnapshotId,
                        CreatedUtc = DateTime.UtcNow,
                        Findings = [],
                    }),
                },
                cancellationToken: ct));

        const string insertTrace = """
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
                @RuleSetId, @RuleSetVersion, @RuleSetHash,
                @AppliedRuleIdsJson, @AcceptedFindingIdsJson, @RejectedFindingIdsJson, @NotesJson,
                @TenantId, @WorkspaceId, @ProjectId
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertTrace,
                new
                {
                    DecisionTraceId = decisionTraceId,
                    RunId = runId,
                    CreatedUtc = DateTime.UtcNow,
                    RuleSetId = "rs",
                    RuleSetVersion = "1",
                    RuleSetHash = "h",
                    AppliedRuleIdsJson = emptyList,
                    AcceptedFindingIdsJson = emptyList,
                    RejectedFindingIdsJson = emptyList,
                    NotesJson = emptyList,
                    TenantId,
                    WorkspaceId,
                    ProjectId,
                },
                cancellationToken: ct));
    }
}
