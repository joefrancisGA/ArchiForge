using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Host.Core.Jobs;

/// <summary>One poll of trial lifecycle automation (same body as one iteration of <see cref="Hosted.TrialLifecycleSchedulerHostedService"/>).</summary>
public sealed class TrialLifecycleArchLucidJob(
    IServiceProvider serviceProvider,
    ILogger<TrialLifecycleArchLucidJob> logger) : IArchLucidJob
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ILogger<TrialLifecycleArchLucidJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public string Name => ArchLucidJobNames.TrialLifecycle;

    /// <inheritdoc />
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            ITenantRepository tenantRepository = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
            TrialLifecycleTransitionEngine engine =
                scope.ServiceProvider.GetRequiredService<TrialLifecycleTransitionEngine>();

            IReadOnlyList<Guid> tenantIds =
                await tenantRepository.ListTrialLifecycleAutomationTenantIdsAsync(cancellationToken)
                    .ConfigureAwait(false);

            foreach (Guid tenantId in tenantIds)

                await engine.TryAdvanceTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trial lifecycle job iteration failed.");

            return ArchLucidJobExitCodes.JobFailure;
        }

        return ArchLucidJobExitCodes.Success;
    }
}
