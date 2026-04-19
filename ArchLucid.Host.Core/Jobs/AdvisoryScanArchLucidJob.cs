using ArchLucid.Host.Core.Hosted;

namespace ArchLucid.Host.Core.Jobs;

/// <summary>One poll iteration of advisory schedules (same body as <see cref="AdvisoryScanHostedService"/>).</summary>
public sealed class AdvisoryScanArchLucidJob(
    IServiceScopeFactory scopeFactory,
    ILogger<AdvisoryScanArchLucidJob> logger) : IArchLucidJob
{
    private const int MaxSchedulesPerRun = 10;

    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly ILogger<AdvisoryScanArchLucidJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string Name => ArchLucidJobNames.AdvisoryScan;

    /// <inheritdoc />
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            AdvisoryDueScheduleProcessor processor =
                scope.ServiceProvider.GetRequiredService<AdvisoryDueScheduleProcessor>();

            await processor.ProcessDueAsync(DateTime.UtcNow, MaxSchedulesPerRun, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Advisory scan job iteration failed.");

            return ArchLucidJobExitCodes.JobFailure;
        }

        return ArchLucidJobExitCodes.Success;
    }
}
