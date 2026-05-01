using System.Data.Common;
using System.Diagnostics;
using System.Globalization;

using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.DataConsistency;

public sealed class DataConsistencyReconciliationService(
    IDbConnectionFactory connectionFactory,
    IRunRepository runRepository,
    IArchLucidStorageMode storageMode,
    ILogger<DataConsistencyReconciliationService> logger) : IDataConsistencyReconciliationService
{
    private readonly IDbConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IArchLucidStorageMode _storageMode =
        storageMode ?? throw new ArgumentNullException(nameof(storageMode));

    private readonly ILogger<DataConsistencyReconciliationService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<DataConsistencyReport> RunReconciliationAsync(CancellationToken cancellationToken)
    {
        DateTime checkedAt = DateTime.UtcNow;
        Stopwatch sw = Stopwatch.StartNew();

        if (_storageMode.IsInMemory)
        {
            IReadOnlyList<DataConsistencyFinding> findings =
            [
                new(
                    "reconciliation_skipped_in_memory",
                    DataConsistencyFindingSeverity.Info,
                    "Relational reconciliation skipped: host storage is InMemory.",
                    [])
            ];

            DataConsistencyReport skipped = new(checkedAt, findings, IsHealthy(findings));
            RecordInstrumentation(sw.Elapsed.TotalMilliseconds, findings);
            return skipped;
        }

        List<DataConsistencyFinding> list = [];

        await using (DbConnection connection = OpenConnection())
        {
            await connection.OpenAsync(cancellationToken);

            await AddCountAndFindingsAsync(
                    connection,
                    DataConsistencyReconciliationSql.ComparisonRecordsLeftRunId,
                    "orphan_comparison_records_left_run",
                    "ComparisonRecords reference a LeftRunId with no dbo.Runs row.",
                    DataConsistencyReconciliationSql.SampleComparisonRecordsLeftOrphans,
                    list,
                    cancellationToken)
                .ConfigureAwait(false);

            await AddCountAndFindingsAsync(
                    connection,
                    DataConsistencyReconciliationSql.ComparisonRecordsRightRunId,
                    "orphan_comparison_records_right_run",
                    "ComparisonRecords reference a RightRunId with no dbo.Runs row.",
                    DataConsistencyReconciliationSql.SampleComparisonRecordsRightOrphans,
                    list,
                    cancellationToken)
                .ConfigureAwait(false);

            await AddCountAndFindingsAsync(
                    connection,
                    DataConsistencyReconciliationSql.RunsMissingArchitectureRequest,
                    "runs_missing_architecture_request",
                    "Non-archived Runs reference an ArchitectureRequestId missing from dbo.ArchitectureRequests.",
                    DataConsistencyReconciliationSql.SampleRunsMissingArchitectureRequest,
                    list,
                    cancellationToken)
                .ConfigureAwait(false);

            await AddCountAndFindingsAsync(
                    connection,
                    DataConsistencyReconciliationSql.GoldenManifestsRunId,
                    "orphan_golden_manifests_run",
                    "GoldenManifests reference a RunId with no dbo.Runs row.",
                    DataConsistencyReconciliationSql.SampleGoldenManifestOrphans,
                    list,
                    cancellationToken)
                .ConfigureAwait(false);

            await AddCountAndFindingsAsync(
                    connection,
                    DataConsistencyReconciliationSql.FindingsSnapshotsRunId,
                    "orphan_findings_snapshots_run",
                    "FindingsSnapshots reference a RunId with no dbo.Runs row.",
                    DataConsistencyReconciliationSql.SampleFindingsSnapshotOrphans,
                    list,
                    cancellationToken)
                .ConfigureAwait(false);

            await AddCountAndFindingsAsync(
                    connection,
                    DataConsistencyReconciliationSql.ArtifactBundlesRunId,
                    "orphan_artifact_bundles_run",
                    "ArtifactBundles reference a RunId with no dbo.Runs row.",
                    DataConsistencyReconciliationSql.SampleArtifactBundleOrphans,
                    list,
                    cancellationToken)
                .ConfigureAwait(false);

            await AddStaleRunFindingsAsync(connection, list, cancellationToken).ConfigureAwait(false);

            await AddCacheVersusDatabaseFindingsAsync(connection, list, cancellationToken).ConfigureAwait(false);
        }

        DataConsistencyReport report = new(checkedAt, list, IsHealthy(list));
        RecordInstrumentation(sw.Elapsed.TotalMilliseconds, list);

        if (_logger.IsEnabled(LogLevel.Information))

            _logger.LogInformation(
                "Data consistency reconciliation finished in {ElapsedMs} ms; healthy={IsHealthy}; findings={FindingCount}.",
                sw.ElapsedMilliseconds,
                report.IsHealthy,
                list.Count);

        return report;
    }

    private DbConnection OpenConnection() => (DbConnection)_connectionFactory.CreateConnection();

    private static bool IsHealthy(IReadOnlyList<DataConsistencyFinding> findings) =>
        findings.All(f => f.Severity == DataConsistencyFindingSeverity.Info);

    private static void RecordInstrumentation(double elapsedMs, IReadOnlyList<DataConsistencyFinding> findings)
    {
        ArchLucidInstrumentation.DataConsistencyReconciliationDurationMilliseconds.Record(elapsedMs);

        foreach (DataConsistencyFinding f in findings)
        {
            KeyValuePair<string, object?>[] tags =
            [
                new("severity", f.Severity.ToString()),
                new("check_name", f.CheckName)
            ];

            ArchLucidInstrumentation.DataConsistencyReconciliationFindingsTotal.Add(1, tags);
        }
    }

    private async Task AddCountAndFindingsAsync(
        DbConnection connection,
        string countSql,
        string checkName,
        string description,
        string sampleSql,
        List<DataConsistencyFinding> findings,
        CancellationToken ct)
    {
        long count = await ExecuteCountAsync(connection, countSql, ct).ConfigureAwait(false);

        if (count <= 0)
            return;


        IReadOnlyList<string> ids = await ReadStringColumnAsync(connection, sampleSql, ct).ConfigureAwait(false);

        findings.Add(
            new DataConsistencyFinding(
                checkName,
                DataConsistencyFindingSeverity.Warning,
                $"{description} Count={count}.",
                ids));

        if (_logger.IsEnabled(LogLevel.Warning))

            _logger.LogWarning(
                "Data consistency [{CheckName}]: {Description}. Count={Count}.",
                checkName,
                description,
                count);
    }

    private async Task AddStaleRunFindingsAsync(
        DbConnection connection,
        List<DataConsistencyFinding> findings,
        CancellationToken ct)
    {
        long count = await ExecuteCountAsync(connection, DataConsistencyReconciliationSql.StaleInFlightRuns, ct)
            .ConfigureAwait(false);

        if (count <= 0)
            return;


        IReadOnlyList<string> ids =
            await ReadStringColumnAsync(connection, DataConsistencyReconciliationSql.SampleStaleRunIds, ct)
                .ConfigureAwait(false);

        findings.Add(
            new DataConsistencyFinding(
                "stale_in_flight_runs",
                DataConsistencyFindingSeverity.Warning,
                "Runs stayed in Created/TasksGenerated/WaitingForResults/Retrying for more than 1 hour. " +
                "Treated as operational risk (not exactly 'Pending/Executing' legacy labels).",
                ids));

        if (_logger.IsEnabled(LogLevel.Warning))

            _logger.LogWarning("Data consistency [stale_in_flight_runs]: count={Count}.", count);
    }

    private async Task AddCacheVersusDatabaseFindingsAsync(
        DbConnection connection,
        List<DataConsistencyFinding> findings,
        CancellationToken ct)
    {
        List<RecentRunRow> rows = [];

        await using (DbCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = DataConsistencyReconciliationSql.RecentRunsForCacheSample;
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                rows.Add(
                    new RecentRunRow(
                        reader.GetGuid(0),
                        reader.GetGuid(1),
                        reader.GetGuid(2),
                        reader.GetGuid(3),
                        reader.IsDBNull(4) ? null : reader.GetString(4),
                        reader.IsDBNull(5) ? null : reader.GetString(5),
                        reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                        reader.GetDateTime(7)));
            }
        }

        List<string> mismatched = [];

        foreach (RecentRunRow row in rows)
        {
            ScopeContext scope = new()
            {
                TenantId = row.TenantId,
                WorkspaceId = row.WorkspaceId,
                ProjectId = row.ScopeProjectId
            };

            RunRecord? cached = await _runRepository.GetByIdAsync(scope, row.RunId, ct).ConfigureAwait(false);

            if (cached is null)
            {
                mismatched.Add($"{row.RunId:D}|cache_miss");

                continue;
            }


            if (!string.Equals(NormalizeStatus(cached.LegacyRunStatus), NormalizeStatus(row.LegacyRunStatus), StringComparison.Ordinal))
                mismatched.Add($"{row.RunId:D}|legacy_status");

            if (!string.Equals(
                    NormalizeVersion(cached.CurrentManifestVersion),
                    NormalizeVersion(row.CurrentManifestVersion),
                    StringComparison.Ordinal))
                mismatched.Add($"{row.RunId:D}|manifest_version");

            if (!NullableUtcEquals(cached.CompletedUtc, row.CompletedUtc))
                mismatched.Add($"{row.RunId:D}|completed_utc");
        }

        if (mismatched.Count <= 0)
            return;


        findings.Add(
            new DataConsistencyFinding(
                "run_cache_database_divergence",
                DataConsistencyFindingSeverity.Critical,
                "Hot-path run cache diverged from dbo.Runs on a sample of the 10 most recently created non-archived runs.",
                mismatched));

        if (_logger.IsEnabled(LogLevel.Error))

            _logger.LogError(
                "Data consistency [run_cache_database_divergence]: mismatches={MismatchCount} on sampled rows.",
                mismatched.Count);
    }

    private static bool NullableUtcEquals(DateTime? a, DateTime? b)
    {
        if (!a.HasValue && !b.HasValue)
            return true;

        if (a.HasValue != b.HasValue)
            return false;

        return a!.Value.ToUniversalTime() == b!.Value.ToUniversalTime();
    }

    private static string NormalizeStatus(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    private static string NormalizeVersion(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    private static async Task<long> ExecuteCountAsync(DbConnection connection, string sql, CancellationToken ct)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? scalar = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);

        return scalar is long l
            ? l
            : Convert.ToInt64(scalar ?? 0L, CultureInfo.InvariantCulture);
    }

    private static async Task<IReadOnlyList<string>> ReadStringColumnAsync(
        DbConnection connection,
        string sql,
        CancellationToken ct)
    {
        List<string> ids = [];

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await using DbDataReader reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            ids.Add(reader.GetString(0));

        return ids;
    }

    private readonly record struct RecentRunRow(
        Guid TenantId,
        Guid WorkspaceId,
        Guid ScopeProjectId,
        Guid RunId,
        string? LegacyRunStatus,
        string? CurrentManifestVersion,
        DateTime? CompletedUtc,
        [UsedImplicitly] DateTime CreatedUtc);
}
