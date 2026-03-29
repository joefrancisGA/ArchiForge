using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Manifest.Sections;
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
/// <see cref="SqlArtifactBundleRepository"/> against SQL Server + DbUp (relational slices + JSON dual-write).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlArtifactBundleRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid WorkspaceId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ProjectId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Fact]
    public async Task Save_then_GetByManifestId_round_trips_relational_slices()
    {
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

        SqlGoldenManifestRepository manifestRepository = new(factory);
        await manifestRepository.SaveAsync(manifest, CancellationToken.None);

        DateTime bundleCreated = new DateTime(2026, 8, 1, 12, 5, 0, DateTimeKind.Utc);
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

        SqlArtifactBundleRepository repository = new(factory);
        await repository.SaveAsync(bundle, CancellationToken.None);

        ScopeContext scope = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
        };

        ArtifactBundle? loaded = await repository.GetByManifestIdAsync(scope, manifestId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.BundleId.Should().Be(bundleId);
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

    [Fact]
    public async Task GetByManifestId_when_no_relational_rows_falls_back_to_json_columns()
    {
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

        SqlGoldenManifestRepository manifestRepository = new(factory);
        await manifestRepository.SaveAsync(manifest, CancellationToken.None);

        Guid bundleId = Guid.NewGuid();
        DateTime created = DateTime.UtcNow;

        List<SynthesizedArtifact> artifacts =
        [
            new SynthesizedArtifact
            {
                ArtifactId = Guid.NewGuid(),
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

        SqlArtifactBundleRepository repository = new(factory);
        ArtifactBundle? loaded = await repository.GetByManifestIdAsync(scope, manifestId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Artifacts.Should().ContainSingle();
        loaded.Artifacts[0].Content.Should().Be("json-only path");
        loaded.Trace.GeneratorsUsed.Should().Equal("G");
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
                    TenantId = TenantId,
                    WorkspaceId = WorkspaceId,
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
                    TenantId = TenantId,
                    WorkspaceId = WorkspaceId,
                    ProjectId = ProjectId,
                },
                cancellationToken: ct));
    }
}
