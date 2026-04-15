using System.Data.Common;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.DataConsistency;
using ArchLucid.Persistence.Data.Infrastructure;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>
/// Periodically counts <c>dbo.ComparisonRecords</c> rows whose <c>LeftRunId</c> parses as a GUID but has no matching <c>dbo.Runs</c> row (detection-only).
/// </summary>
public sealed class DataConsistencyOrphanProbeHostedService(
    IOptionsMonitor<DataConsistencyProbeOptions> optionsMonitor,
    IDbConnectionFactory connectionFactory,
    IOptions<ArchLucidOptions> archLucidOptions,
    ILogger<DataConsistencyOrphanProbeHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ArchLucidOptions.EffectiveIsInMemory(archLucidOptions.Value.StorageProvider))
        {
            return;
        }

        TimeSpan firstDelay = TimeSpan.FromMinutes(2);

        try
        {
            await Task.Delay(firstDelay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            DataConsistencyProbeOptions snapshot = optionsMonitor.CurrentValue;

            if (!snapshot.OrphanProbeEnabled)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);

                continue;
            }

            int minutes = Math.Clamp(snapshot.OrphanProbeIntervalMinutes, 5, 24 * 60);

            try
            {
                await ProbeOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Data consistency orphan probe failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ProbeOnceAsync(CancellationToken ct)
    {
        DbConnection connection = (DbConnection)connectionFactory.CreateConnection();
        await using DbConnection _ = connection;
        await connection.OpenAsync(ct);

        await LogAndCountOrphansAsync(
                connection,
                DataConsistencyOrphanProbeSql.ComparisonRecordsLeftRunId,
                "ComparisonRecords",
                "LeftRunId",
                ct)
            .ConfigureAwait(false);
        await LogAndCountOrphansAsync(
                connection,
                DataConsistencyOrphanProbeSql.ComparisonRecordsRightRunId,
                "ComparisonRecords",
                "RightRunId",
                ct)
            .ConfigureAwait(false);
        await LogAndCountOrphansAsync(
                connection,
                DataConsistencyOrphanProbeSql.GoldenManifestsRunId,
                "GoldenManifests",
                "RunId",
                ct)
            .ConfigureAwait(false);
        await LogAndCountOrphansAsync(
                connection,
                DataConsistencyOrphanProbeSql.FindingsSnapshotsRunId,
                "FindingsSnapshots",
                "RunId",
                ct)
            .ConfigureAwait(false);
    }

    private async Task LogAndCountOrphansAsync(
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
            return;
        }

        logger.LogWarning(
            "Data consistency: {Count} row(s) in {Table} reference a missing authority RunId ({Column}).",
            count,
            tableMetricLabel,
            columnLabel);

        ArchLucidInstrumentation.DataConsistencyOrphansDetected.Add(
            count,
            new KeyValuePair<string, object?>("table", tableMetricLabel),
            new KeyValuePair<string, object?>("column", columnLabel));
    }
}
