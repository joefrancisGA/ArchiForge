using ArchLucid.Decisioning.Analysis;
using ArchLucid.Decisioning.Configuration;
using ArchLucid.Decisioning.Compliance.Evaluators;
using ArchLucid.Decisioning.Compliance.Loaders;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Plugins;
using ArchLucid.Persistence.Coordination.Compliance;
using ArchLucid.Provenance;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

using Di = ArchLucid.Decisioning.Interfaces;
using Dm = ArchLucid.Decisioning.Manifest.Builders;
using Dr = ArchLucid.Decisioning.Rules;
using Ds = ArchLucid.Decisioning.Services;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterDecisioningEngines(IServiceCollection services, IConfiguration configuration)
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

        services.Configure<HumanReviewFindingOptions>(configuration.GetSection(HumanReviewFindingOptions.SectionPath));

        RegisterPluginFindingEngines(services, configuration);

        services.AddScoped<Di.IFindingsOrchestrator, Ds.FindingsOrchestrator>();
        services.AddSingleton<Di.IFindingPayloadValidator, Ds.FindingPayloadValidator>();
        services.AddSingleton<FindingConfidenceCalculator>();
        services.AddSingleton<IExplanationFaithfulnessChecker, ExplanationFaithfulnessChecker>();
        services.AddSingleton<Di.IDecisionRuleProvider, Dr.InMemoryDecisionRuleProvider>();
        services.AddScoped<Di.IGoldenManifestBuilder, Dm.DefaultGoldenManifestBuilder>();
        services.AddSingleton<Di.IGoldenManifestValidator, Ds.GoldenManifestValidator>();
        services.AddSingleton<Di.IManifestHashService, Ds.ManifestHashService>();
        services.AddScoped<Di.IDecisionEngine, Ds.RuleBasedDecisionEngine>();
        services.AddSingleton<IProvenanceBuilder, ProvenanceBuilder>();
        services.AddScoped<Di.IAuthorityCommitProjectionBuilder, Decisioning.Manifest.AuthorityCommitProjectionBuilder>();
    }

    private static void RegisterPluginFindingEngines(IServiceCollection services, IConfiguration configuration)
    {
        string? pluginDirectory = configuration["ArchLucid:FindingEngines:PluginDirectory"];
        ILogger logger = NullLoggerFactory.Instance.CreateLogger("FindingEnginePlugins");

        foreach (Type engineType in FindingEnginePluginDiscovery.Discover(pluginDirectory, logger))

            services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(Di.IFindingEngine), engineType));

    }
}
