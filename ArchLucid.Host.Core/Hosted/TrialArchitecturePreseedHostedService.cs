using ArchLucid.Application;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Hosted;

/// <summary>Leader-elected worker loop that completes queued trial architecture pre-seeds.</summary>
public sealed class TrialArchitecturePreseedHostedService(
    IServiceProvider serviceProvider,
    HostLeaderElectionCoordinator electionCoordinator,
    IOptionsMonitor<TrialArchitecturePreseedOptions> optionsMonitor,
    ILogger<TrialArchitecturePreseedHostedService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        electionCoordinator.RunLeaderWorkAsync(
            HostElectionLeaseNames.TrialArchitecturePreseed,
            PollLoopAsync,
            stoppingToken);

    private async Task PollLoopAsync(CancellationToken leaderToken)
    {
        TrialArchitecturePreseedOptions initial = optionsMonitor.CurrentValue;

        if (!initial.Enabled)
        {
            logger.LogInformation("Trial architecture pre-seed is disabled (TrialArchitecturePreseed:Enabled=false).");

            return;
        }

        logger.LogInformation(
            "Trial architecture pre-seed worker started (poll every {Seconds}s).",
            Math.Max(5, initial.PollIntervalSeconds));

        while (!leaderToken.IsCancellationRequested)
        {
            try
            {
                TrialArchitecturePreseedOptions opts = optionsMonitor.CurrentValue;

                if (!opts.Enabled)
                    break;

                using IServiceScope scope = serviceProvider.CreateScope();
                ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
                TrialArchitecturePreseedExecutor executor =
                    scope.ServiceProvider.GetRequiredService<TrialArchitecturePreseedExecutor>();

                IReadOnlyList<Guid> pending =
                    await tenants.ListTenantIdsPendingTrialArchitecturePreseedAsync(opts.BatchSize, leaderToken)
                        .ConfigureAwait(false);

                foreach (Guid tenantId in pending)
                    await executor.TryProcessTenantAsync(tenantId, leaderToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (leaderToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!leaderToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Trial architecture pre-seed iteration failed.");
            }

            TrialArchitecturePreseedOptions delayOpts = optionsMonitor.CurrentValue;
            TimeSpan delay = TimeSpan.FromSeconds(Math.Max(5, delayOpts.PollIntervalSeconds));

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
