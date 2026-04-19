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
    IDbConnectionFactory connectionFactory,
    IOptions<ArchLucidOptions> archLucidOptions,
    ILogger<DataConsistencyOrphanProbeExecutor> logger) : IDataConsistencyOrphanProbeExecutor
{
    private readonly IOptionsMonitor<DataConsistencyProbeOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

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
        {
            return;
        }

        DataConsistencyProbeOptions snapshot = _optionsMonitor.CurrentValue;

        if (!snapshot.OrphanProbeEnabled)
        {
            return;
        }

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

        if (sampleCap <= 0)
        {
            return;
        }

        bool anyOrphans = leftCount > 0 || rightCount > 0 || goldenCount > 0 || findingsCount > 0;

        if (!anyOrphans)
        {
            return;
        }

        await LogRemediationDryRunSamplesAsync(connection, sampleCap, leftCount, rightCount, goldenCount, findingsCount, cancellationToken)
            .ConfigureAwait(false);
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
            {
                _logger.LogInformation(
                    "Data consistency orphan remediation dry-run (probe, no delete): ComparisonRecords sample (top {MaxRows}): {Ids}",
                    maxRows,
                    string.Join(", ", ids));
            }
        }

        if (goldenCount > 0)
        {
            IReadOnlyList<string> ids = await ReadTopOrphanGoldenManifestIdsAsync(connection, maxRows, ct).ConfigureAwait(false);

            if (ids.Count > 0)
            {
                _logger.LogInformation(
                    "Data consistency orphan remediation dry-run (probe, no delete): GoldenManifests sample (top {MaxRows}): {Ids}",
                    maxRows,
                    string.Join(", ", ids));
            }
        }

        if (findingsCount > 0)
        {
            IReadOnlyList<string> ids = await ReadTopOrphanFindingsSnapshotIdsAsync(connection, maxRows, ct).ConfigureAwait(false);

            if (ids.Count > 0)
            {
                _logger.LogInformation(
                    "Data consistency orphan remediation dry-run (probe, no delete): FindingsSnapshots sample (top {MaxRows}): {Ids}",
                    maxRows,
                    string.Join(", ", ids));
            }
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
        {
            ids.Add(reader.GetString(0));
        }

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
        {
            ids.Add(reader.GetGuid(0).ToString("D", System.Globalization.CultureInfo.InvariantCulture));
        }

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
        {
            ids.Add(reader.GetGuid(0).ToString("D", System.Globalization.CultureInfo.InvariantCulture));
        }

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
        {
            return count;
        }

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
