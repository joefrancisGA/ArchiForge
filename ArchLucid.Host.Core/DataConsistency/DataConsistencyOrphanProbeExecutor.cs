using System.Data.Common;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence.Data.Infrastructure;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.DataConsistency;

/// <summary>
/// One-shot orphan counts + optional dry-run samples (same logic as <see cref="Hosted.DataConsistencyOrphanProbeHostedService"/> loop body).
/// </summary>
public sealed class DataConsistencyOrphanProbeExecutor(
    IOptionsMonitor<DataConsistencyProbeOptions> optionsMonitor,
    IOptionsMonitor<DataConsistencyEnforcementOptions> enforcementOptionsMonitor,
    IDbConnectionFactory connectionFactory,
    IOptions<ArchLucidOptions> archLucidOptions,
    ILogger<DataConsistencyOrphanProbeExecutor> logger) : IDataConsistencyOrphanProbeExecutor
{
    private readonly IOptionsMonitor<DataConsistencyProbeOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private readonly IOptionsMonitor<DataConsistencyEnforcementOptions> _enforcementOptionsMonitor =
        enforcementOptionsMonitor ?? throw new ArgumentNullException(nameof(enforcementOptionsMonitor));

    private readonly IDbConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private readonly IOptions<ArchLucidOptions> _archLucidOptions =
        archLucidOptions ?? throw new ArgumentNullException(nameof(archLucidOptions));

    private readonly ILogger<DataConsistencyOrphanProbeExecutor> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>When storage is in-memory, returns immediately without opening SQL.</summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (ArchLucidOptions.EffectiveIsInMemory(_archLucidOptions.Value.StorageProvider))
            return;


        DataConsistencyProbeOptions snapshot = _optionsMonitor.CurrentValue;

        if (!snapshot.OrphanProbeEnabled)
            return;


        int sampleCap = Math.Clamp(snapshot.OrphanProbeRemediationDryRunLogMaxRows, 0, 500);

        DbConnection connection = (DbConnection)_connectionFactory.CreateConnection();
        await using DbConnection _ = connection;
        await connection.OpenAsync(cancellationToken);

        long leftCount = await LogAndCountOrphansAsync(
                connection,
                DataConsistencyOrphanProbeSql.ComparisonRecordsLeftRunId,
                "ComparisonRecords",
                "LeftRunId",
                cancellationToken)
            .ConfigureAwait(false);
        long rightCount = await LogAndCountOrphansAsync(
                connection,
                DataConsistencyOrphanProbeSql.ComparisonRecordsRightRunId,
                "ComparisonRecords",
                "RightRunId",
                cancellationToken)
            .ConfigureAwait(false);
        long goldenCount = await LogAndCountOrphansAsync(
                connection,
                DataConsistencyOrphanProbeSql.GoldenManifestsRunId,
                "GoldenManifests",
                "RunId",
                cancellationToken)
            .ConfigureAwait(false);
        long findingsCount = await LogAndCountOrphansAsync(
                connection,
                DataConsistencyOrphanProbeSql.FindingsSnapshotsRunId,
                "FindingsSnapshots",
                "RunId",
                cancellationToken)
            .ConfigureAwait(false);

        await ApplyEnforcementAsync(connection, leftCount, rightCount, goldenCount, findingsCount, cancellationToken)
            .ConfigureAwait(false);

        if (sampleCap <= 0)
            return;


        bool anyOrphans = leftCount > 0 || rightCount > 0 || goldenCount > 0 || findingsCount > 0;

        if (!anyOrphans)
            return;


        await LogRemediationDryRunSamplesAsync(connection, sampleCap, leftCount, rightCount, goldenCount, findingsCount, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ApplyEnforcementAsync(
        DbConnection connection,
        long leftCount,
        long rightCount,
        long goldenCount,
        long findingsCount,
        CancellationToken ct)
    {
        DataConsistencyEnforcementOptions enf = _enforcementOptionsMonitor.CurrentValue;

        if (enf.Mode == DataConsistencyEnforcementMode.Off)
            return;


        int threshold = Math.Max(1, enf.AlertThreshold);

        if (enf.Mode == DataConsistencyEnforcementMode.Alert || enf.Mode == DataConsistencyEnforcementMode.Quarantine)
        {
            TryRecordAlert(leftCount, threshold, "ComparisonRecords", "LeftRunId");
            TryRecordAlert(rightCount, threshold, "ComparisonRecords", "RightRunId");
            TryRecordAlert(goldenCount, threshold, "GoldenManifests", "RunId");
            TryRecordAlert(findingsCount, threshold, "FindingsSnapshots", "RunId");
        }

        if (goldenCount <= 0)
            return;


        bool quarantineGoldenManifests =
            enf.Mode == DataConsistencyEnforcementMode.Quarantine || enf.AutoQuarantine;

        if (!quarantineGoldenManifests)
            return;


        int cap = Math.Clamp(enf.MaxRowsPerBatch, 1, 5000);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = DataConsistencyEnforcementSql.InsertOrphanGoldenManifestsMissingRun;
        DbParameter maxRowsParameter = command.CreateParameter();
        maxRowsParameter.ParameterName = "@MaxRows";
        maxRowsParameter.Value = cap;
        command.Parameters.Add(maxRowsParameter);

        int inserted = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        if (inserted > 0)
        {
            ArchLucidInstrumentation.DataConsistencyOrphansQuarantined.Add(
                inserted,
                new KeyValuePair<string, object?>("table", "GoldenManifests"),
                new KeyValuePair<string, object?>("column", "RunId"));

            _logger.LogWarning("Data consistency quarantine inserted {Inserted} orphan GoldenManifests row(s).", inserted);
        }
    }

    private static void TryRecordAlert(long count, int threshold, string table, string column)
    {
        if (count < threshold)
            return;


        ArchLucidInstrumentation.DataConsistencyAlerts.Add(
            1,
            new KeyValuePair<string, object?>("table", table),
            new KeyValuePair<string, object?>("column", column));
    }

    private async Task LogRemediationDryRunSamplesAsync(
        DbConnection connection,
        int maxRows,
        long leftCount,
        long rightCount,
        long goldenCount,
        long findingsCount,
        CancellationToken ct)
    {
        if (leftCount > 0 || rightCount > 0)
        {
            IReadOnlyList<string> ids = await ReadTopOrphanComparisonRecordIdsAsync(connection, maxRows, ct).ConfigureAwait(false);

            if (ids.Count > 0)

                _logger.LogInformation(
                    "Data consistency orphan remediation dry-run (probe, no delete): ComparisonRecords sample (top {MaxRows}): {Ids}",
                    maxRows,
                    string.Join(", ", ids));

        }

        if (goldenCount > 0)
        {
            IReadOnlyList<string> ids = await ReadTopOrphanGoldenManifestIdsAsync(connection, maxRows, ct).ConfigureAwait(false);

            if (ids.Count > 0)

                _logger.LogInformation(
                    "Data consistency orphan remediation dry-run (probe, no delete): GoldenManifests sample (top {MaxRows}): {Ids}",
                    maxRows,
                    string.Join(", ", ids));

        }

        if (findingsCount > 0)
        {
            IReadOnlyList<string> ids = await ReadTopOrphanFindingsSnapshotIdsAsync(connection, maxRows, ct).ConfigureAwait(false);

            if (ids.Count > 0)

                _logger.LogInformation(
                    "Data consistency orphan remediation dry-run (probe, no delete): FindingsSnapshots sample (top {MaxRows}): {Ids}",
                    maxRows,
                    string.Join(", ", ids));

        }
    }

    private static async Task<IReadOnlyList<string>> ReadTopOrphanComparisonRecordIdsAsync(
        DbConnection connection,
        int maxRows,
        CancellationToken ct)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = DataConsistencyOrphanRemediationSql.SelectOrphanComparisonRecordIds;
        DbParameter maxRowsParameter = command.CreateParameter();
        maxRowsParameter.ParameterName = "@MaxRows";
        maxRowsParameter.Value = maxRows;
        command.Parameters.Add(maxRowsParameter);

        List<string> ids = [];

        await using DbDataReader reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

        while (await reader.ReadAsync(ct).ConfigureAwait(false))

            ids.Add(reader.GetString(0));


        return ids;
    }

    private static async Task<IReadOnlyList<string>> ReadTopOrphanGoldenManifestIdsAsync(
        DbConnection connection,
        int maxRows,
        CancellationToken ct)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = DataConsistencyOrphanRemediationSql.SelectOrphanGoldenManifestIds;
        DbParameter maxRowsParameter = command.CreateParameter();
        maxRowsParameter.ParameterName = "@MaxRows";
        maxRowsParameter.Value = maxRows;
        command.Parameters.Add(maxRowsParameter);

        List<string> ids = [];

        await using DbDataReader reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

        while (await reader.ReadAsync(ct).ConfigureAwait(false))

            ids.Add(reader.GetGuid(0).ToString("D", System.Globalization.CultureInfo.InvariantCulture));


        return ids;
    }

    private static async Task<IReadOnlyList<string>> ReadTopOrphanFindingsSnapshotIdsAsync(
        DbConnection connection,
        int maxRows,
        CancellationToken ct)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = DataConsistencyOrphanRemediationSql.SelectOrphanFindingsSnapshotIds;
        DbParameter maxRowsParameter = command.CreateParameter();
        maxRowsParameter.ParameterName = "@MaxRows";
        maxRowsParameter.Value = maxRows;
        command.Parameters.Add(maxRowsParameter);

        List<string> ids = [];

        await using DbDataReader reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

        while (await reader.ReadAsync(ct).ConfigureAwait(false))

            ids.Add(reader.GetGuid(0).ToString("D", System.Globalization.CultureInfo.InvariantCulture));


        return ids;
    }

    private async Task<long> LogAndCountOrphansAsync(
        DbConnection connection,
        string sql,
        string tableMetricLabel,
        string columnLabel,
        CancellationToken ct)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? scalar = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        long count = scalar is long l ? l : Convert.ToInt64(scalar ?? 0L, System.Globalization.CultureInfo.InvariantCulture);

        if (count <= 0)
            return count;


        _logger.LogWarning(
            "Data consistency: {Count} row(s) in {Table} reference a missing authority RunId ({Column}).",
            count,
            tableMetricLabel,
            columnLabel);

        ArchLucidInstrumentation.DataConsistencyOrphansDetected.Add(
            count,
            new KeyValuePair<string, object?>("table", tableMetricLabel),
            new KeyValuePair<string, object?>("column", columnLabel));

        return count;
    }
}
