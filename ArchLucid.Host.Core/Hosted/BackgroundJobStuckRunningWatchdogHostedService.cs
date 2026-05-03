using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>
///     Periodically reclaims <c>dbo.BackgroundJobs</c> rows stuck in <see cref="BackgroundJobRow.State" /> = <c>Running</c>
///     when a worker terminates before finishing (visibility timeout / crash).
/// </summary>
public sealed class BackgroundJobStuckRunningWatchdogHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundJobStuckRunningWatchdogHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

                IBackgroundJobRepository repository =
                    scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();

                int affected = await repository.ResetStaleRunningJobsOlderThanAsync(StaleAfter, stoppingToken);

                if (affected > 0)

                    logger.LogWarning(
                        "Reclaimed background jobs stuck Running > {Minutes} minutes: {Count}.",
                        StaleAfter.TotalMinutes,
                        affected);
            }

            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)

            {
                logger.LogError(ex, "Background job watchdog iteration failed.");
            }

            try

            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }

            catch (OperationCanceledException)

            {
                break;
            }
        }
    }
}
