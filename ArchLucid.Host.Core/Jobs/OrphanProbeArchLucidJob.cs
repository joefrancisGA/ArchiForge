using ArchLucid.Host.Core.DataConsistency;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Core.Jobs;

/// <summary>One-shot data consistency orphan probe (same work as one loop iteration of <see cref="Hosted.DataConsistencyOrphanProbeHostedService"/>).</summary>
public sealed class OrphanProbeArchLucidJob(
    DataConsistencyOrphanProbeExecutor executor,
    ILogger<OrphanProbeArchLucidJob> logger) : IArchLucidJob
{
    private readonly DataConsistencyOrphanProbeExecutor _executor =
        executor ?? throw new ArgumentNullException(nameof(executor));

    private readonly ILogger<OrphanProbeArchLucidJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string Name => ArchLucidJobNames.OrphanProbe;

    /// <inheritdoc />
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _executor.RunOnceAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orphan probe job failed.");

            return ArchLucidJobExitCodes.JobFailure;
        }

        return ArchLucidJobExitCodes.Success;
    }
}
