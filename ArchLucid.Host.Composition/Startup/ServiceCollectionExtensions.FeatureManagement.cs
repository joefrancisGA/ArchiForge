using Microsoft.FeatureManagement;

namespace ArchiForge.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    /// <summary>Registers Microsoft Feature Management for gradual authority pipeline rollout.</summary>
    public static IServiceCollection AddArchiForgeFeatureManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));

        return services;
    }
}
