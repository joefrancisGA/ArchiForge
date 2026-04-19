using ArchLucid.Application.Notifications.Email;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>Periodic scan that enqueues scheduled trial lifecycle emails (day 7, limits, expiry).</summary>
public sealed class TrialLifecycleEmailScanHostedService(
    IServiceProvider serviceProvider,
    HostLeaderElectionCoordinator electionCoordinator,
    ILogger<TrialLifecycleEmailScanHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return electionCoordinator.RunLeaderWorkAsync(
            HostElectionLeaseNames.TrialLifecycleEmailPolling,
            PollLoopAsync,
            stoppingToken);
    }

    private async Task PollLoopAsync(CancellationToken leaderToken)
    {
        logger.LogInformation(
            "Trial lifecycle email scan started (poll every {Hours} hours).",
            PollInterval.TotalHours);

        while (!leaderToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                TrialScheduledLifecycleEmailScanner scanner =
                    scope.ServiceProvider.GetRequiredService<TrialScheduledLifecycleEmailScanner>();

                await scanner.PublishDueAsync(DateTimeOffset.UtcNow, leaderToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!leaderToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Trial lifecycle email scan iteration failed.");
            }

            try
            {
                await Task.Delay(PollInterval, leaderToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
