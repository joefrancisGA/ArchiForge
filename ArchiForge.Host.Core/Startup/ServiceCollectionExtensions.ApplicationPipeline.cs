using ArchiForge.Application;
using ArchiForge.Application.Agents;
using ArchiForge.Application.Analysis;
using ArchiForge.Application.Bootstrap;
using ArchiForge.Application.Common;
using ArchiForge.Application.Determinism;
using ArchiForge.Application.Diagrams;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Evidence;
using ArchiForge.Application.Evolution;
using ArchiForge.Application.Exports;
using ArchiForge.Application.Summaries;
using ArchiForge.Contracts.Evolution;
using ArchiForge.ContextIngestion.Canonicalization;
using ArchiForge.ContextIngestion.Connectors;
using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Infrastructure;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Parsing;
using ArchiForge.ContextIngestion.Summaries;
using ArchiForge.Persistence.Data.Repositories;
using ArchiForge.Host.Core.Configuration;
using ArchiForge.Host.Core.Services;
using ArchiForge.KnowledgeGraph.Inference;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Mapping;
using ArchiForge.KnowledgeGraph.Services;

using Microsoft.Extensions.DependencyInjection;

using ContextConnector = ArchiForge.ContextIngestion.Interfaces.IContextConnector;
using ContextIngestionService = ArchiForge.ContextIngestion.Interfaces.IContextIngestionService;
using GraphBuilder = ArchiForge.KnowledgeGraph.Interfaces.IGraphBuilder;
using KnowledgeGraphService = ArchiForge.KnowledgeGraph.Interfaces.IKnowledgeGraphService;

namespace ArchiForge.Host.Core.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterRunExportAndArchitectureAnalysis(IServiceCollection services, IConfiguration configuration)
    {
        ArchiForgeOptions exportStorage = configuration
                                              .GetSection(ArchiForgeOptions.SectionName)
                                              .Get<ArchiForgeOptions>()
                                          ?? new ArchiForgeOptions();

        if (string.Equals(exportStorage.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IRunExportRecordRepository, InMemoryRunExportRecordRepository>();
        }
        else
        {
            services.AddScoped<IRunExportRecordRepository, RunExportRecordRepository>();
        }

        services.AddScoped<IRunExportAuditService, RunExportAuditService>();
        services.AddScoped<IArchitectureApplicationService, ArchitectureApplicationService>();
        services.AddScoped<IArchitectureAnalysisService, ArchitectureAnalysisService>();
        services.AddScoped<ISimulationEngine, SimulationEngine>();
        services.AddScoped<IShadowExecutionService, ShadowExecutionService>();
        services.AddScoped<ISimulationEvaluationService, SimulationEvaluationService>();
        services.AddScoped<IArchitectureAnalysisExportService, MarkdownArchitectureAnalysisExportService>();
        services.AddScoped<IDiagramImageRenderer, NullDiagramImageRenderer>();
        services.AddScoped<IArchitectureAnalysisDocxExportService, DocxArchitectureAnalysisExportService>();
        services.Configure<ConsultingDocxTemplateOptions>(configuration.GetSection("ConsultingDocxTemplate"));
        services.AddScoped<IConsultingDocxTemplateOptionsProvider, DefaultConsultingDocxTemplateOptionsProvider>();
        services.AddScoped<IDocumentLogoProvider, FileSystemDocumentLogoProvider>();
        services.AddScoped<IArchitectureAnalysisConsultingDocxExportService, ConsultingDocxArchitectureAnalysisExportService>();
        services.AddSingleton<IConsultingDocxTemplateProfileResolver, DefaultConsultingDocxTemplateProfileResolver>();
        services.AddScoped<IConsultingDocxTemplateRecommendationService, ConsultingDocxTemplateRecommendationService>();
        services.AddScoped<IConsultingDocxExportProfileSelector, ConsultingDocxExportProfileSelector>();
        services.AddScoped<IEndToEndReplayComparisonService, EndToEndReplayComparisonService>();
        services.AddScoped<IEndToEndReplayComparisonSummaryFormatter, MarkdownEndToEndReplayComparisonSummaryFormatter>();
        services.AddScoped<IEndToEndReplayComparisonExportService, EndToEndReplayComparisonExportService>();
    }

    private static void RegisterComparisonReplayAndDrift(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ReplayDiagnosticsOptions>(configuration.GetSection(ReplayDiagnosticsOptions.SectionName));

        ArchiForgeOptions storageMode = configuration
                                            .GetSection(ArchiForgeOptions.SectionName)
                                            .Get<ArchiForgeOptions>()
                                        ?? new ArchiForgeOptions();

        if (string.Equals(storageMode.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IComparisonRecordRepository, InMemoryComparisonRecordRepository>();
        }
        else
        {
            services.AddScoped<IComparisonRecordRepository, ComparisonRecordRepository>();
        }

        services.AddScoped<IComparisonAuditService, ComparisonAuditService>();
        services.AddScoped<IComparisonDriftAnalyzer, ComparisonDriftAnalyzer>();
        services.AddScoped<IComparisonReplayService, ComparisonReplayService>();
        services.AddScoped<IComparisonReplayCostEstimator, ComparisonReplayCostEstimator>();
        services.AddScoped<IComparisonReplayApiService, ComparisonReplayApiService>();
        services.AddScoped<IComparisonDriftReportExportService, ComparisonDriftReportExportService>();
        services.AddSingleton<IReplayDiagnosticsRecorder, ReplayDiagnosticsRecorder>();
    }

    private static void RegisterRunReplayManifestAndDiffs(IServiceCollection services)
    {
        services.AddScoped<IActorContext, ActorContext>();
        services.AddScoped<IBaselineMutationAuditService, BaselineMutationAuditService>();
        services.AddScoped<IArchitectureRunService, ArchitectureRunService>();
        services.AddScoped<IRunDetailQueryService, RunDetailQueryService>();
        services.AddScoped<IReplayRunService, ReplayRunService>();
        services.AddScoped<IDeterminismCheckService, DeterminismCheckService>();
        services.AddScoped<IExportReplayService, ExportReplayService>();
        services.AddScoped<IAgentExecutorResolver, DefaultAgentExecutorResolver>();
        services.AddScoped<IDiagramGenerator, MermaidDiagramGenerator>();
        services.AddScoped<IManifestDiagramService, ManifestDiagramService>();
        services.AddScoped<IEvidenceSummaryFormatter, MarkdownEvidenceSummaryFormatter>();
        services.AddScoped<IManifestSummaryGenerator, MarkdownManifestSummaryGenerator>();
        services.AddScoped<IManifestSummaryService, ManifestSummaryService>();
        services.AddScoped<IArchitectureExportService, MarkdownArchitectureExportService>();
        services.AddScoped<IManifestDiffService, ManifestDiffService>();
        services.AddScoped<IManifestDiffSummaryFormatter, MarkdownManifestDiffSummaryFormatter>();
        services.AddScoped<IManifestDiffExportService, MarkdownManifestDiffExportService>();
        services.AddScoped<IAgentResultDiffService, AgentResultDiffService>();
        services.AddScoped<IAgentResultDiffSummaryFormatter, MarkdownAgentResultDiffSummaryFormatter>();
        services.AddScoped<IExportRecordDiffService, ExportRecordDiffService>();
        services.AddScoped<IExportRecordDiffSummaryFormatter, MarkdownExportRecordDiffSummaryFormatter>();
        services.AddScoped<IExportRecordDiffExportService, ExportRecordDiffExportService>();
        services.AddScoped<IDriftReportFormatter, MarkdownDriftReportFormatter>();
        services.AddScoped<DriftReportDocxExport>();
    }

    private static void RegisterContextIngestionAndKnowledgeGraph(IServiceCollection services)
    {
        services.AddSingleton<PlainTextContextDocumentParser>();
        services.AddSingleton<IContextDocumentParser>(static sp => sp.GetRequiredService<PlainTextContextDocumentParser>());
        services.AddSingleton<IReadOnlyList<IContextDocumentParser>>(static sp =>
            ContextDocumentParserPipeline.CreateOrderedContextDocumentParsers(sp));

        services.AddSingleton<IInfrastructureDeclarationParser, JsonInfrastructureDeclarationParser>();
        services.AddSingleton<IInfrastructureDeclarationParser, SimpleTerraformDeclarationParser>();

        // Concrete connectors (registered once each). Order here matches pipeline order for readability only;
        // execution order is defined solely in ContextConnectorPipeline.CreateOrderedContextConnectorPipeline.
        services.AddSingleton<StaticRequestContextConnector>();
        services.AddSingleton<InlineRequirementsConnector>();
        services.AddSingleton<DocumentConnector>();
        services.AddSingleton<PolicyReferenceConnector>();
        services.AddSingleton<TopologyHintsConnector>();
        services.AddSingleton<SecurityBaselineHintsConnector>();
        services.AddSingleton<InfrastructureDeclarationConnector>();

        // IEnumerable<IContextConnector> must come only from CreateOrderedContextConnectorPipeline — preserves
        // deterministic DeltaSummary segment order and operator-facing narrative (see docs/CONTEXT_INGESTION.md).
        services.AddSingleton<IEnumerable<ContextConnector>>(static sp =>
            ContextConnectorPipeline.CreateOrderedContextConnectorPipeline(sp));

        services.AddSingleton<ICanonicalEnricher, CanonicalInfrastructureEnricher>();
        services.AddSingleton<ICanonicalDeduplicator, CanonicalDeduplicator>();
        services.AddSingleton<IContextDeltaSummaryBuilder, DefaultContextDeltaSummaryBuilder>();

        services.AddScoped<ContextIngestionService, ArchiForge.ContextIngestion.Services.ContextIngestionService>();
        services.AddScoped<IGraphNodeFactory, GraphNodeFactory>();
        services.AddScoped<IGraphEdgeInferer, DefaultGraphEdgeInferer>();
        services.AddSingleton<IGraphValidator, GraphValidator>();
        services.AddScoped<GraphBuilder, KnowledgeGraph.Builders.DefaultGraphBuilder>();
        services.AddScoped<KnowledgeGraphService, ArchiForge.KnowledgeGraph.Services.KnowledgeGraphService>();
    }
}
