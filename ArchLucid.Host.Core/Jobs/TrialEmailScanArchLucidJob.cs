using ArchLucid.Application.Notifications.Email;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Core.Jobs;

/// <summary>One scan for due trial lifecycle emails (same body as <see cref="Hosted.TrialLifecycleEmailScanHostedService"/> iteration).</summary>
public sealed class TrialEmailScanArchLucidJob(
    IServiceProvider serviceProvider,
    ILogger<TrialEmailScanArchLucidJob> logger) : IArchLucidJob
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ILogger<TrialEmailScanArchLucidJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string Name => ArchLucidJobNames.TrialEmailScan;

    /// <inheritdoc />
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            TrialScheduledLifecycleEmailScanner scanner =
                scope.ServiceProvider.GetRequiredService<TrialScheduledLifecycleEmailScanner>();

            await scanner.PublishDueAsync(DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trial lifecycle email scan job failed.");

            return ArchLucidJobExitCodes.JobFailure;
        }

        return ArchLucidJobExitCodes.Success;
    }
}
