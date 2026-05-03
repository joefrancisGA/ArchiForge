using ArchLucid.Persistence.Cosmos;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Jobs;

/// <summary>Processes at most one Cosmos audit change feed batch (for scheduled or KEDA-scaled Container Apps Jobs).</summary>
public sealed class AuditEventChangeFeedArchLucidJob(
    IAuditEventChangeFeedSingleBatchRunner processor,
    IOptionsMonitor<CosmosDbOptions> cosmosOptions,
    ILogger<AuditEventChangeFeedArchLucidJob> logger) : IArchLucidJob
{
    private static readonly TimeSpan DefaultMaxIdleWait = TimeSpan.FromSeconds(45);

    private readonly IAuditEventChangeFeedSingleBatchRunner _processor =
        processor ?? throw new ArgumentNullException(nameof(processor));

    private readonly IOptionsMonitor<CosmosDbOptions> _cosmosOptions =
        cosmosOptions ?? throw new ArgumentNullException(nameof(cosmosOptions));

    private readonly ILogger<AuditEventChangeFeedArchLucidJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string Name => ArchLucidJobNames.AuditChangeFeed;

    /// <inheritdoc />
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!_cosmosOptions.CurrentValue.AuditEventsEnabled)
            return ArchLucidJobExitCodes.Success;

        try
        {
            await _processor.RunSingleBatchOrIdleAsync(DefaultMaxIdleWait, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit change feed job failed.");

            return ArchLucidJobExitCodes.JobFailure;
        }

        return ArchLucidJobExitCodes.Success;
    }
}
