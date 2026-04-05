using ArchiForge.Host.Core.Configuration;
using ArchiForge.Persistence.Data.Repositories;

using Microsoft.Extensions.Options;

namespace ArchiForge.Host.Core.Hosted;

/// <summary>
/// Runs a leader-only body with SQL lease acquisition, background renewal, and loss detection.
/// </summary>
public sealed class HostLeaderElectionCoordinator(
    IOptionsMonitor<HostLeaderElectionOptions> optionsMonitor,
    IHostLeaderLeaseRepository leaseRepository,
    HostInstanceIdentifier instanceId,
    ILogger<HostLeaderElectionCoordinator> logger)
{
    private readonly IOptionsMonitor<HostLeaderElectionOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private readonly IHostLeaderLeaseRepository _leaseRepository =
        leaseRepository ?? throw new ArgumentNullException(nameof(leaseRepository));

    private readonly HostInstanceIdentifier _instanceId =
        instanceId ?? throw new ArgumentNullException(nameof(instanceId));

    private readonly ILogger<HostLeaderElectionCoordinator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// When election is disabled, runs <paramref name="leaderWork"/> with <paramref name="applicationStoppingToken"/> only.
    /// When enabled, repeats: acquire lease, run work with a token cancelled on lease loss or shutdown, renew in the background, release on exit.
    /// </summary>
    public async Task RunLeaderWorkAsync(
        string leaseName,
        Func<CancellationToken, Task> leaderWork,
        CancellationToken applicationStoppingToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseName);
        ArgumentNullException.ThrowIfNull(leaderWork);

        HostLeaderElectionOptions opts = _optionsMonitor.CurrentValue;

        if (!opts.Enabled)
        {
            try
            {
                await leaderWork(applicationStoppingToken);
            }
            catch (OperationCanceledException) when (applicationStoppingToken.IsCancellationRequested)
            {
            }

            return;
        }

        string id = _instanceId.Value;
        int leaseSec = Math.Clamp(opts.LeaseDurationSeconds, 15, 3600);
        int renewSec = Math.Clamp(opts.RenewIntervalSeconds, 5, leaseSec - 1);
        int followerMs = Math.Clamp(opts.FollowerPollMilliseconds, 100, 120_000);

        while (!applicationStoppingToken.IsCancellationRequested)
        {
            bool acquired = await _leaseRepository.TryAcquireOrRenewAsync(
                leaseName,
                id,
                leaseSec,
                applicationStoppingToken);

            if (!acquired)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Host leader lease not held for {LeaseName}; follower wait {Ms} ms.", leaseName, followerMs);
                }

                try
                {
                    await Task.Delay(followerMs, applicationStoppingToken);
                }
                catch (OperationCanceledException) when (applicationStoppingToken.IsCancellationRequested)
                {
                    return;
                }

                continue;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Acquired host leader lease {LeaseName} for instance {InstanceId}.", leaseName, id);
            }

            using CancellationTokenSource leaderCts = CancellationTokenSource.CreateLinkedTokenSource(applicationStoppingToken);
            CancellationToken leaderToken = leaderCts.Token;

            Task renewTask = RenewLoopAsync(leaseName, id, leaseSec, renewSec, leaderCts, applicationStoppingToken);

            try
            {
                await leaderWork(leaderToken);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested
                                                     && !applicationStoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Leader work for {LeaseName} stopped after lease loss or handoff.", leaseName);
                }
            }
            finally
            {
                await leaderCts.CancelAsync();

                try
                {
                    await renewTask;
                }
                catch (OperationCanceledException)
                {
                }

                await _leaseRepository.TryReleaseAsync(leaseName, id, applicationStoppingToken);
            }

            if (applicationStoppingToken.IsCancellationRequested)
            {
                return;
            }

            // Lost lease while app still running: re-enter outer loop to compete again.
        }
    }

    private async Task RenewLoopAsync(
        string leaseName,
        string id,
        int leaseDurationSeconds,
        int renewIntervalSeconds,
        CancellationTokenSource leaderCts,
        CancellationToken applicationStoppingToken)
    {
        try
        {
            while (!applicationStoppingToken.IsCancellationRequested && !leaderCts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(renewIntervalSeconds), applicationStoppingToken);

                if (applicationStoppingToken.IsCancellationRequested || leaderCts.IsCancellationRequested)
                {
                    return;
                }

                bool renewed = await _leaseRepository.TryAcquireOrRenewAsync(
                    leaseName,
                    id,
                    leaseDurationSeconds,
                    applicationStoppingToken);

                if (!renewed)
                {
                    _logger.LogWarning(
                        "Failed to renew host leader lease {LeaseName} for {InstanceId}; stopping leader work.",
                        leaseName,
                        id);

                    await leaderCts.CancelAsync();

                    return;
                }
            }
        }
        catch (OperationCanceledException) when (applicationStoppingToken.IsCancellationRequested)
        {
        }
    }
}
