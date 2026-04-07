using ArchLucid.Persistence.Coordination.Retrieval;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>
/// Periodically drains <see cref="IRetrievalIndexingOutboxRepository"/> so retrieval indexing runs after the authority UOW commits.
/// </summary>
/// <remarks>
/// When <c>HostLeaderElection:Enabled</c> is true and storage is SQL, only one worker replica drains the outbox.
/// </remarks>
public sealed class RetrievalIndexingOutboxHostedService(
    IRetrievalIndexingOutboxProcessor processor,
    ILogger<RetrievalIndexingOutboxHostedService> logger,
    HostLeaderElectionCoordinator electionCoordinator) : BackgroundService
{
    private readonly IRetrievalIndexingOutboxProcessor _processor =
        processor ?? throw new ArgumentNullException(nameof(processor));

    private readonly ILogger<RetrievalIndexingOutboxHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly HostLeaderElectionCoordinator _electionCoordinator =
        electionCoordinator ?? throw new ArgumentNullException(nameof(electionCoordinator));

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _electionCoordinator.RunLeaderWorkAsync(
            HostElectionLeaseNames.RetrievalIndexingOutbox,
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
                _logger.LogError(ex, "Retrieval indexing outbox host loop error.");
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
