using ArchLucid.Application.ExecDigest;

namespace ArchLucid.Host.Core.Jobs;

/// <summary>One-shot exec digest delivery scan (same body as <see cref="Hosted.ExecDigestWeeklyHostedService"/> iteration).</summary>
public sealed class ExecDigestWeeklyArchLucidJob(
    IServiceProvider serviceProvider,
    ILogger<ExecDigestWeeklyArchLucidJob> logger) : IArchLucidJob
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ILogger<ExecDigestWeeklyArchLucidJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string Name => ArchLucidJobNames.ExecDigestWeekly;

    /// <inheritdoc />
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            ExecDigestWeeklyDeliveryScanner scanner =
                scope.ServiceProvider.GetRequiredService<ExecDigestWeeklyDeliveryScanner>();

            await scanner.PublishDueAsync(DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exec digest weekly job failed.");

            return ArchLucidJobExitCodes.JobFailure;
        }

        return ArchLucidJobExitCodes.Success;
    }
}
