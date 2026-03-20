using Di = ArchiForge.Decisioning.Interfaces;
using Dm = ArchiForge.Decisioning.Manifest.Builders;
using Dr = ArchiForge.Decisioning.Rules;
using Ds = ArchiForge.Decisioning.Services;

namespace ArchiForge.Api.Startup;

internal static partial class ServiceCollectionExtensions
{
    private static void RegisterDecisioningEngines(IServiceCollection services)
    {
        services.AddScoped<Di.IFindingEngine, Ds.RequirementFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.TopologySanityFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.SecurityBaselineFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.CostConstraintFindingEngine>();
        services.AddScoped<Di.IFindingsOrchestrator, Ds.FindingsOrchestrator>();
        services.AddSingleton<Di.IFindingPayloadValidator, Ds.FindingPayloadValidator>();
        services.AddSingleton<Di.IDecisionRuleProvider, Dr.InMemoryDecisionRuleProvider>();
        services.AddScoped<Di.IGoldenManifestBuilder, Dm.DefaultGoldenManifestBuilder>();
        services.AddSingleton<Di.IGoldenManifestValidator, Ds.GoldenManifestValidator>();
        services.AddSingleton<Di.IManifestHashService, Ds.ManifestHashService>();
        services.AddScoped<Di.IDecisionEngine, Ds.RuleBasedDecisionEngine>();
    }
}
