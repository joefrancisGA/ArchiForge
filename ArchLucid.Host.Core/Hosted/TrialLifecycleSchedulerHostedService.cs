using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>
/// Periodic trial expiry / read-only / export-only / purge scheduler (leader-elected).
/// </summary>
public sealed class TrialLifecycleSchedulerHostedService(
    IServiceProvider serviceProvider,
    HostLeaderElectionCoordinator electionCoordinator,
    IOptionsMonitor<TrialLifecycleSchedulerOptions> optionsMonitor,
    ILogger<TrialLifecycleSchedulerHostedService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return electionCoordinator.RunLeaderWorkAsync(
            HostElectionLeaseNames.TrialLifecycleAutomation,
            PollLoopAsync,
            stoppingToken);
    }

    private async Task PollLoopAsync(CancellationToken leaderToken)
    {
        TrialLifecycleSchedulerOptions initial = optionsMonitor.CurrentValue;

        logger.LogInformation(
            "Trial lifecycle scheduler started (poll every {Minutes} minutes).",
            initial.IntervalMinutes);

        while (!leaderToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ITenantRepository tenantRepository = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
                TrialLifecycleTransitionEngine engine =
                    scope.ServiceProvider.GetRequiredService<TrialLifecycleTransitionEngine>();

                IReadOnlyList<Guid> tenantIds =
                    await tenantRepository.ListTrialLifecycleAutomationTenantIdsAsync(leaderToken)
                        .ConfigureAwait(false);

                foreach (Guid tenantId in tenantIds)
                {
                    await engine.TryAdvanceTenantAsync(tenantId, leaderToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!leaderToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Trial lifecycle scheduler iteration failed.");
            }

            TrialLifecycleSchedulerOptions opts = optionsMonitor.CurrentValue;
            TimeSpan delay = TimeSpan.FromMinutes(Math.Max(1, opts.IntervalMinutes));

            try
            {
                await Task.Delay(delay, leaderToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
