using ArchLucid.Application.ExecDigest;
using ArchLucid.Application.Notifications.Email;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Jobs;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterExecDigestServices(IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddSingleton<IExecDigestUnsubscribeTokenFactory, ExecDigestUnsubscribeTokenFactory>();
        services.AddScoped<IExecDigestComposer, ExecDigestComposer>();
        services.AddScoped<IExecDigestEmailDispatcher, ExecDigestEmailDispatcher>();
        services.AddScoped<ExecDigestWeeklyDeliveryScanner>();
    }

    private static void RegisterExecDigestWorkerInfrastructure(
        IServiceCollection services,
        IConfiguration configuration,
        ArchLucidHostingRole hostingRole)
    {
        if (hostingRole is not (ArchLucidHostingRole.Worker or ArchLucidHostingRole.Combined))
            return;


        if (!ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.ExecDigestWeekly))

            services.AddHostedService<ExecDigestWeeklyHostedService>();
    }
}
