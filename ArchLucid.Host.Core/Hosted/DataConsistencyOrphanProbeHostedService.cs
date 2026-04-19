using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.DataConsistency;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>
/// Periodically counts orphan coordinator rows (comparison left/right run ids, golden manifests, findings snapshots)
/// against <c>dbo.Runs</c>, emits warnings + Prometheus counters (detection-only). Optionally logs admin-equivalent
/// <c>SELECT</c> samples when <see cref="Configuration.DataConsistencyProbeOptions.OrphanProbeRemediationDryRunLogMaxRows"/> is set; never deletes.
/// </summary>
public sealed class DataConsistencyOrphanProbeHostedService(
    IOptionsMonitor<DataConsistencyProbeOptions> optionsMonitor,
    DataConsistencyOrphanProbeExecutor executor,
    IOptions<ArchLucidOptions> archLucidOptions,
    ILogger<DataConsistencyOrphanProbeHostedService> logger) : BackgroundService
{
    private readonly DataConsistencyOrphanProbeExecutor _executor =
        executor ?? throw new ArgumentNullException(nameof(executor));

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
                await _executor.RunOnceAsync(stoppingToken).ConfigureAwait(false);
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
}
