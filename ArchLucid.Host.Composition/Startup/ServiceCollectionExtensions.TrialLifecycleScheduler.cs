using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Jobs;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterTrialLifecycleScheduler(
        IServiceCollection services,
        IConfiguration configuration,
        ArchLucidHostingRole hostingRole)
    {
        if (hostingRole is not (ArchLucidHostingRole.Worker or ArchLucidHostingRole.Combined))
        {
            return;
        }

        if (!ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.TrialLifecycle))
        {
            services.AddHostedService<TrialLifecycleSchedulerHostedService>();
        }
    }
}
