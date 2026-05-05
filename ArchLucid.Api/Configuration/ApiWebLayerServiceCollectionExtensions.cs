using ArchLucid.Api.Auth.Services;
using ArchLucid.Api.Health;
using ArchLucid.Api.Middleware;
using ArchLucid.Api.Services;
using ArchLucid.Api.Services.Admin;
using ArchLucid.Api.Services.Evolution;
using ArchLucid.Api.Validators;
using ArchLucid.Application.Import;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Health;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchLucid.Api.Configuration;

/// <summary>
///     HTTP/API-only DI registrations that depend on API-layer models (product learning read models, evolution
///     simulation).
/// </summary>
/// <remarks>
///     The Worker host does not register these services; it does not expose Learning/Evolution controllers.
/// </remarks>
public static class ApiWebLayerServiceCollectionExtensions
{
    public static IServiceCollection AddArchLucidApiWebLayerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArchLucidOptions options = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        if (ArchLucidOptions.EffectiveIsInMemory(options.StorageProvider))
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
        services.AddScoped<ApiRequestMeteringMiddleware>();
        services.AddSingleton<ILocalTrialJwtIssuer, LocalTrialJwtIssuer>();
        services.AddScoped<IArchitectureRequestImportValidator, FluentArchitectureRequestImportValidator>();
        services.AddScoped<IImportRequestFileService, ImportRequestFileService>();

        services.AddHttpClient<IOutboundWebhookDryRunService, OutboundWebhookDryRunService>(static client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHealthChecks().AddCheck<AzureServiceBusNamespaceHealthCheck>(
            "azure_service_bus",
            failureStatus: HealthStatus.Unhealthy,
            tags: [ReadinessTags.Ready]);

        return services;
    }
}
