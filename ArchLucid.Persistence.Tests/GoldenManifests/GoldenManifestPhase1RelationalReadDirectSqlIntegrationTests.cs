using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.GoldenManifests;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.GoldenManifests;

/// <summary>
///     Direct coverage for <see cref="GoldenManifestPhase1RelationalRead.HydrateAsync" /> when relational slice rows
///     override legacy JSON columns.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GoldenManifestPhase1RelationalReadDirectSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [SkippableFact]
    public async Task HydrateAsync_prefers_relational_GoldenManifestAssumptions_over_AssumptionsJson()
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
            "proj-gm-phase1-direct",
            CancellationToken.None);

        ManifestDocument template = new()
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
            CreatedUtc = new DateTime(2026, 4, 14, 14, 0, 0, DateTimeKind.Utc),
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
            Assumptions = ["json-fallback-should-not-win"],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = []
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
                    template.TenantId,
                    template.WorkspaceId,
                    template.ProjectId,
                    template.ManifestId,
                    template.RunId,
                    template.ContextSnapshotId,
                    template.GraphSnapshotId,
                    template.FindingsSnapshotId,
                    template.DecisionTraceId,
                    template.CreatedUtc,
                    template.ManifestHash,
                    template.RuleSetId,
                    template.RuleSetVersion,
                    template.RuleSetHash,
                    MetadataJson = JsonEntitySerializer.Serialize(template.Metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(template.Requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(template.Topology),
                    SecurityJson = JsonEntitySerializer.Serialize(template.Security),
                    ComplianceJson = JsonEntitySerializer.Serialize(template.Compliance),
                    CostJson = JsonEntitySerializer.Serialize(template.Cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(template.Constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(template.UnresolvedIssues),
                    DecisionsJson = JsonEntitySerializer.Serialize(template.Decisions),
                    AssumptionsJson = JsonEntitySerializer.Serialize(template.Assumptions),
                    WarningsJson = JsonEntitySerializer.Serialize(template.Warnings),
                    ProvenanceJson = JsonEntitySerializer.Serialize(template.Provenance)
                },
                cancellationToken: CancellationToken.None));

        const string insertAssumption = """
                                        INSERT INTO dbo.GoldenManifestAssumptions (ManifestId, SortOrder, AssumptionText, TenantId, WorkspaceId, ProjectId)
                                        VALUES (@ManifestId, @SortOrder, @AssumptionText, @TenantId, @WorkspaceId, @ProjectId);
                                        """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertAssumption,
                new
                {
                    ManifestId = manifestId,
                    SortOrder = 0,
                    AssumptionText = "relational-assumption-wins",
                    TenantId,
                    WorkspaceId,
                    ProjectId
                },
                cancellationToken: CancellationToken.None));

        const string selectRow = """
                                 SELECT
                                     TenantId, WorkspaceId, ProjectId,
                                     ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                     CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                     MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                     ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                                     WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                                 FROM dbo.GoldenManifests
                                 WHERE ManifestId = @ManifestId;
                                 """;

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(selectRow, new { ManifestId = manifestId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Assumptions.Should().Equal("relational-assumption-wins");
    }

    [SkippableFact]
    public async Task HydrateAsync_prefers_relational_GoldenManifestWarnings_over_WarningsJson()
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
            "proj-gm-phase1-warnings",
            CancellationToken.None);

        ManifestDocument template = new()
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
            CreatedUtc = new DateTime(2026, 4, 14, 15, 0, 0, DateTimeKind.Utc),
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
            Assumptions = [],
            Warnings = ["json-fallback-should-not-win"],
            Provenance = new ManifestProvenance(),
            Decisions = []
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
                    template.TenantId,
                    template.WorkspaceId,
                    template.ProjectId,
                    template.ManifestId,
                    template.RunId,
                    template.ContextSnapshotId,
                    template.GraphSnapshotId,
                    template.FindingsSnapshotId,
                    template.DecisionTraceId,
                    template.CreatedUtc,
                    template.ManifestHash,
                    template.RuleSetId,
                    template.RuleSetVersion,
                    template.RuleSetHash,
                    MetadataJson = JsonEntitySerializer.Serialize(template.Metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(template.Requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(template.Topology),
                    SecurityJson = JsonEntitySerializer.Serialize(template.Security),
                    ComplianceJson = JsonEntitySerializer.Serialize(template.Compliance),
                    CostJson = JsonEntitySerializer.Serialize(template.Cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(template.Constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(template.UnresolvedIssues),
                    DecisionsJson = JsonEntitySerializer.Serialize(template.Decisions),
                    AssumptionsJson = JsonEntitySerializer.Serialize(template.Assumptions),
                    WarningsJson = JsonEntitySerializer.Serialize(template.Warnings),
                    ProvenanceJson = JsonEntitySerializer.Serialize(template.Provenance)
                },
                cancellationToken: CancellationToken.None));

        const string insertWarning = """
                                     INSERT INTO dbo.GoldenManifestWarnings (ManifestId, SortOrder, WarningText)
                                     VALUES (@ManifestId, @SortOrder, @WarningText);
                                     """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertWarning,
                new { ManifestId = manifestId, SortOrder = 0, WarningText = "relational-warning-wins" },
                cancellationToken: CancellationToken.None));

        const string selectRow = """
                                 SELECT
                                     TenantId, WorkspaceId, ProjectId,
                                     ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                     CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                     MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                     ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                                     WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                                 FROM dbo.GoldenManifests
                                 WHERE ManifestId = @ManifestId;
                                 """;

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(selectRow, new { ManifestId = manifestId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Warnings.Should().Equal("relational-warning-wins");
    }

    [SkippableFact]
    public async Task HydrateAsync_prefers_relational_provenance_source_findings_over_ProvenanceJson()
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
            "proj-gm-phase1-prov",
            CancellationToken.None);

        ManifestProvenance jsonOnlyProvenance = new()
        {
            SourceFindingIds = ["json-fallback-should-not-win"],
            SourceGraphNodeIds = ["n-json"],
            AppliedRuleIds = ["r-json"]
        };

        ManifestDocument template = new()
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
            CreatedUtc = new DateTime(2026, 4, 14, 16, 0, 0, DateTimeKind.Utc),
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
            Assumptions = [],
            Warnings = [],
            Provenance = jsonOnlyProvenance,
            Decisions = []
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
                    template.TenantId,
                    template.WorkspaceId,
                    template.ProjectId,
                    template.ManifestId,
                    template.RunId,
                    template.ContextSnapshotId,
                    template.GraphSnapshotId,
                    template.FindingsSnapshotId,
                    template.DecisionTraceId,
                    template.CreatedUtc,
                    template.ManifestHash,
                    template.RuleSetId,
                    template.RuleSetVersion,
                    template.RuleSetHash,
                    MetadataJson = JsonEntitySerializer.Serialize(template.Metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(template.Requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(template.Topology),
                    SecurityJson = JsonEntitySerializer.Serialize(template.Security),
                    ComplianceJson = JsonEntitySerializer.Serialize(template.Compliance),
                    CostJson = JsonEntitySerializer.Serialize(template.Cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(template.Constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(template.UnresolvedIssues),
                    DecisionsJson = JsonEntitySerializer.Serialize(template.Decisions),
                    AssumptionsJson = JsonEntitySerializer.Serialize(template.Assumptions),
                    WarningsJson = JsonEntitySerializer.Serialize(template.Warnings),
                    ProvenanceJson = JsonEntitySerializer.Serialize(template.Provenance)
                },
                cancellationToken: CancellationToken.None));

        const string insertProvFinding = """
                                         INSERT INTO dbo.GoldenManifestProvenanceSourceFindings (ManifestId, SortOrder, FindingId)
                                         VALUES (@ManifestId, @SortOrder, @FindingId);
                                         """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertProvFinding,
                new { ManifestId = manifestId, SortOrder = 0, FindingId = "relational-provenance-finding" },
                cancellationToken: CancellationToken.None));

        const string selectRow = """
                                 SELECT
                                     TenantId, WorkspaceId, ProjectId,
                                     ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                     CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                     MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                     ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                                     WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                                 FROM dbo.GoldenManifests
                                 WHERE ManifestId = @ManifestId;
                                 """;

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(selectRow, new { ManifestId = manifestId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Provenance.SourceFindingIds.Should().Equal("relational-provenance-finding");
        hydrated.Provenance.SourceGraphNodeIds.Should().BeEmpty();
        hydrated.Provenance.AppliedRuleIds.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task HydrateAsync_loads_relational_decisions_with_evidence_and_node_links_ordered()
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
            "proj-gm-phase1-decisions",
            CancellationToken.None);

        List<ResolvedArchitectureDecision> jsonDecisions =
        [
            new()
            {
                DecisionId = "json-should-not-win",
                Category = "x",
                Title = "x",
                SelectedOption = "x",
                Rationale = "x"
            }
        ];

        ManifestDocument template = new()
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
            CreatedUtc = new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc),
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
            Assumptions = [],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = jsonDecisions
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
                    template.TenantId,
                    template.WorkspaceId,
                    template.ProjectId,
                    template.ManifestId,
                    template.RunId,
                    template.ContextSnapshotId,
                    template.GraphSnapshotId,
                    template.FindingsSnapshotId,
                    template.DecisionTraceId,
                    template.CreatedUtc,
                    template.ManifestHash,
                    template.RuleSetId,
                    template.RuleSetVersion,
                    template.RuleSetHash,
                    MetadataJson = JsonEntitySerializer.Serialize(template.Metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(template.Requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(template.Topology),
                    SecurityJson = JsonEntitySerializer.Serialize(template.Security),
                    ComplianceJson = JsonEntitySerializer.Serialize(template.Compliance),
                    CostJson = JsonEntitySerializer.Serialize(template.Cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(template.Constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(template.UnresolvedIssues),
                    DecisionsJson = JsonEntitySerializer.Serialize(template.Decisions),
                    AssumptionsJson = JsonEntitySerializer.Serialize(template.Assumptions),
                    WarningsJson = JsonEntitySerializer.Serialize(template.Warnings),
                    ProvenanceJson = JsonEntitySerializer.Serialize(template.Provenance)
                },
                cancellationToken: CancellationToken.None));

        const string insertDecision = """
                                      INSERT INTO dbo.GoldenManifestDecisions
                                      (ManifestId, SortOrder, DecisionId, Category, Title, SelectedOption, Rationale, RawDecisionJson)
                                      VALUES (@ManifestId, @SortOrder, @DecisionId, @Category, @Title, @SelectedOption, @Rationale, @RawDecisionJson);
                                      """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertDecision,
                new
                {
                    ManifestId = manifestId,
                    SortOrder = 0,
                    DecisionId = "dec-rel-1",
                    Category = "Security",
                    Title = "Use TLS",
                    SelectedOption = "Enforce",
                    Rationale = "Because",
                    RawDecisionJson = (string?)"{\"k\":1}"
                },
                cancellationToken: CancellationToken.None));

        const string insertEvidence = """
                                      INSERT INTO dbo.GoldenManifestDecisionEvidenceLinks (ManifestId, DecisionId, SortOrder, FindingId)
                                      VALUES (@ManifestId, @DecisionId, @SortOrder, @FindingId);
                                      """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertEvidence,
                new { ManifestId = manifestId, DecisionId = "dec-rel-1", SortOrder = 0, FindingId = "f-ev-2" },
                cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertEvidence,
                new { ManifestId = manifestId, DecisionId = "dec-rel-1", SortOrder = 1, FindingId = "f-ev-1" },
                cancellationToken: CancellationToken.None));

        const string insertNodeLink = """
                                      INSERT INTO dbo.GoldenManifestDecisionNodeLinks (ManifestId, DecisionId, SortOrder, NodeId)
                                      VALUES (@ManifestId, @DecisionId, @SortOrder, @NodeId);
                                      """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertNodeLink,
                new { ManifestId = manifestId, DecisionId = "dec-rel-1", SortOrder = 0, NodeId = "node-b" },
                cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertNodeLink,
                new { ManifestId = manifestId, DecisionId = "dec-rel-1", SortOrder = 1, NodeId = "node-a" },
                cancellationToken: CancellationToken.None));

        const string selectRow = """
                                 SELECT
                                     TenantId, WorkspaceId, ProjectId,
                                     ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                     CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                     MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                     ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                                     WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                                 FROM dbo.GoldenManifests
                                 WHERE ManifestId = @ManifestId;
                                 """;

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(selectRow, new { ManifestId = manifestId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Decisions.Should().ContainSingle();
        ResolvedArchitectureDecision d = hydrated.Decisions[0];
        d.DecisionId.Should().Be("dec-rel-1");
        d.Category.Should().Be("Security");
        d.Title.Should().Be("Use TLS");
        d.SelectedOption.Should().Be("Enforce");
        d.Rationale.Should().Be("Because");
        d.RawDecisionJson.Should().Be("{\"k\":1}");
        d.SupportingFindingIds.Should().Equal("f-ev-2", "f-ev-1");
        d.RelatedNodeIds.Should().Equal("node-b", "node-a");
    }

    [SkippableFact]
    public async Task HydrateAsync_loads_relational_decisions_without_evidence_or_node_links()
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
            "proj-gm-phase1-decisions-no-links",
            CancellationToken.None);

        List<ResolvedArchitectureDecision> jsonDecisions =
        [
            new()
            {
                DecisionId = "json-should-not-win",
                Category = "x",
                Title = "x",
                SelectedOption = "x",
                Rationale = "x"
            }
        ];

        ManifestDocument template = new()
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
            CreatedUtc = new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc),
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
            Assumptions = [],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = jsonDecisions
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
                    template.TenantId,
                    template.WorkspaceId,
                    template.ProjectId,
                    template.ManifestId,
                    template.RunId,
                    template.ContextSnapshotId,
                    template.GraphSnapshotId,
                    template.FindingsSnapshotId,
                    template.DecisionTraceId,
                    template.CreatedUtc,
                    template.ManifestHash,
                    template.RuleSetId,
                    template.RuleSetVersion,
                    template.RuleSetHash,
                    MetadataJson = JsonEntitySerializer.Serialize(template.Metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(template.Requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(template.Topology),
                    SecurityJson = JsonEntitySerializer.Serialize(template.Security),
                    ComplianceJson = JsonEntitySerializer.Serialize(template.Compliance),
                    CostJson = JsonEntitySerializer.Serialize(template.Cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(template.Constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(template.UnresolvedIssues),
                    DecisionsJson = JsonEntitySerializer.Serialize(template.Decisions),
                    AssumptionsJson = JsonEntitySerializer.Serialize(template.Assumptions),
                    WarningsJson = JsonEntitySerializer.Serialize(template.Warnings),
                    ProvenanceJson = JsonEntitySerializer.Serialize(template.Provenance)
                },
                cancellationToken: CancellationToken.None));

        const string insertDecision = """
                                      INSERT INTO dbo.GoldenManifestDecisions
                                      (ManifestId, SortOrder, DecisionId, Category, Title, SelectedOption, Rationale, RawDecisionJson)
                                      VALUES (@ManifestId, @SortOrder, @DecisionId, @Category, @Title, @SelectedOption, @Rationale, @RawDecisionJson);
                                      """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertDecision,
                new
                {
                    ManifestId = manifestId,
                    SortOrder = 0,
                    DecisionId = "dec-no-links",
                    Category = "Cost",
                    Title = "Cap spend",
                    SelectedOption = "10k",
                    Rationale = "Budget",
                    RawDecisionJson = (string?)"{}"
                },
                cancellationToken: CancellationToken.None));

        const string selectRow = """
                                 SELECT
                                     TenantId, WorkspaceId, ProjectId,
                                     ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                     CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                     MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                     ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                                     WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                                 FROM dbo.GoldenManifests
                                 WHERE ManifestId = @ManifestId;
                                 """;

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(selectRow, new { ManifestId = manifestId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Decisions.Should().ContainSingle();
        ResolvedArchitectureDecision d = hydrated.Decisions[0];
        d.DecisionId.Should().Be("dec-no-links");
        d.SupportingFindingIds.Should().BeEmpty();
        d.RelatedNodeIds.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task HydrateAsync_loads_relational_provenance_applied_rules_only_other_slices_empty()
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
            "proj-gm-phase1-prov-rules-only",
            CancellationToken.None);

        ManifestProvenance jsonProvenance = new()
        {
            SourceFindingIds = ["json-f"],
            SourceGraphNodeIds = ["json-n"],
            AppliedRuleIds = ["json-r-should-not-win"]
        };

        ManifestDocument template = new()
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
            CreatedUtc = new DateTime(2026, 4, 15, 13, 0, 0, DateTimeKind.Utc),
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
            Assumptions = [],
            Warnings = [],
            Provenance = jsonProvenance,
            Decisions = []
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
                    template.TenantId,
                    template.WorkspaceId,
                    template.ProjectId,
                    template.ManifestId,
                    template.RunId,
                    template.ContextSnapshotId,
                    template.GraphSnapshotId,
                    template.FindingsSnapshotId,
                    template.DecisionTraceId,
                    template.CreatedUtc,
                    template.ManifestHash,
                    template.RuleSetId,
                    template.RuleSetVersion,
                    template.RuleSetHash,
                    MetadataJson = JsonEntitySerializer.Serialize(template.Metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(template.Requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(template.Topology),
                    SecurityJson = JsonEntitySerializer.Serialize(template.Security),
                    ComplianceJson = JsonEntitySerializer.Serialize(template.Compliance),
                    CostJson = JsonEntitySerializer.Serialize(template.Cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(template.Constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(template.UnresolvedIssues),
                    DecisionsJson = JsonEntitySerializer.Serialize(template.Decisions),
                    AssumptionsJson = JsonEntitySerializer.Serialize(template.Assumptions),
                    WarningsJson = JsonEntitySerializer.Serialize(template.Warnings),
                    ProvenanceJson = JsonEntitySerializer.Serialize(template.Provenance)
                },
                cancellationToken: CancellationToken.None));

        const string insertRule = """
                                  INSERT INTO dbo.GoldenManifestProvenanceAppliedRules (ManifestId, SortOrder, RuleId)
                                  VALUES (@ManifestId, @SortOrder, @RuleId);
                                  """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRule,
                new { ManifestId = manifestId, SortOrder = 0, RuleId = "relational-rule-only" },
                cancellationToken: CancellationToken.None));

        const string selectRow = """
                                 SELECT
                                     TenantId, WorkspaceId, ProjectId,
                                     ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                     CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                     MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                     ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                                     WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                                 FROM dbo.GoldenManifests
                                 WHERE ManifestId = @ManifestId;
                                 """;

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(selectRow, new { ManifestId = manifestId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Provenance.SourceFindingIds.Should().BeEmpty();
        hydrated.Provenance.SourceGraphNodeIds.Should().BeEmpty();
        hydrated.Provenance.AppliedRuleIds.Should().Equal("relational-rule-only");
    }

    [SkippableFact]
    public async Task HydrateAsync_loads_relational_provenance_nodes_and_rules_without_source_findings_rows()
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
            "proj-gm-phase1-prov-nodes-rules",
            CancellationToken.None);

        ManifestProvenance jsonProvenance = new()
        {
            SourceFindingIds = ["json-f"], SourceGraphNodeIds = ["json-n"], AppliedRuleIds = ["json-r"]
        };

        ManifestDocument template = new()
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
            CreatedUtc = new DateTime(2026, 4, 15, 11, 0, 0, DateTimeKind.Utc),
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
            Assumptions = [],
            Warnings = [],
            Provenance = jsonProvenance,
            Decisions = []
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
                    template.TenantId,
                    template.WorkspaceId,
                    template.ProjectId,
                    template.ManifestId,
                    template.RunId,
                    template.ContextSnapshotId,
                    template.GraphSnapshotId,
                    template.FindingsSnapshotId,
                    template.DecisionTraceId,
                    template.CreatedUtc,
                    template.ManifestHash,
                    template.RuleSetId,
                    template.RuleSetVersion,
                    template.RuleSetHash,
                    MetadataJson = JsonEntitySerializer.Serialize(template.Metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(template.Requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(template.Topology),
                    SecurityJson = JsonEntitySerializer.Serialize(template.Security),
                    ComplianceJson = JsonEntitySerializer.Serialize(template.Compliance),
                    CostJson = JsonEntitySerializer.Serialize(template.Cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(template.Constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(template.UnresolvedIssues),
                    DecisionsJson = JsonEntitySerializer.Serialize(template.Decisions),
                    AssumptionsJson = JsonEntitySerializer.Serialize(template.Assumptions),
                    WarningsJson = JsonEntitySerializer.Serialize(template.Warnings),
                    ProvenanceJson = JsonEntitySerializer.Serialize(template.Provenance)
                },
                cancellationToken: CancellationToken.None));

        const string insertNode = """
                                  INSERT INTO dbo.GoldenManifestProvenanceSourceGraphNodes (ManifestId, SortOrder, NodeId)
                                  VALUES (@ManifestId, @SortOrder, @NodeId);
                                  """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertNode,
                new { ManifestId = manifestId, SortOrder = 0, NodeId = "prov-node-rel" },
                cancellationToken: CancellationToken.None));

        const string insertRule = """
                                  INSERT INTO dbo.GoldenManifestProvenanceAppliedRules (ManifestId, SortOrder, RuleId)
                                  VALUES (@ManifestId, @SortOrder, @RuleId);
                                  """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRule,
                new { ManifestId = manifestId, SortOrder = 0, RuleId = "prov-rule-rel" },
                cancellationToken: CancellationToken.None));

        const string selectRow = """
                                 SELECT
                                     TenantId, WorkspaceId, ProjectId,
                                     ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                     CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                     MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                     ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                                     WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                                 FROM dbo.GoldenManifests
                                 WHERE ManifestId = @ManifestId;
                                 """;

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(selectRow, new { ManifestId = manifestId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Provenance.SourceFindingIds.Should().BeEmpty();
        hydrated.Provenance.SourceGraphNodeIds.Should().Equal("prov-node-rel");
        hydrated.Provenance.AppliedRuleIds.Should().Equal("prov-rule-rel");
    }

    [SkippableFact]
    public async Task HydrateAsync_falls_back_to_AssumptionsJson_when_no_relational_assumption_rows()
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
            "proj-gm-phase1-assumptions-json-fallback",
            CancellationToken.None);

        ManifestDocument template = CreateEmptySliceTemplate(
            manifestId,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc));
        template.Assumptions = ["json-assumption-a", "json-assumption-b"];

        await InsertGoldenManifestRowAsync(connection, template);

        GoldenManifestStorageRow? row = await QueryGoldenManifestRowAsync(connection, manifestId);

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Assumptions.Should().Equal("json-assumption-a", "json-assumption-b");
    }

    [SkippableFact]
    public async Task HydrateAsync_falls_back_to_ProvenanceJson_when_no_relational_provenance_rows()
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
            "proj-gm-phase1-provenance-json-fallback",
            CancellationToken.None);

        ManifestDocument template = CreateEmptySliceTemplate(
            manifestId,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            new DateTime(2026, 4, 15, 12, 5, 0, DateTimeKind.Utc));
        template.Provenance = new ManifestProvenance
        {
            SourceFindingIds = ["json-prov-f1"],
            SourceGraphNodeIds = ["json-prov-n1"],
            AppliedRuleIds = ["json-prov-r1"]
        };

        await InsertGoldenManifestRowAsync(connection, template);

        GoldenManifestStorageRow? row = await QueryGoldenManifestRowAsync(connection, manifestId);

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Provenance.SourceFindingIds.Should().Equal("json-prov-f1");
        hydrated.Provenance.SourceGraphNodeIds.Should().Equal("json-prov-n1");
        hydrated.Provenance.AppliedRuleIds.Should().Equal("json-prov-r1");
    }

    [SkippableFact]
    public async Task HydrateAsync_falls_back_to_DecisionsJson_when_no_relational_decision_rows()
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
            "proj-gm-phase1-decisions-json-fallback",
            CancellationToken.None);

        ResolvedArchitectureDecision jsonDecision = new()
        {
            DecisionId = "json-decision-only",
            Category = "Cost",
            Title = "Cap spend",
            SelectedOption = "Yes",
            Rationale = "Budget",
            SupportingFindingIds = ["jf1"],
            RelatedNodeIds = ["jn1"],
            RawDecisionJson = "{\"source\":\"json\"}"
        };

        ManifestDocument template = CreateEmptySliceTemplate(
            manifestId,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            new DateTime(2026, 4, 15, 12, 10, 0, DateTimeKind.Utc));
        template.Decisions = [jsonDecision];

        await InsertGoldenManifestRowAsync(connection, template);

        GoldenManifestStorageRow? row = await QueryGoldenManifestRowAsync(connection, manifestId);

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Decisions.Should().ContainSingle();
        ResolvedArchitectureDecision d = hydrated.Decisions[0];
        d.DecisionId.Should().Be("json-decision-only");
        d.Category.Should().Be("Cost");
        d.Title.Should().Be("Cap spend");
        d.SelectedOption.Should().Be("Yes");
        d.Rationale.Should().Be("Budget");
        d.SupportingFindingIds.Should().Equal("jf1");
        d.RelatedNodeIds.Should().Equal("jn1");
        d.RawDecisionJson.Should().Be("{\"source\":\"json\"}");
    }

    private static ManifestDocument CreateEmptySliceTemplate(
        Guid manifestId,
        Guid runId,
        Guid contextId,
        Guid graphId,
        Guid findingsId,
        Guid traceId,
        DateTime createdUtc)
    {
        return new ManifestDocument
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
            CreatedUtc = createdUtc,
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
            Assumptions = [],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = []
        };
    }

    private static async Task InsertGoldenManifestRowAsync(SqlConnection connection, ManifestDocument template)
    {
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
                    template.TenantId,
                    template.WorkspaceId,
                    template.ProjectId,
                    template.ManifestId,
                    template.RunId,
                    template.ContextSnapshotId,
                    template.GraphSnapshotId,
                    template.FindingsSnapshotId,
                    template.DecisionTraceId,
                    template.CreatedUtc,
                    template.ManifestHash,
                    template.RuleSetId,
                    template.RuleSetVersion,
                    template.RuleSetHash,
                    MetadataJson = JsonEntitySerializer.Serialize(template.Metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(template.Requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(template.Topology),
                    SecurityJson = JsonEntitySerializer.Serialize(template.Security),
                    ComplianceJson = JsonEntitySerializer.Serialize(template.Compliance),
                    CostJson = JsonEntitySerializer.Serialize(template.Cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(template.Constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(template.UnresolvedIssues),
                    DecisionsJson = JsonEntitySerializer.Serialize(template.Decisions),
                    AssumptionsJson = JsonEntitySerializer.Serialize(template.Assumptions),
                    WarningsJson = JsonEntitySerializer.Serialize(template.Warnings),
                    ProvenanceJson = JsonEntitySerializer.Serialize(template.Provenance)
                },
                cancellationToken: CancellationToken.None));
    }

    private static async Task<GoldenManifestStorageRow?> QueryGoldenManifestRowAsync(
        SqlConnection connection,
        Guid manifestId)
    {
        const string selectRow = """
                                 SELECT
                                     TenantId, WorkspaceId, ProjectId,
                                     ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                     CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                     MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                     ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                                     WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                                 FROM dbo.GoldenManifests
                                 WHERE ManifestId = @ManifestId;
                                 """;

        return await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(selectRow, new { ManifestId = manifestId },
                cancellationToken: CancellationToken.None));
    }
}
