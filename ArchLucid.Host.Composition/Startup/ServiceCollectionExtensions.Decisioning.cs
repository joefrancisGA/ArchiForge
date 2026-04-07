using ArchLucid.Decisioning.Analysis;
using ArchLucid.Decisioning.Compliance.Evaluators;
using ArchLucid.Decisioning.Compliance.Loaders;
using ArchLucid.Persistence.Coordination.Compliance;
using ArchLucid.Provenance;

using Di = ArchLucid.Decisioning.Interfaces;
using Dm = ArchLucid.Decisioning.Manifest.Builders;
using Dr = ArchLucid.Decisioning.Rules;
using Ds = ArchLucid.Decisioning.Services;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterDecisioningEngines(IServiceCollection services)
    {
        services.AddSingleton<IGraphCoverageAnalyzer, GraphCoverageAnalyzer>();

        string complianceRulePackPath = Path.Combine(
            AppContext.BaseDirectory,
            "Compliance",
            "RulePacks",
            "default-compliance.rules.json");
        services.AddSingleton<IComplianceRulePackLoader>(_ => new FileComplianceRulePackLoader(complianceRulePackPath));
        services.AddScoped<IComplianceRulePackProvider, PolicyFilteredComplianceRulePackProvider>();
        services.AddSingleton<IComplianceRulePackValidator, ComplianceRulePackValidator>();
        services.AddSingleton<IComplianceEvaluator, GraphComplianceEvaluator>();

        services.AddScoped<Di.IFindingEngine, Ds.RequirementFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.TopologyCoverageFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.SecurityBaselineFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.SecurityCoverageFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.PolicyApplicabilityFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.PolicyCoverageFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.RequirementCoverageFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.ComplianceFindingEngine>();
        services.AddScoped<Di.IFindingEngine, Ds.CostConstraintFindingEngine>();
        services.AddScoped<Di.IFindingsOrchestrator, Ds.FindingsOrchestrator>();
        services.AddSingleton<Di.IFindingPayloadValidator, Ds.FindingPayloadValidator>();
        services.AddSingleton<Di.IDecisionRuleProvider, Dr.InMemoryDecisionRuleProvider>();
        services.AddScoped<Di.IGoldenManifestBuilder, Dm.DefaultGoldenManifestBuilder>();
        services.AddSingleton<Di.IGoldenManifestValidator, Ds.GoldenManifestValidator>();
        services.AddSingleton<Di.IManifestHashService, Ds.ManifestHashService>();
        services.AddScoped<Di.IDecisionEngine, Ds.RuleBasedDecisionEngine>();
        services.AddSingleton<IProvenanceBuilder, ProvenanceBuilder>();
    }
}
