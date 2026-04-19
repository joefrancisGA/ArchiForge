using ArchLucid.Core.Audit;
using ArchLucid.Host.Core.Auth.Services;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Host.Core.Notifications.Email;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterTrialLifecycleAuditEmailPublishing(IServiceCollection services)
    {
        services.AddScoped<AuditService>();
        services.AddScoped<IAuditService, TrialLifecycleEmailPublishingAuditDecorator>();
    }

    private static void RegisterTrialLifecycleEmailHostedServices(
        IServiceCollection services,
        IConfiguration configuration,
        ArchLucidHostingRole hostingRole)
    {
        if (hostingRole is not (ArchLucidHostingRole.Worker or ArchLucidHostingRole.Combined))
        {
            return;
        }

        if (!ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.TrialEmailScan))
        {
            services.AddHostedService<TrialLifecycleEmailScanHostedService>();
        }
    }
}
