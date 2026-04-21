using ArchLucid.Application.ExecDigest;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>Hourly leader-elected poll that sends weekly executive digest emails when tenant-local schedule matches.</summary>
public sealed class ExecDigestWeeklyHostedService(
    IServiceProvider serviceProvider,
    HostLeaderElectionCoordinator electionCoordinator,
    ILogger<ExecDigestWeeklyHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return electionCoordinator.RunLeaderWorkAsync(
            HostElectionLeaseNames.ExecDigestWeeklyPolling,
            PollLoopAsync,
            stoppingToken);
    }

    private async Task PollLoopAsync(CancellationToken leaderToken)
    {
        logger.LogInformation(
            "Exec digest weekly delivery started (poll every {Hours} hours).",
            PollInterval.TotalHours);

        while (!leaderToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ExecDigestWeeklyDeliveryScanner scanner =
                    scope.ServiceProvider.GetRequiredService<ExecDigestWeeklyDeliveryScanner>();

                await scanner.PublishDueAsync(DateTimeOffset.UtcNow, leaderToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!leaderToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Exec digest weekly delivery iteration failed.");
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
