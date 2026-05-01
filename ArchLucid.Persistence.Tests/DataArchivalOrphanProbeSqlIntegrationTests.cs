using System.Globalization;

using ArchLucid.Persistence.Conversation;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     After run archival (<see cref="DataArchivalCoordinator" />), authority rows remain in <c>dbo.Runs</c> (soft
///     archive);
///     child rows keyed by <c>RunId</c> must not appear as orphans to the same probes as
///     <see cref="ArchLucid.Host.Core.Hosted.DataConsistencyOrphanProbeHostedService" />.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "SqlServer")]
public sealed class DataArchivalOrphanProbeSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    // Keep in sync with ArchLucid.Host.Core.DataConsistency.DataConsistencyOrphanRemediationSql.SelectOrphanFindingsSnapshotIds
    // (admin dry-run / remediation uses the same selection).
    private const string AdminStyleOrphanFindingsSnapshotSelect = """
                                                                  SELECT TOP (@MaxRows) f.FindingsSnapshotId
                                                                  FROM dbo.FindingsSnapshots f
                                                                  WHERE NOT EXISTS (
                                                                      SELECT 1
                                                                      FROM dbo.Runs r
                                                                      WHERE r.RunId = f.RunId)
                                                                    AND NOT EXISTS (
                                                                      SELECT 1
                                                                      FROM dbo.GoldenManifests g
                                                                      WHERE g.FindingsSnapshotId = f.FindingsSnapshotId)
                                                                  ORDER BY f.CreatedUtc ASC;
                                                                  """;

    // Keep in sync with ArchLucid.Host.Core.DataConsistency.DataConsistencyOrphanProbeSql.
    private const string ComparisonRecordsLeftRunId = """
                                                      SELECT COUNT_BIG(1)
                                                      FROM dbo.ComparisonRecords c
                                                      WHERE c.LeftRunId IS NOT NULL
                                                        AND TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId) IS NOT NULL
                                                        AND NOT EXISTS (
                                                            SELECT 1
                                                            FROM dbo.Runs r
                                                            WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId));
                                                      """;

    private const string ComparisonRecordsRightRunId = """
                                                       SELECT COUNT_BIG(1)
                                                       FROM dbo.ComparisonRecords c
                                                       WHERE c.RightRunId IS NOT NULL
                                                         AND TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId) IS NOT NULL
                                                         AND NOT EXISTS (
                                                             SELECT 1
                                                             FROM dbo.Runs r
                                                             WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId));
                                                       """;

    private const string GoldenManifestsRunId = """
                                                SELECT COUNT_BIG(1)
                                                FROM dbo.GoldenManifests g
                                                WHERE NOT EXISTS (
                                                    SELECT 1
                                                    FROM dbo.Runs r
                                                    WHERE r.RunId = g.RunId);
                                                """;

    private const string FindingsSnapshotsRunId = """
                                                  SELECT COUNT_BIG(1)
                                                  FROM dbo.FindingsSnapshots f
                                                  WHERE NOT EXISTS (
                                                      SELECT 1
                                                      FROM dbo.Runs r
                                                      WHERE r.RunId = f.RunId);
                                                  """;

    private static readonly Guid SeedTenantId = Guid.Parse("10101010-1010-1010-1010-101010101010");

    private static readonly Guid SeedWorkspaceId = Guid.Parse("20202020-2020-2020-2020-202020202020");

    private static readonly Guid SeedScopeProjectId = Guid.Parse("30303030-3030-3030-3030-303030303030");

    [SkippableFact]
    public async Task After_archival_child_rows_remain_consistent_with_probe_queries()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string runId = Guid.NewGuid().ToString("N");
        Guid runGuid = Guid.ParseExact(runId, "N");
        string requestId = "archival-orphan-req-" + runId;
        Guid manifestId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsSnapId = Guid.NewGuid();
        Guid decisionTraceId = Guid.NewGuid();

        await using (SqlConnection setup = new(fixture.ConnectionString))
        {
            await setup.OpenAsync(CancellationToken.None);
            await ArchitectureCommitTestSeed.InsertRequestAndRunAsync(setup, requestId, runId, CancellationToken.None);

            await AuthorityRunChainTestSeed.SeedSnapshotChainForExistingRunAsync(
                setup,
                SeedTenantId,
                SeedWorkspaceId,
                SeedScopeProjectId,
                runGuid,
                contextId,
                graphId,
                findingsSnapId,
                decisionTraceId,
                "ContractSeed",
                CancellationToken.None);

            const string insertManifest = """
                                          IF NOT EXISTS (SELECT 1 FROM dbo.GoldenManifests WHERE ManifestId = @ManifestId)
                                          INSERT INTO dbo.GoldenManifests
                                          (ManifestId, RunId, ContextSnapshotId, GraphSnapshotId, FindingsSnapshotId, DecisionTraceId,
                                           CreatedUtc, ManifestHash, RuleSetId, RuleSetVersion, RuleSetHash,
                                           MetadataJson, RequirementsJson, TopologyJson, SecurityJson, ComplianceJson, CostJson,
                                           ConstraintsJson, UnresolvedIssuesJson, DecisionsJson, AssumptionsJson, WarningsJson, ProvenanceJson,
                                           TenantId, WorkspaceId, ProjectId)
                                          VALUES
                                          (@ManifestId, @RunId, @ContextSnapshotId, @GraphSnapshotId, @FindingsSnapshotId, @DecisionTraceId,
                                           SYSUTCDATETIME(), N'h', N'rs', N'1', N'rh', N'{}', N'{}', N'{}', N'{}', N'{}', N'{}',
                                           N'{}', N'{}', N'{}', N'{}', N'{}', N'{}',
                                           @TenantId, @WorkspaceId, @ScopeProjectId);
                                          """;

            await setup.ExecuteAsync(
                new CommandDefinition(
                    insertManifest,
                    new
                    {
                        ManifestId = manifestId,
                        RunId = runGuid,
                        ContextSnapshotId = contextId,
                        GraphSnapshotId = graphId,
                        FindingsSnapshotId = findingsSnapId,
                        DecisionTraceId = decisionTraceId,
                        TenantId = SeedTenantId,
                        WorkspaceId = SeedWorkspaceId,
                        ScopeProjectId = SeedScopeProjectId
                    },
                    cancellationToken: CancellationToken.None));

            string comparisonId = "cmp-" + runId;

            const string insertComparison = """
                                            IF NOT EXISTS (SELECT 1 FROM dbo.ComparisonRecords WHERE ComparisonRecordId = @ComparisonRecordId)
                                            INSERT INTO dbo.ComparisonRecords
                                            (ComparisonRecordId, ComparisonType, LeftRunId, RightRunId, Format, PayloadJson, CreatedUtc)
                                            VALUES
                                            (@ComparisonRecordId, N'test', @LeftRunId, NULL, N'json', N'{}', SYSUTCDATETIME());
                                            """;

            await setup.ExecuteAsync(
                new CommandDefinition(
                    insertComparison,
                    new
                    {
                        ComparisonRecordId = comparisonId,
                        LeftRunId = runId
                    },
                    cancellationToken: CancellationToken.None));

            await setup.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE dbo.Runs SET CreatedUtc = DATEADD(day, -400, SYSUTCDATETIME()) WHERE RunId = @RunGuid;",
                    new
                    {
                        RunGuid = runGuid
                    },
                    cancellationToken: CancellationToken.None));
        }

        TestSqlConnectionFactory sqlFactory = new(fixture.ConnectionString);
        TestAuthorityRunListConnectionFactory listFactory = new(sqlFactory);
        SqlRunRepository runRepo = SqlRunRepositoryTestFactory.Create(sqlFactory, listFactory);
        DataArchivalCoordinator coordinator = new(
            runRepo,
            new InMemoryArchitectureDigestRepository(),
            new InMemoryConversationThreadRepository(),
            NullLogger<DataArchivalCoordinator>.Instance);

        await coordinator.RunOnceAsync(
            new DataArchivalOptions
            {
                RunsRetentionDays = 30,
                DigestsRetentionDays = 0,
                ConversationsRetentionDays = 0
            },
            CancellationToken.None);

        await using SqlConnection verify = new(fixture.ConnectionString);
        await verify.OpenAsync(CancellationToken.None);
        DateTime? archivedUtc = await verify.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(
                "SELECT ArchivedUtc FROM dbo.Runs WHERE RunId = @RunGuid;",
                new
                {
                    RunGuid = runGuid
                },
                cancellationToken: CancellationToken.None));

        archivedUtc.Should().NotBeNull();

        DateTime? manifestArchivedUtc = await verify.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(
                "SELECT ArchivedUtc FROM dbo.GoldenManifests WHERE ManifestId = @ManifestId;",
                new
                {
                    ManifestId = manifestId
                },
                cancellationToken: CancellationToken.None));

        manifestArchivedUtc.Should().NotBeNull("bulk run archival cascades ArchivedUtc to dbo.GoldenManifests");

        DateTime? findingsArchivedUtc = await verify.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(
                "SELECT ArchivedUtc FROM dbo.FindingsSnapshots WHERE FindingsSnapshotId = @FindingsSnapshotId;",
                new
                {
                    FindingsSnapshotId = findingsSnapId
                },
                cancellationToken: CancellationToken.None));

        findingsArchivedUtc.Should().NotBeNull("bulk run archival cascades ArchivedUtc to dbo.FindingsSnapshots");

        long leftOrphans = await ScalarCountAsync(verify, ComparisonRecordsLeftRunId, CancellationToken.None);
        long rightOrphans = await ScalarCountAsync(verify, ComparisonRecordsRightRunId, CancellationToken.None);
        long goldenOrphans = await ScalarCountAsync(verify, GoldenManifestsRunId, CancellationToken.None);
        long findingsOrphans = await ScalarCountAsync(verify, FindingsSnapshotsRunId, CancellationToken.None);

        leftOrphans.Should().Be(0L, "ComparisonRecords.LeftRunId probe after archival");
        rightOrphans.Should().Be(0L, "ComparisonRecords.RightRunId probe after archival");
        goldenOrphans.Should().Be(0L, "GoldenManifests.RunId probe after archival");
        findingsOrphans.Should().Be(0L, "FindingsSnapshots.RunId probe after archival");

        // Same rows the admin POST â€¦/orphan-findings-snapshots would list (not referenced by a golden manifest as orphan).
        IReadOnlyList<Guid> adminOrphanList = (await verify.QueryAsync<Guid>(
            new CommandDefinition(
                AdminStyleOrphanFindingsSnapshotSelect,
                new
                {
                    MaxRows = 100
                },
                cancellationToken: CancellationToken.None))).ToList();
        adminOrphanList.Should().BeEmpty("admin-style findings-snapshot orphan select after cascaded archival");
    }

    private static async Task<long> ScalarCountAsync(SqlConnection connection, string sql, CancellationToken ct)
    {
        object? scalar = await connection.ExecuteScalarAsync(new CommandDefinition(sql, cancellationToken: ct));

        return scalar is long l ? l : Convert.ToInt64(scalar ?? 0L, CultureInfo.InvariantCulture);
    }
}
