using ArchLucid.Host.Core.Jobs;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterArchLucidJobRunners(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ArchLucidJobsOptions>(configuration.GetSection(ArchLucidJobsOptions.SectionPath));
        services.AddSingleton<JobRunTelemetry>();
        services.AddSingleton<IArchLucidJob, AdvisoryScanArchLucidJob>();
        services.AddSingleton<ArchLucidJobRunner>();
    }
}
