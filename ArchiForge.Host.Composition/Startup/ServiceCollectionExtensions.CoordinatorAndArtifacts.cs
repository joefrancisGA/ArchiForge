using ArchiForge.AgentRuntime;
using ArchiForge.AgentRuntime.Explanation;
using ArchiForge.Application.Decisions;
using ArchiForge.Application.Evidence;
using ArchiForge.ArtifactSynthesis.Docx;
using ArchiForge.ArtifactSynthesis.Generators;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Packaging;
using ArchiForge.ArtifactSynthesis.Renderers;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Coordinator.Services;
using ArchiForge.Core.Ask;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.DecisionEngine.Validation;
using ArchiForge.Decisioning.Advisory.Analysis;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Host.Core.Ask;
using ArchiForge.Host.Core.Configuration;
using ArchiForge.Host.Core.Services.Ask;
using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterCoordinatorDecisionEngineAndRepositories(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICoordinatorService, CoordinatorService>();
        services.AddSchemaValidation(configuration);
        services.AddScoped<IDecisionEngineService, DecisionEngineService>();
        services.AddScoped<IDecisionEngineV2, DecisionEngineV2>();
        services.AddSingleton<IComparisonService, ComparisonService>();
        services.AddSingleton<IImprovementSignalAnalyzer, ImprovementSignalAnalyzer>();
        services.AddSingleton<IAdaptiveRecommendationScorer, AdaptiveRecommendationScorer>();
        services.AddSingleton<IRecommendationLearningAnalyzer, RecommendationLearningAnalyzer>();
        services.AddSingleton<IRecommendationGenerator, RecommendationGenerator>();
        services.AddScoped<IImprovementAdvisorService, ImprovementAdvisorService>();
        services.AddScoped<IExplanationService, ExplanationService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IAskService, AskService>();
        services.AddScoped<IAgentEvaluationService, DefaultAgentEvaluationService>();
        services.AddScoped<IEvidenceBuilder, DefaultEvidenceBuilder>();
        services.AddScoped<IAgentExecutionTraceRecorder, AgentExecutionTraceRecorder>();

        ArchiForgeOptions coordinatorStorage = ArchiForgeConfigurationBridge.ResolveArchiForgeOptions(configuration);

        if (string.Equals(coordinatorStorage.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IArchitectureRequestRepository, InMemoryArchitectureRequestRepository>();
            services.AddSingleton<IArchitectureRunRepository>(sp =>
                new InMemoryArchitectureRunRepository(sp.GetRequiredService<IArchitectureRequestRepository>()));
            services.AddSingleton<IArchitectureRunIdempotencyRepository, InMemoryArchitectureRunIdempotencyRepository>();
            services.AddSingleton<IAgentTaskRepository, InMemoryAgentTaskRepository>();
            services.AddSingleton<IAgentResultRepository, InMemoryAgentResultRepository>();
            services.AddSingleton<IAgentEvaluationRepository, InMemoryAgentEvaluationRepository>();
            services.AddSingleton<IDecisionNodeRepository, InMemoryDecisionNodeRepository>();
            services.AddSingleton<IGoldenManifestRepository, InMemoryCoordinatorGoldenManifestRepository>();
            services.AddSingleton<IEvidenceBundleRepository, InMemoryEvidenceBundleRepository>();
            services.AddSingleton<IDecisionTraceRepository, InMemoryCoordinatorDecisionTraceRepository>();
            services.AddSingleton<IAgentEvidencePackageRepository, InMemoryAgentEvidencePackageRepository>();
            services.AddSingleton<IAgentExecutionTraceRepository, InMemoryAgentExecutionTraceRepository>();
        }
        else
        {
            services.AddScoped<IAgentEvaluationRepository, AgentEvaluationRepository>();
            services.AddScoped<IDecisionNodeRepository, DecisionNodeRepository>();
            services.AddScoped<IArchitectureRequestRepository, ArchitectureRequestRepository>();
            services.AddScoped<IArchitectureRunRepository, ArchitectureRunRepository>();
            services.AddScoped<IArchitectureRunIdempotencyRepository, ArchitectureRunIdempotencyRepository>();
            services.AddScoped<IAgentTaskRepository, AgentTaskRepository>();
            services.AddScoped<IAgentResultRepository, AgentResultRepository>();
            // Data-layer contracts (CreateAsync / GetByVersionAsync / batch traces) — distinct from
            // Decisioning.Interfaces.IGoldenManifestRepository / IDecisionTraceRepository registered in AddArchiForgeStorage.
            services.AddScoped<IGoldenManifestRepository, GoldenManifestRepository>();
            services.AddScoped<IEvidenceBundleRepository, EvidenceBundleRepository>();
            services.AddScoped<IDecisionTraceRepository, DecisionTraceRepository>();
            services.AddScoped<IAgentEvidencePackageRepository, AgentEvidencePackageRepository>();
            services.AddScoped<IAgentExecutionTraceRepository, AgentExecutionTraceRepository>();
        }
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
