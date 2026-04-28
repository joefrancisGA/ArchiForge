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
///     Covers <see cref="GoldenManifestPhase1RelationalRead.HydrateAsync" /> relational
///     <c>dbo.GoldenManifestAssumptions</c> ordering: <c>ORDER BY SortOrder</c> when multiple rows exist.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GoldenManifestPhase1RelationalReadOrderedAssumptionsDirectSqlIntegrationTests(
    SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [SkippableFact]
    public async Task HydrateAsync_returns_assumptions_in_SortOrder_when_multiple_relational_rows()
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
            "proj-gm-assumptions-order",
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
            CreatedUtc = new DateTime(2026, 4, 17, 10, 0, 0, DateTimeKind.Utc),
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
            Assumptions = ["json-order-should-not-win"],
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
                    SortOrder = 1,
                    AssumptionText = "second-by-sort-order",
                    TenantId,
                    WorkspaceId,
                    ProjectId
                },
                cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertAssumption,
                new
                {
                    ManifestId = manifestId,
                    SortOrder = 0,
                    AssumptionText = "first-by-sort-order",
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

        hydrated.Assumptions.Should().Equal("first-by-sort-order", "second-by-sort-order");
    }
}
