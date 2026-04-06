using ArchiForge.Api.Services;
using ArchiForge.Api.Services.Admin;
using ArchiForge.Api.Services.Evolution;
using ArchiForge.Host.Core.Configuration;

namespace ArchiForge.Api.Configuration;

/// <summary>
/// HTTP/API-only DI registrations that depend on API-layer models (product learning read models, evolution simulation).
/// </summary>
/// <remarks>
/// The Worker host does not register these services; it does not expose Learning/Evolution controllers.
/// </remarks>
public static class ApiWebLayerServiceCollectionExtensions
{
    public static IServiceCollection AddArchiForgeApiWebLayerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArchiForgeOptions options = ArchiForgeConfigurationBridge.ResolveArchiForgeOptions(configuration);

        if (string.Equals(options.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ILearningPlanningReadService, LearningPlanningReadService>();
            services.AddScoped<IEvolutionSimulationService, EvolutionSimulationService>();
        }
        else
        {
            services.AddScoped<ILearningPlanningReadService, LearningPlanningReadService>();
            services.AddScoped<IEvolutionSimulationService, EvolutionSimulationService>();
        }

        services.AddScoped<IAdminDiagnosticsService, AdminDiagnosticsService>();

        return services;
    }
}
