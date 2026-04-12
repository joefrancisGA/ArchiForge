using ArchLucid.Core.Configuration;
using ArchLucid.Host.Core.Configuration;

using Microsoft.FeatureManagement;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    /// <summary>Registers Microsoft Feature Management for gradual authority pipeline rollout.</summary>
    public static IServiceCollection AddArchLucidFeatureManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));
        services.AddSingleton<IFeatureFlags, FeatureManagementFeatureFlags>();

        return services;
    }
}
