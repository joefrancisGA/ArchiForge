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
///     Relational <c>ORDER BY SortOrder</c> for
///     <see cref="GoldenManifestPhase1RelationalRead.LoadDecisionsRelationalAsync" /> when
///     rows are inserted with non-monotonic <c>SortOrder</c> (database ordering must win).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GoldenManifestPhase1RelationalReadOrderedDecisionsNonMonotonicInsertDirectSqlIntegrationTests(
    SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task HydrateAsync_orders_decisions_by_SortOrder_when_insert_order_differs()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
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
            "proj-gm-decisions-order",
            CancellationToken.None);

        ManifestMetadata metadata = new();
        RequirementsCoverageSection requirements = new();
        TopologySection topology = new();
        SecuritySection security = new();
        ComplianceSection compliance = new();
        CostSection cost = new();
        ConstraintSection constraints = new();
        UnresolvedIssuesSection unresolved = new();

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

        DateTime createdUtc = new(2026, 4, 23, 14, 0, 0, DateTimeKind.Utc);
        string noiseDecisionsJson =
            JsonEntitySerializer.Serialize(new List<ResolvedArchitectureDecision>
            {
                new()
                {
                    DecisionId = "json-noise",
                    Category = "c",
                    Title = "t",
                    SelectedOption = "o",
                    Rationale = "r"
                }
            });

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertManifest,
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
                    CreatedUtc = createdUtc,
                    ManifestHash = "h",
                    RuleSetId = "r",
                    RuleSetVersion = "1",
                    RuleSetHash = "rh",
                    MetadataJson = JsonEntitySerializer.Serialize(metadata),
                    RequirementsJson = JsonEntitySerializer.Serialize(requirements),
                    TopologyJson = JsonEntitySerializer.Serialize(topology),
                    SecurityJson = JsonEntitySerializer.Serialize(security),
                    ComplianceJson = JsonEntitySerializer.Serialize(compliance),
                    CostJson = JsonEntitySerializer.Serialize(cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(unresolved),
                    DecisionsJson = noiseDecisionsJson,
                    AssumptionsJson = JsonEntitySerializer.Serialize(new List<string>()),
                    WarningsJson = JsonEntitySerializer.Serialize(new List<string>()),
                    ProvenanceJson = JsonEntitySerializer.Serialize(new ManifestProvenance())
                },
                cancellationToken: CancellationToken.None));

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO dbo.GoldenManifestDecisions
                (ManifestId, SortOrder, DecisionId, Category, Title, SelectedOption, Rationale, RawDecisionJson)
                VALUES (@M, 1, N'd-second', N'c', N't2', N'o', N'r', NULL);
                INSERT INTO dbo.GoldenManifestDecisions
                (ManifestId, SortOrder, DecisionId, Category, Title, SelectedOption, Rationale, RawDecisionJson)
                VALUES (@M, 0, N'd-first', N'c', N't1', N'o', N'r', NULL);
                INSERT INTO dbo.GoldenManifestDecisionEvidenceLinks (ManifestId, DecisionId, SortOrder, FindingId)
                VALUES (@M, N'd-second', 0, N'f-second');
                INSERT INTO dbo.GoldenManifestDecisionNodeLinks (ManifestId, DecisionId, SortOrder, NodeId)
                VALUES (@M, N'd-first', 0, N'n-first');
                """,
                new { M = manifestId },
                cancellationToken: CancellationToken.None));

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(
                """
                SELECT
                    TenantId, WorkspaceId, ProjectId,
                    ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                    CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                    MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                    ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson,
                    WarningsJson, ProvenanceJson, ManifestPayloadBlobUri
                FROM dbo.GoldenManifests WHERE ManifestId = @ManifestId;
                """,
                new { ManifestId = manifestId },
                cancellationToken: CancellationToken.None));

        row.Should().NotBeNull();

        ManifestDocument hydrated =
            await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Decisions.Should().HaveCount(2);
        hydrated.Decisions[0].DecisionId.Should().Be("d-first");
        hydrated.Decisions[0].RelatedNodeIds.Should().ContainSingle().Which.Should().Be("n-first");
        hydrated.Decisions[0].SupportingFindingIds.Should().BeEmpty();
        hydrated.Decisions[1].DecisionId.Should().Be("d-second");
        hydrated.Decisions[1].SupportingFindingIds.Should().ContainSingle().Which.Should().Be("f-second");
        hydrated.Decisions[1].RelatedNodeIds.Should().BeEmpty();
    }
}
