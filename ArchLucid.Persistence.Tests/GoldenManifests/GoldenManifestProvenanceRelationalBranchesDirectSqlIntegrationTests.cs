using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.GoldenManifests;
using ArchLucid.Persistence.Serialization;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.GoldenManifests;

/// <summary>
/// Branch coverage for <see cref="GoldenManifestPhase1RelationalRead.HydrateAsync"/> provenance paths:
/// relational rules-only (no source findings / nodes), and JSON fallback when no relational provenance rows.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class GoldenManifestProvenanceRelationalBranchesDirectSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [SkippableFact]
    public async Task HydrateAsync_provenance_relational_applied_rules_only_uses_empty_lists_for_findings_and_nodes()
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
            "proj-prov-rules-only",
            CancellationToken.None);

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
                N'{}', N'{}', N'{}', N'{}', N'{}', N'{}',
                N'{}', N'{}', N'[]', N'[]',
                N'[]', N'{"sourceFindingIds":[],"sourceGraphNodeIds":[],"appliedRuleIds":["should-not-win"]}'
            );
            """;

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
                    CreatedUtc = DateTime.UtcNow,
                    ManifestHash = "h",
                    RuleSetId = "r",
                    RuleSetVersion = "1",
                    RuleSetHash = "rh",
                },
                cancellationToken: CancellationToken.None));

        const string insertRule = """
            INSERT INTO dbo.GoldenManifestProvenanceAppliedRules (ManifestId, SortOrder, RuleId)
            VALUES (@ManifestId, 0, N'rule-rel-1');
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(insertRule, new
            {
                ManifestId = manifestId
            }, cancellationToken: CancellationToken.None));

        GoldenManifestStorageRow row = await QueryManifestRowAsync(connection, manifestId, CancellationToken.None);

        GoldenManifest hydrated = await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Provenance.AppliedRuleIds.Should().Equal("rule-rel-1");
        hydrated.Provenance.SourceFindingIds.Should().BeEmpty();
        hydrated.Provenance.SourceGraphNodeIds.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task HydrateAsync_provenance_total_count_zero_falls_back_to_ProvenanceJson()
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
            "proj-prov-json-fallback",
            CancellationToken.None);

        string provenanceJson = JsonEntitySerializer.Serialize(
            new ManifestProvenance
            {
                SourceFindingIds = ["f-json"],
                SourceGraphNodeIds = ["n-json"],
                AppliedRuleIds = ["r-json"],
            });

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
                N'{}', N'{}', N'{}', N'{}', N'{}', N'{}',
                N'{}', N'{}', N'[]', N'[]',
                N'[]', @ProvenanceJson
            );
            """;

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
                    CreatedUtc = DateTime.UtcNow,
                    ManifestHash = "h",
                    RuleSetId = "r",
                    RuleSetVersion = "1",
                    RuleSetHash = "rh",
                    ProvenanceJson = provenanceJson,
                },
                cancellationToken: CancellationToken.None));

        GoldenManifestStorageRow row = await QueryManifestRowAsync(connection, manifestId, CancellationToken.None);

        GoldenManifest hydrated = await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Provenance.SourceFindingIds.Should().ContainSingle().Which.Should().Be("f-json");
        hydrated.Provenance.SourceGraphNodeIds.Should().ContainSingle().Which.Should().Be("n-json");
        hydrated.Provenance.AppliedRuleIds.Should().ContainSingle().Which.Should().Be("r-json");
    }

    [SkippableFact]
    public async Task HydrateAsync_assumptions_count_zero_uses_AssumptionsJson_fallback()
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
            "proj-assum-json",
            CancellationToken.None);

        string assumptionsJson = JsonEntitySerializer.Serialize(new List<string> { "from-json-assumption" });

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
                N'{}', N'{}', N'{}', N'{}', N'{}', N'{}',
                N'{}', N'{}', N'[]', @AssumptionsJson,
                N'[]', N'{}'
            );
            """;

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
                    CreatedUtc = DateTime.UtcNow,
                    ManifestHash = "h",
                    RuleSetId = "r",
                    RuleSetVersion = "1",
                    RuleSetHash = "rh",
                    AssumptionsJson = assumptionsJson,
                },
                cancellationToken: CancellationToken.None));

        GoldenManifestStorageRow row = await QueryManifestRowAsync(connection, manifestId, CancellationToken.None);

        GoldenManifest hydrated = await GoldenManifestPhase1RelationalRead.HydrateAsync(connection, row, CancellationToken.None);

        hydrated.Assumptions.Should().Equal("from-json-assumption");
    }

    private static async Task<GoldenManifestStorageRow> QueryManifestRowAsync(
        SqlConnection connection,
        Guid manifestId,
        CancellationToken ct)
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

        GoldenManifestStorageRow? row = await connection.QuerySingleOrDefaultAsync<GoldenManifestStorageRow>(
            new CommandDefinition(selectRow, new
            {
                ManifestId = manifestId
            }, cancellationToken: ct));

        row.Should().NotBeNull();

        return row;
    }

}
