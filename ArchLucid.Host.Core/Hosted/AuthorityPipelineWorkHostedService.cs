using ArchiForge.Persistence.Orchestration;

namespace ArchiForge.Host.Core.Hosted;

/// <summary>
/// Periodically drains <see cref="IAuthorityPipelineWorkRepository"/> so deferred authority stages run after the run header commits.
/// </summary>
public sealed class AuthorityPipelineWorkHostedService(
    IAuthorityPipelineWorkProcessor processor,
    ILogger<AuthorityPipelineWorkHostedService> logger,
    HostLeaderElectionCoordinator electionCoordinator) : BackgroundService
{
    private readonly IAuthorityPipelineWorkProcessor _processor =
        processor ?? throw new ArgumentNullException(nameof(processor));

    private readonly ILogger<AuthorityPipelineWorkHostedService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly HostLeaderElectionCoordinator _electionCoordinator =
        electionCoordinator ?? throw new ArgumentNullException(nameof(electionCoordinator));

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _electionCoordinator.RunLeaderWorkAsync(
            HostElectionLeaseNames.AuthorityPipelineWorkOutbox,
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
                _logger.LogError(ex, "Authority pipeline work host loop error.");
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
