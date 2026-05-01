using ArchLucid.Core.Hosting;
using ArchLucid.Core.Integration;
using ArchLucid.Persistence;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.DataConsistency;

/// <summary>Schedules read-only data consistency reconciliation on the leader host.</summary>
public sealed class DataConsistencyReconciliationHostedService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<DataConsistencyReconciliationOptions> optionsMonitor,
    ILeaderElectionWorkRunner electionWorkRunner,
    DataConsistencyReconciliationHealthState healthState,
    IIntegrationEventPublisher integrationEventPublisher,
    IOptionsMonitor<IntegrationEventsOptions> integrationEventsOptions,
    ILogger<DataConsistencyReconciliationHostedService> logger) : BackgroundService
{
    /// <summary>Must stay aligned with Host leader lease name <c>hosted:data-consistency-reconciliation</c>.</summary>
    private const string LeaderLeaseName = "hosted:data-consistency-reconciliation";

    /// <summary>Sentinel tenancy for platform-scope reconciliation events (no single tenant/workspace).</summary>
    private static readonly Guid PlatformScopeSentinelTenantId = Guid.Empty;

    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly IOptionsMonitor<DataConsistencyReconciliationOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private readonly ILeaderElectionWorkRunner _electionWorkRunner =
        electionWorkRunner ?? throw new ArgumentNullException(nameof(electionWorkRunner));

    private readonly DataConsistencyReconciliationHealthState _healthState =
        healthState ?? throw new ArgumentNullException(nameof(healthState));

    private readonly IIntegrationEventPublisher _integrationEventPublisher =
        integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));

    private readonly IOptionsMonitor<IntegrationEventsOptions> _integrationEventsOptions =
        integrationEventsOptions ?? throw new ArgumentNullException(nameof(integrationEventsOptions));

    private readonly ILogger<DataConsistencyReconciliationHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _electionWorkRunner.RunLeaderWorkAsync(
            LeaderLeaseName,
            LoopAsync,
            stoppingToken);
    }

    private async Task LoopAsync(CancellationToken leaderToken)
    {
        try
        {
            int initialSeconds = Math.Clamp(_optionsMonitor.CurrentValue.InitialDelaySeconds, 0, 600);

            await Task.Delay(TimeSpan.FromSeconds(initialSeconds), leaderToken);
        }
        catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
        {
            return;
        }

        while (!leaderToken.IsCancellationRequested)
        {
            int minutes = Math.Clamp(_optionsMonitor.CurrentValue.ReconciliationIntervalMinutes, 15, 24 * 60);

            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IDataConsistencyReconciliationService reconciliation =
                    scope.ServiceProvider.GetRequiredService<IDataConsistencyReconciliationService>();

                DataConsistencyReport report = await reconciliation.RunReconciliationAsync(leaderToken)
                    .ConfigureAwait(false);

                _healthState.RecordSuccess(report);
                await TryPublishCompletedEventAsync(report, leaderToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _healthState.RecordFailure(ex);

                if (_logger.IsEnabled(LogLevel.Warning))

                    _logger.LogWarning(ex, "Data consistency reconciliation iteration failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(minutes), leaderToken);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task TryPublishCompletedEventAsync(DataConsistencyReport report, CancellationToken ct)
    {
        try
        {
            // Downstream consumers: bounded payload (ids truncated per finding).
            object payload = new
            {
                report.CheckedAtUtc,
                report.IsHealthy,
                Findings = report.Findings.Select(
                        f => new
                        {
                            f.CheckName,
                            Severity = f.Severity.ToString(),
                            f.Description,
                            AffectedEntityIds = f.AffectedEntityIds.Take(50).ToArray()
                        })
                    .ToArray()
            };

            string messageId = $"data-consistency-check:{report.CheckedAtUtc:o}";

            using IServiceScope scope = _scopeFactory.CreateScope();
            IIntegrationEventOutboxRepository outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutboxRepository>();

            await OutboxAwareIntegrationEventPublishing.TryPublishOrEnqueueAsync(
                    outbox,
                    _integrationEventPublisher,
                    _integrationEventsOptions.CurrentValue,
                    _logger,
                    IntegrationEventTypes.DataConsistencyCheckCompletedV1,
                    payload,
                    messageId,
                    runId: null,
                    PlatformScopeSentinelTenantId,
                    PlatformScopeSentinelTenantId,
                    PlatformScopeSentinelTenantId,
                    connection: null,
                    transaction: null,
                    ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(ex, "Failed to publish data consistency reconciliation integration event.");
        }
    }
}
