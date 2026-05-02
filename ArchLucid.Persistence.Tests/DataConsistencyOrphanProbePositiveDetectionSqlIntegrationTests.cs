using System.Globalization;

using ArchLucid.Persistence.Tests.Support;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Probes that orphan detection SQL matches admin remediation semantics after forcing a manifest onto a bogus
///     <c>RunId</c> under <c>NOCHECK</c> when <c>FK_GoldenManifests_Runs_RunId</c> exists.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Suite", "SqlServer")]
public sealed class DataConsistencyOrphanProbePositiveDetectionSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    // Keep in sync with ArchLucid.Host.Core.DataConsistency.DataConsistencyOrphanProbeSql.GoldenManifestsRunId
    private const string GoldenManifestsRunIdProbe = """
                                                   SELECT COUNT_BIG(1)
                                                   FROM dbo.GoldenManifests g
                                                   WHERE NOT EXISTS (
                                                       SELECT 1
                                                       FROM dbo.Runs r
                                                       WHERE r.RunId = g.RunId);
                                                   """;

    // Keep in sync with ArchLucid.Host.Core.DataConsistency.DataConsistencyOrphanRemediationSql.SelectOrphanGoldenManifestIds
    private const string SelectOrphanGoldenManifestIds = """
                                                         SELECT TOP (@MaxRows) g.ManifestId
                                                         FROM dbo.GoldenManifests g
                                                         WHERE NOT EXISTS (
                                                             SELECT 1
                                                             FROM dbo.Runs r
                                                             WHERE r.RunId = g.RunId)
                                                         ORDER BY g.CreatedUtc ASC;
                                                         """;

    private static readonly Guid SeedTenantId = Guid.Parse("70707070-7070-7070-7070-707070707070");

    private static readonly Guid SeedWorkspaceId = Guid.Parse("71717171-7171-7171-7171-717171717171");

    private static readonly Guid SeedScopeProjectId = Guid.Parse("72727272-7272-7272-7272-727272727272");

    [SkippableFact]
    public async Task Probe_and_remediation_select_find_orphan_manifest_then_delete_clears_probe()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string runId = Guid.NewGuid().ToString("N");
        Guid runGuid = Guid.ParseExact(runId, "N");
        string requestId = "orphan-probe-pos-req-" + runId;
        Guid manifestId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid graphId = Guid.NewGuid();
        Guid findingsSnapId = Guid.NewGuid();
        Guid decisionTraceId = Guid.NewGuid();
        Guid bogusRunId = Guid.NewGuid();

        await using SqlConnection conn = new(fixture.ConnectionString);
        await conn.OpenAsync(CancellationToken.None);
        bool nchecked = false;

        try
        {
            await ArchitectureCommitTestSeed.InsertRequestAndRunAsync(conn, requestId, runId, CancellationToken.None);

            await AuthorityRunChainTestSeed.SeedSnapshotChainForExistingRunAsync(
                conn,
                SeedTenantId,
                SeedWorkspaceId,
                SeedScopeProjectId,
                runGuid,
                contextId,
                graphId,
                findingsSnapId,
                decisionTraceId,
                "OrphanPositiveProbeSeed",
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

            await conn.ExecuteAsync(
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

            object? fkRow = await conn.ExecuteScalarAsync(
                new CommandDefinition(
                    """
                    SELECT COUNT(1)
                    FROM sys.foreign_keys
                    WHERE name = N'FK_GoldenManifests_Runs_RunId'
                      AND parent_object_id = OBJECT_ID(N'dbo.GoldenManifests');
                    """,
                    cancellationToken: CancellationToken.None));

            int fkHits = fkRow is int i ? i : Convert.ToInt32(fkRow ?? 0, CultureInfo.InvariantCulture);

            if (fkHits > 0)
            {
                await conn.ExecuteAsync(
                    new CommandDefinition(
                        "ALTER TABLE dbo.GoldenManifests NOCHECK CONSTRAINT FK_GoldenManifests_Runs_RunId;",
                        cancellationToken: CancellationToken.None));

                nchecked = true;
            }

            await conn.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE dbo.GoldenManifests SET RunId = @BogusRunId WHERE ManifestId = @ManifestId;",
                    new
                    {
                        BogusRunId = bogusRunId,
                        ManifestId = manifestId
                    },
                    cancellationToken: CancellationToken.None));

            long thisOrphan = await conn.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    """
                    SELECT COUNT_BIG(1)
                    FROM dbo.GoldenManifests g
                    WHERE g.ManifestId = @ManifestId
                      AND NOT EXISTS (SELECT 1 FROM dbo.Runs r WHERE r.RunId = g.RunId);
                    """,
                    new
                    {
                        ManifestId = manifestId
                    },
                    cancellationToken: CancellationToken.None));

            thisOrphan.Should().Be(1L, "seeded manifest should be an orphan against dbo.Runs");

            long orphansAfter = await ScalarCountAsync(conn, GoldenManifestsRunIdProbe, CancellationToken.None);
            orphansAfter.Should().BeGreaterThanOrEqualTo(1L);

            IReadOnlyList<Guid> listed = (await conn.QueryAsync<Guid>(
                new CommandDefinition(
                    SelectOrphanGoldenManifestIds,
                    new
                    {
                        MaxRows = 50
                    },
                    cancellationToken: CancellationToken.None))).ToList();

            listed.Should().Contain(manifestId);

            await conn.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM dbo.ArtifactBundles WHERE ManifestId = @ManifestId;",
                    new
                    {
                        ManifestId = manifestId
                    },
                    cancellationToken: CancellationToken.None));

            await conn.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM dbo.GoldenManifests WHERE ManifestId = @ManifestId;",
                    new
                    {
                        ManifestId = manifestId
                    },
                    cancellationToken: CancellationToken.None));

            int remaining = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(1) FROM dbo.GoldenManifests WHERE ManifestId = @ManifestId;",
                    new
                    {
                        ManifestId = manifestId
                    },
                    cancellationToken: CancellationToken.None));

            remaining.Should().Be(0);

            long stillOrphanById = await conn.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    """
                    SELECT COUNT_BIG(1)
                    FROM dbo.GoldenManifests g
                    WHERE g.ManifestId = @ManifestId
                      AND NOT EXISTS (SELECT 1 FROM dbo.Runs r WHERE r.RunId = g.RunId);
                    """,
                    new
                    {
                        ManifestId = manifestId
                    },
                    cancellationToken: CancellationToken.None));

            stillOrphanById.Should().Be(0L);
        }
        finally
        {
            if (nchecked)
            {
                try
                {
                    await conn.ExecuteAsync(
                        new CommandDefinition(
                            """
                            ALTER TABLE dbo.GoldenManifests WITH CHECK CHECK CONSTRAINT FK_GoldenManifests_Runs_RunId;
                            """,
                            cancellationToken: CancellationToken.None));
                }
                catch
                {
                    // Best-effort: shared DB may still hold other brownfield orphans; tests should not fail teardown.
                }
            }
        }
    }

    private static async Task<long> ScalarCountAsync(SqlConnection connection, string sql, CancellationToken ct)
    {
        object? scalar = await connection.ExecuteScalarAsync(new CommandDefinition(sql, cancellationToken: ct));

        return scalar is long l ? l : Convert.ToInt64(scalar ?? 0L, CultureInfo.InvariantCulture);
    }
}
