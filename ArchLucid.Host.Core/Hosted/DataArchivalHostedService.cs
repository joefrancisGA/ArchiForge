using ArchLucid.Persistence.Archival;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>
/// Periodically applies <see cref="DataArchivalOptions"/> retention cutoffs via <see cref="IDataArchivalCoordinator"/>.
/// </summary>
/// <remarks>
/// When <c>HostLeaderElection:Enabled</c> is true and storage is SQL, only one worker replica runs the archival loop.
/// </remarks>
public sealed class DataArchivalHostedService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<DataArchivalOptions> optionsMonitor,
    ILogger<DataArchivalHostedService> logger,
    DataArchivalHostHealthState healthState,
    HostLeaderElectionCoordinator electionCoordinator) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly IOptionsMonitor<DataArchivalOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private readonly ILogger<DataArchivalHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly DataArchivalHostHealthState _healthState =
        healthState ?? throw new ArgumentNullException(nameof(healthState));

    private readonly HostLeaderElectionCoordinator _electionCoordinator =
        electionCoordinator ?? throw new ArgumentNullException(nameof(electionCoordinator));

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _electionCoordinator.RunLeaderWorkAsync(
            HostElectionLeaseNames.DataArchival,
            LoopAsync,
            stoppingToken);
    }

    private async Task LoopAsync(CancellationToken leaderToken)
    {
        while (!leaderToken.IsCancellationRequested)
        {
            DataArchivalOptions opts = _optionsMonitor.CurrentValue;
            TimeSpan delay = TimeSpan.FromHours(Math.Clamp(opts.IntervalHours, 1, 168));

            try
            {
                _ = await DataArchivalHostIteration.RunOnceAsync(
                    _scopeFactory,
                    opts,
                    _logger,
                    _healthState,
                    leaderToken);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(delay, leaderToken);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
