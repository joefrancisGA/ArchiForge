using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Hosting;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterTrialLifecycleScheduler(IServiceCollection services, ArchLucidHostingRole hostingRole)
    {
        if (hostingRole is not (ArchLucidHostingRole.Worker or ArchLucidHostingRole.Combined))
        {
            return;
        }

        services.AddHostedService<TrialLifecycleSchedulerHostedService>();
    }
}
