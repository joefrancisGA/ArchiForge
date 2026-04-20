using ArchLucid.AgentRuntime;
using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.ArtifactSynthesis.Docx;
using ArchLucid.ArtifactSynthesis.Generators;
using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Packaging;
using ArchLucid.ArtifactSynthesis.Renderers;
using ArchLucid.ArtifactSynthesis.Services;
using ArchLucid.Coordinator.Services;
using ArchLucid.Core.Ask;
using ArchLucid.Core.Configuration;
using ArchLucid.Decisioning.Advisory.Analysis;
using ArchLucid.Decisioning.Advisory.Learning;
using ArchLucid.Decisioning.Advisory.Services;
using ArchLucid.Decisioning.Comparison;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Decisioning.Validation;
using ArchLucid.Host.Core.Ask;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Services.Ask;
using ArchLucid.Persistence.Caching;
using ArchLucid.Persistence.Coordination.Caching;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Queries;
using ArchLucid.Persistence.Reads;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterCoordinatorDecisionEngineAndRepositories(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICoordinatorService, CoordinatorService>();
        services.AddSchemaValidation(configuration);
        services.AddScoped<DecisionMergeInputGate>();
        services.AddScoped<AgentProposalManifestMerger>();
        services.AddScoped<DecisionNodeManifestMerger>();
        services.AddScoped<ManifestGovernanceMerger>();
        services.AddScoped<IDecisionEngineService, DecisionEngineService>();
        services.AddScoped<IDecisionEngineV2, DecisionEngineV2>();
        services.AddSingleton<IComparisonService, ComparisonService>();
        services.AddSingleton<IImprovementSignalAnalyzer, ImprovementSignalAnalyzer>();
        services.AddSingleton<IAdaptiveRecommendationScorer, AdaptiveRecommendationScorer>();
        services.AddSingleton<IRecommendationLearningAnalyzer, RecommendationLearningAnalyzer>();
        services.AddSingleton<IRecommendationGenerator, RecommendationGenerator>();
        services.AddScoped<IImprovementAdvisorService, ImprovementAdvisorService>();
        services.Configure<ExplanationServiceOptions>(
            configuration.GetSection(ExplanationServiceOptions.SectionPath));
        services.Configure<RunExplanationAggregateOptions>(
            configuration.GetSection(RunExplanationAggregateOptions.SectionPath));
        // Binds AgentExecution:LlmCostEstimation; option type defaults keep cost visibility on when the section is absent.
        services.Configure<LlmCostEstimationOptions>(
            configuration.GetSection(LlmCostEstimationOptions.SectionPath));
        services.AddSingleton<ILlmCostEstimator, LlmCostEstimator>();
        services.AddSingleton<IDeterministicExplanationService, DeterministicExplanationService>();
        services.AddScoped<IExplanationService, ExplanationService>();
        RegisterRunExplanationSummaryService(services, configuration);
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IAskService, AskService>();
        services.AddScoped<IAgentEvaluationService, DefaultAgentEvaluationService>();
        services.AddScoped<IEvidenceBuilder, DefaultEvidenceBuilder>();
        services.AddScoped<IAgentExecutionTraceRecorder, AgentExecutionTraceRecorder>();

        ArchLucidOptions coordinatorStorage = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        if (ArchLucidOptions.EffectiveIsInMemory(coordinatorStorage.StorageProvider))
        {
            services.AddSingleton<IArchitectureRequestRepository, InMemoryArchitectureRequestRepository>();
            services.AddSingleton<IArchitectureRunIdempotencyRepository, InMemoryArchitectureRunIdempotencyRepository>();
            services.AddSingleton<IAgentTaskRepository, InMemoryAgentTaskRepository>();
            services.AddSingleton<IAgentResultRepository, InMemoryAgentResultRepository>();
            services.AddSingleton<IAgentEvaluationRepository, InMemoryAgentEvaluationRepository>();
            services.AddSingleton<IDecisionNodeRepository, InMemoryDecisionNodeRepository>();
            services.AddSingleton<ICoordinatorGoldenManifestRepository, InMemoryCoordinatorGoldenManifestRepository>();
            services.AddScoped<IUnifiedGoldenManifestReader, UnifiedGoldenManifestReader>();
            services.AddSingleton<IEvidenceBundleRepository, InMemoryEvidenceBundleRepository>();
            services.AddSingleton<ICoordinatorDecisionTraceRepository, InMemoryCoordinatorDecisionTraceRepository>();
            services.AddSingleton<IAgentEvidencePackageRepository, InMemoryAgentEvidencePackageRepository>();
            services.AddSingleton<IAgentExecutionTraceRepository, InMemoryAgentExecutionTraceRepository>();
            services.AddSingleton<IAgentOutputEvaluationResultRepository, NoOpAgentOutputEvaluationResultRepository>();
        }
        else
        {
            services.AddScoped<IAgentEvaluationRepository, AgentEvaluationRepository>();
            services.AddScoped<IDecisionNodeRepository, DecisionNodeRepository>();
            services.AddScoped<IArchitectureRequestRepository, ArchitectureRequestRepository>();
            services.AddScoped<IArchitectureRunIdempotencyRepository, ArchitectureRunIdempotencyRepository>();
            services.AddScoped<IAgentTaskRepository, AgentTaskRepository>();
            services.AddScoped<IAgentResultRepository, AgentResultRepository>();
            // Data-layer contracts (CreateAsync / GetByVersionAsync / batch traces) — distinct from
            // Decisioning.Interfaces.IGoldenManifestRepository / IDecisionTraceRepository registered in AddArchLucidStorage.
            services.AddScoped<ICoordinatorGoldenManifestRepository, GoldenManifestRepository>();
            services.AddScoped<IUnifiedGoldenManifestReader, UnifiedGoldenManifestReader>();
            services.AddScoped<IEvidenceBundleRepository, EvidenceBundleRepository>();
            services.AddScoped<ICoordinatorDecisionTraceRepository, DecisionTraceRepository>();
            services.AddScoped<IAgentEvidencePackageRepository, AgentEvidencePackageRepository>();
            services.AddScoped<IAgentExecutionTraceRepository, AgentExecutionTraceRepository>();
            services.AddScoped<IAgentOutputEvaluationResultRepository, AgentOutputEvaluationResultRepository>();
        }
    }

    private static void RegisterRunExplanationSummaryService(
        IServiceCollection services,
        IConfiguration configuration)
    {
        HotPathCacheOptions hotPath = configuration
                                          .GetSection(HotPathCacheOptions.SectionName)
                                          .Get<HotPathCacheOptions>()
                                      ?? new HotPathCacheOptions();

        if (!hotPath.Enabled)
        {
            services.AddScoped<IRunExplanationSummaryService, RunExplanationSummaryService>();
            return;
        }

        services.AddScoped<RunExplanationSummaryService>();
        services.AddScoped<IRunExplanationSummaryService>(sp => new CachingRunExplanationSummaryService(
            sp.GetRequiredService<RunExplanationSummaryService>(),
            sp.GetRequiredService<IHotPathReadCache>(),
            sp.GetRequiredService<IAuthorityQueryService>(),
            sp.GetRequiredService<ILogger<CachingRunExplanationSummaryService>>()));
    }

    private static void RegisterArtifactSynthesis(IServiceCollection services)
    {
        services.AddSingleton<IArtifactContentTypeResolver, ArtifactContentTypeResolver>();
        services.AddSingleton<IArtifactPackagingService, ArtifactPackagingService>();
        services.AddSingleton<IArtifactBundleValidator, ArtifactBundleValidator>();
        services.AddSingleton<IDiagramRenderer, MermaidDiagramRenderer>();
        services.AddScoped<IArtifactGenerator, ReferenceArchitectureMarkdownGenerator>();
        services.AddScoped<IArtifactGenerator, ArchitectureNarrativeArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, ComplianceMatrixArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, CoverageSummaryArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, DiagramAstGenerator>();
        services.AddScoped<IArtifactGenerator, MermaidDiagramArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, InventoryArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, CostSummaryArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, UnresolvedIssuesArtifactGenerator>();
        services.AddScoped<IArtifactSynthesisService, ArtifactSynthesisService>();
        services.AddScoped<IDocxExportService, DocxExportService>();
    }
}
