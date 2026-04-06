using ArchiForge.Persistence.Integration;

namespace ArchiForge.Host.Core.Hosted;

/// <summary>Periodically drains <see cref="IIntegrationEventOutboxRepository"/> after authority commits.</summary>
public sealed class IntegrationEventOutboxHostedService(
    IIntegrationEventOutboxProcessor processor,
    ILogger<IntegrationEventOutboxHostedService> logger,
    HostLeaderElectionCoordinator electionCoordinator) : BackgroundService
{
    private readonly IIntegrationEventOutboxProcessor _processor =
        processor ?? throw new ArgumentNullException(nameof(processor));

    private readonly ILogger<IntegrationEventOutboxHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly HostLeaderElectionCoordinator _electionCoordinator =
        electionCoordinator ?? throw new ArgumentNullException(nameof(electionCoordinator));

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _electionCoordinator.RunLeaderWorkAsync(
            HostElectionLeaseNames.IntegrationEventOutbox,
            LoopAsync,
            stoppingToken);
    }

    private async Task LoopAsync(CancellationToken leaderToken)
    {
        while (!leaderToken.IsCancellationRequested)
        {
            try
            {
                await _processor.ProcessPendingBatchAsync(leaderToken);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Integration event outbox host loop error.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), leaderToken);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
