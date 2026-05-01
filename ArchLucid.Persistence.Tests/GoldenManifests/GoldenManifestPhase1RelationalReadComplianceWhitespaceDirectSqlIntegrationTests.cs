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
///     Covers <see cref="GoldenManifestPhase1RelationalRead.HydrateAsync" /> <c>DeserializeCompliance</c> when
///     <c>ComplianceJson</c> is whitespace-only (default <see cref="ComplianceSection" />), independent of relational
///     slices.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "Core")]
public sealed class GoldenManifestPhase1RelationalReadComplianceWhitespaceDirectSqlIntegrationTests(
    SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task HydrateAsync_whitespace_ComplianceJson_yields_empty_ComplianceSection()
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
            "proj-gm-compliance-ws",
            CancellationToken.None);

        ManifestMetadata metadata = new();
        RequirementsCoverageSection requirements = new();
        TopologySection topology = new();
        SecuritySection security = new();
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

        DateTime createdUtc = new(2026, 4, 23, 12, 0, 0, DateTimeKind.Utc);

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
                    ComplianceJson = " \t\r\n ",
                    CostJson = JsonEntitySerializer.Serialize(cost),
                    ConstraintsJson = JsonEntitySerializer.Serialize(constraints),
                    UnresolvedIssuesJson = JsonEntitySerializer.Serialize(unresolved),
                    DecisionsJson = JsonEntitySerializer.Serialize(new List<ResolvedArchitectureDecision>()),
                    AssumptionsJson = JsonEntitySerializer.Serialize(new List<string>()),
                    WarningsJson = JsonEntitySerializer.Serialize(new List<string>()),
                    ProvenanceJson = JsonEntitySerializer.Serialize(new ManifestProvenance())
                },
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

        hydrated.Compliance.Should().NotBeNull();
        hydrated.Compliance.Gaps.Should().BeEmpty();
    }
}
