using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Serialization;
using ArchiForge.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// <see cref="SqlGoldenManifestRepository"/> against SQL Server + DbUp (phase-1 relational + JSON dual-write).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlGoldenManifestRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [SkippableFact]
    public async Task Save_then_GetById_round_trips_phase1_relational_slices()
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

        await AuthorityRunChainTestSeed.SeedFullChainAsync(
            connection,
            TenantId,
            WorkspaceId,
            ProjectId,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            "proj-gm",
            CancellationToken.None);

        ScopeContext scope = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
        };

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
            CreatedUtc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc),
            ManifestHash = "abc",
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
            Assumptions = ["assume-a"],
            Warnings = ["warn-w"],
            Provenance = new ManifestProvenance
            {
                SourceFindingIds = ["pf1"],
                SourceGraphNodeIds = ["pn1"],
                AppliedRuleIds = ["pr1"],
            },
            Decisions =
            [
                new ResolvedArchitectureDecision
                {
                    DecisionId = "d1",
                    Category = "Cat",
                    Title = "Title",
                    SelectedOption = "Opt",
                    Rationale = "Why",
                    SupportingFindingIds = ["sf1"],
                    RelatedNodeIds = ["node-a"],
                    RawDecisionJson = """{"x":1}""",
                },
            ],
        };

        SqlGoldenManifestRepository repository = new(factory);
        await repository.SaveAsync(manifest, CancellationToken.None);

        GoldenManifest? loaded = await repository.GetByIdAsync(scope, manifestId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Assumptions.Should().Equal("assume-a");
        loaded.Warnings.Should().Equal("warn-w");
        loaded.Provenance.SourceFindingIds.Should().Equal("pf1");
        loaded.Provenance.SourceGraphNodeIds.Should().Equal("pn1");
        loaded.Provenance.AppliedRuleIds.Should().Equal("pr1");
        loaded.Decisions.Should().ContainSingle();
        ResolvedArchitectureDecision d = loaded.Decisions[0];
        d.DecisionId.Should().Be("d1");
        d.SupportingFindingIds.Should().Equal("sf1");
        d.RelatedNodeIds.Should().Equal("node-a");
        d.RawDecisionJson.Should().Be("""{"x":1}""");
    }

    [SkippableFact]
    public async Task GetById_when_no_phase1_rows_falls_back_to_json_columns()
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

        await AuthorityRunChainTestSeed.SeedFullChainAsync(
            connection,
            TenantId,
            WorkspaceId,
            ProjectId,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            "proj-gm",
            CancellationToken.None);

        GoldenManifest original = new()
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
            ManifestHash = "h",
            RuleSetId = "r",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
            Metadata = new ManifestMetadata(),
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Assumptions = ["from-json"],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = [],
        };

        const string insertManifest = """
            INSERT INTO dbo.GoldenManifests
            (
                TenantId, WorkspaceId, ProjectId,
                ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                WarningsJson, ProvenanceJson
            )
            VALUES
            (
                @TenantId, @WorkspaceId, @ProjectId,
                @ManifestId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId, @DecisionTraceId,
                @CreatedUtc, @ManifestHash, @RuleSetId, @RuleSetVersion, @RuleSetHash,
                @MetadataJson, @RequirementsJson, @TopologyJson, @SecurityJson, @ComplianceJson, @CostJson,
                @ConstraintsJson, @UnresolvedIssuesJson, @DecisionsJson, @AssumptionsJson,
                @WarningsJson, @ProvenanceJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertManifest,
                new
                {
                    original.TenantId,
                    original.WorkspaceId,
                    original.ProjectId,
                    original.ManifestId,
                    original.RunId,
                    original.ContextSnapshotId,
                    original.GraphSnapshotId,
                    original.FindingsSnapshotId,
                    original.DecisionTraceId,
                    original.CreatedUtc,
                    original.ManifestHash,
                    original.RuleSetId,
                    original.RuleSetVersion,
                    original.RuleSetHash,
                    MetadataJson = JsonEntitySerializer.Serialize(original.Metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(original.Requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(original.Topology),
                    SecurityJson = JsonEntitySerializer.Serialize(original.Security),
                    ComplianceJson = JsonEntitySerializer.Serialize(original.Compliance),
                    CostJson = JsonEntitySerializer.Serialize(original.Cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(original.Constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(original.UnresolvedIssues),
                    DecisionsJson = JsonEntitySerializer.Serialize(original.Decisions),
                    AssumptionsJson = JsonEntitySerializer.Serialize(original.Assumptions),
                    WarningsJson = JsonEntitySerializer.Serialize(original.Warnings),
                    ProvenanceJson = JsonEntitySerializer.Serialize(original.Provenance),
                },
                cancellationToken: CancellationToken.None));

        ScopeContext scope = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
        };

        SqlGoldenManifestRepository repository = new(factory);
        GoldenManifest? loaded = await repository.GetByIdAsync(scope, manifestId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Assumptions.Should().Equal("from-json");
    }

}
