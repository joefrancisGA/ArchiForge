using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Coordination.Diagnostics;
using ArchLucid.Persistence.Tenancy.Diagnostics;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>
/// Periodically reads SQL outbox depths and publishes them to <see cref="ArchLucidInstrumentation.OutboxDepthGauges"/>
/// for Prometheus scrape (observable gauges read cached values).
/// </summary>
public sealed class OutboxOperationalMetricsHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxOperationalMetricsHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly ILogger<OutboxOperationalMetricsHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Outbox operational metrics collection failed; will retry.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task CollectOnceAsync(CancellationToken ct)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IOutboxOperationalMetricsReader? reader =
            scope.ServiceProvider.GetService<IOutboxOperationalMetricsReader>();

        if (reader is null)
        {
            return;
        }

        OutboxOperationalMetricsSnapshot snap = await reader.ReadSnapshotAsync(ct);

        ITrialFunnelOperationalMetricsReader? trialReader =
            scope.ServiceProvider.GetService<ITrialFunnelOperationalMetricsReader>();

        if (trialReader is not null)
        {
            long activeTrials = await trialReader.CountActiveSelfServiceTrialsAsync(ct);
            ArchLucidInstrumentation.PublishTrialActiveTenantCount(activeTrials);
        }

        OutboxDepthGaugeValues values = new(
            snap.AuthorityPipelineWorkPending,
            snap.AuthorityPipelineWorkOldestPendingAgeSeconds,
            snap.RetrievalIndexingOutboxPending,
            snap.RetrievalIndexingOutboxOldestPendingAgeSeconds,
            snap.IntegrationEventOutboxPublishPending,
            snap.IntegrationEventOutboxDeadLetter,
            snap.IntegrationEventOutboxOldestActionablePendingAgeSeconds);

        ArchLucidInstrumentation.OutboxDepthGauges.Publish(in values);
    }
}
