using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Repositories;

using Dapper;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Persistence.Backfill;

/// <summary>
/// Scans authority tables for JSON-only rows, hydrates domain models (same paths as repositories), and inserts
/// missing relational slices. Safe to re-run: each slice insert is skipped when child rows already exist.
/// </summary>
public sealed class SqlRelationalBackfillService(
    ISqlConnectionFactory connectionFactory,
    SqlContextSnapshotRepository contextSnapshotRepository,
    SqlGraphSnapshotRepository graphSnapshotRepository,
    SqlFindingsSnapshotRepository findingsSnapshotRepository,
    SqlGoldenManifestRepository goldenManifestRepository,
    SqlArtifactBundleRepository artifactBundleRepository,
    ILogger<SqlRelationalBackfillService> logger) : ISqlRelationalBackfillService
{
    public async Task<SqlRelationalBackfillReport> RunAsync(SqlRelationalBackfillOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);
        SqlRelationalBackfillReport report = new();

        if (options.ContextSnapshots)
            await BackfillContextSnapshotsAsync(report, ct);

        if (options.GraphSnapshots)
            await BackfillGraphSnapshotsAsync(report, ct);

        if (options.FindingsSnapshots)
            await BackfillFindingsSnapshotsAsync(report, ct);

        if (options.GoldenManifestsPhase1)
            await BackfillGoldenManifestsAsync(report, ct);

        if (options.ArtifactBundles)
            await BackfillArtifactBundlesAsync(report, ct);

        return report;
    }

    private async Task BackfillContextSnapshotsAsync(SqlRelationalBackfillReport report, CancellationToken ct)
    {
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        List<Guid> ids = (await connection.QueryAsync<Guid>(
            new CommandDefinition(
                """
                SELECT SnapshotId
                FROM dbo.ContextSnapshots
                ORDER BY CreatedUtc;
                """,
                cancellationToken: ct))).ToList();

        foreach (Guid snapshotId in ids)
        {
            report.ProcessedCount++;

            try
            {
                await using SqlConnection conn = await connectionFactory.CreateOpenConnectionAsync(ct);
                await using SqlTransaction tx = conn.BeginTransaction();

                ContextSnapshot? snapshot = await contextSnapshotRepository.GetByIdAsync(snapshotId, conn, tx, ct);
                if (snapshot is null)
                {
                    tx.Commit();
                    continue;
                }

                await SqlContextSnapshotRepository.BackfillRelationalSlicesAsync(snapshot, conn, tx, ct);
                tx.Commit();
                report.SuccessCount++;
                logger.LogInformation("Backfill ContextSnapshots: completed {SnapshotId}", snapshotId);
            }
            catch (Exception ex)
            {
                report.FailureCount++;
                report.Failures.Add(
                    new SqlRelationalBackfillFailure
                    {
                        Stage = "ContextSnapshots",
                        EntityKey = snapshotId.ToString(),
                        Message = ex.Message,
                    });
                logger.LogError(ex, "Backfill ContextSnapshots: failed {SnapshotId}", snapshotId);
            }
        }
    }

    private async Task BackfillGraphSnapshotsAsync(SqlRelationalBackfillReport report, CancellationToken ct)
    {
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        List<Guid> ids = (await connection.QueryAsync<Guid>(
            new CommandDefinition(
                """
                SELECT GraphSnapshotId
                FROM dbo.GraphSnapshots
                ORDER BY CreatedUtc;
                """,
                cancellationToken: ct))).ToList();

        foreach (Guid graphSnapshotId in ids)
        {
            report.ProcessedCount++;

            try
            {
                await using SqlConnection conn = await connectionFactory.CreateOpenConnectionAsync(ct);
                await using SqlTransaction tx = conn.BeginTransaction();

                GraphSnapshot? snapshot = await graphSnapshotRepository.GetByIdAsync(graphSnapshotId, conn, tx, ct);
                if (snapshot is null)
                {
                    tx.Commit();
                    continue;
                }

                await SqlGraphSnapshotRepository.BackfillRelationalSlicesAsync(snapshot, conn, tx, ct);
                tx.Commit();
                report.SuccessCount++;
                logger.LogInformation("Backfill GraphSnapshots: completed {GraphSnapshotId}", graphSnapshotId);
            }
            catch (Exception ex)
            {
                report.FailureCount++;
                report.Failures.Add(
                    new SqlRelationalBackfillFailure
                    {
                        Stage = "GraphSnapshots",
                        EntityKey = graphSnapshotId.ToString(),
                        Message = ex.Message,
                    });
                logger.LogError(ex, "Backfill GraphSnapshots: failed {GraphSnapshotId}", graphSnapshotId);
            }
        }
    }

    private async Task BackfillFindingsSnapshotsAsync(SqlRelationalBackfillReport report, CancellationToken ct)
    {
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        List<Guid> ids = (await connection.QueryAsync<Guid>(
            new CommandDefinition(
                """
                SELECT FindingsSnapshotId
                FROM dbo.FindingsSnapshots
                ORDER BY CreatedUtc;
                """,
                cancellationToken: ct))).ToList();

        foreach (Guid findingsSnapshotId in ids)
        {
            report.ProcessedCount++;

            try
            {
                FindingsSnapshot? snapshot = await findingsSnapshotRepository.GetByIdAsync(findingsSnapshotId, ct);
                if (snapshot is null)
                    continue;

                await using SqlConnection conn = await connectionFactory.CreateOpenConnectionAsync(ct);
                await using SqlTransaction tx = conn.BeginTransaction();

                int recordCount = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        SELECT COUNT(1)
                        FROM dbo.FindingRecords
                        WHERE FindingsSnapshotId = @FindingsSnapshotId;
                        """,
                        new
                        {
                            snapshot.FindingsSnapshotId,
                        },
                        tx,
                        cancellationToken: ct));

                if (recordCount > 0)
                {
                    tx.Commit();
                    continue;
                }

                await SqlFindingsSnapshotRepository.BackfillRelationalSlicesAsync(snapshot, conn, tx, ct);
                tx.Commit();
                report.SuccessCount++;
                logger.LogInformation("Backfill FindingsSnapshots: completed {FindingsSnapshotId}", findingsSnapshotId);
            }
            catch (Exception ex)
            {
                report.FailureCount++;
                report.Failures.Add(
                    new SqlRelationalBackfillFailure
                    {
                        Stage = "FindingsSnapshots",
                        EntityKey = findingsSnapshotId.ToString(),
                        Message = ex.Message,
                    });
                logger.LogError(ex, "Backfill FindingsSnapshots: failed {FindingsSnapshotId}", findingsSnapshotId);
            }
        }
    }

    private async Task BackfillGoldenManifestsAsync(SqlRelationalBackfillReport report, CancellationToken ct)
    {
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        List<(Guid ManifestId, Guid TenantId, Guid WorkspaceId, Guid ProjectId)> rows =
            (await connection.QueryAsync<(Guid ManifestId, Guid TenantId, Guid WorkspaceId, Guid ProjectId)>(
                new CommandDefinition(
                    """
                    SELECT ManifestId, TenantId, WorkspaceId, ProjectId
                    FROM dbo.GoldenManifests
                    ORDER BY CreatedUtc;
                    """,
                    cancellationToken: ct))).ToList();

        foreach ((Guid manifestId, Guid tenantId, Guid workspaceId, Guid projectId) in rows)
        {
            report.ProcessedCount++;

            try
            {
                ScopeContext scope = new()
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                };

                GoldenManifest? manifest = await goldenManifestRepository.GetByIdAsync(scope, manifestId, ct);
                if (manifest is null)
                    continue;

                await using SqlConnection conn = await connectionFactory.CreateOpenConnectionAsync(ct);
                await using SqlTransaction tx = conn.BeginTransaction();

                await SqlGoldenManifestRepository.BackfillPhase1RelationalSlicesAsync(manifest, conn, tx, ct);
                tx.Commit();
                report.SuccessCount++;
                logger.LogInformation("Backfill GoldenManifests: completed {ManifestId}", manifestId);
            }
            catch (Exception ex)
            {
                report.FailureCount++;
                report.Failures.Add(
                    new SqlRelationalBackfillFailure
                    {
                        Stage = "GoldenManifestsPhase1",
                        EntityKey = manifestId.ToString(),
                        Message = ex.Message,
                    });
                logger.LogError(ex, "Backfill GoldenManifests: failed {ManifestId}", manifestId);
            }
        }
    }

    private async Task BackfillArtifactBundlesAsync(SqlRelationalBackfillReport report, CancellationToken ct)
    {
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        List<Guid> ids = (await connection.QueryAsync<Guid>(
            new CommandDefinition(
                """
                SELECT BundleId
                FROM dbo.ArtifactBundles
                ORDER BY CreatedUtc;
                """,
                cancellationToken: ct))).ToList();

        foreach (Guid bundleId in ids)
        {
            report.ProcessedCount++;

            try
            {
                ArtifactBundle? bundle = await artifactBundleRepository.GetByBundleIdAsync(bundleId, ct);
                if (bundle is null)
                    continue;

                await using SqlConnection conn = await connectionFactory.CreateOpenConnectionAsync(ct);
                await using SqlTransaction tx = conn.BeginTransaction();

                await SqlArtifactBundleRepository.BackfillRelationalSlicesAsync(bundle, conn, tx, ct);
                tx.Commit();
                report.SuccessCount++;
                logger.LogInformation("Backfill ArtifactBundles: completed {BundleId}", bundleId);
            }
            catch (Exception ex)
            {
                report.FailureCount++;
                report.Failures.Add(
                    new SqlRelationalBackfillFailure
                    {
                        Stage = "ArtifactBundles",
                        EntityKey = bundleId.ToString(),
                        Message = ex.Message,
                    });
                logger.LogError(ex, "Backfill ArtifactBundles: failed {BundleId}", bundleId);
            }
        }
    }
}
