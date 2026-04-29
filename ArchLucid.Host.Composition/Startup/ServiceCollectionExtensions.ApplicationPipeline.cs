using ArchLucid.Application;
using ArchLucid.Application.Architecture;
using ArchLucid.Application.Agents;
using ArchLucid.Application.Analysis;
using ArchLucid.Application.Common;
using ArchLucid.Application.Determinism;
using ArchLucid.Application.Diagrams;
using ArchLucid.Application.Diffs;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Evolution;
using ArchLucid.Application.Explanation;
using ArchLucid.Application.Exports;
using ArchLucid.Application.Governance;
using ArchLucid.Application.Marketing;
using ArchLucid.Application.Pilots;
using ArchLucid.Application.Runs.Finalization;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Application.Summaries;
using ArchLucid.Application.Support;
using ArchLucid.Application.Traceability;
using ArchLucid.Application.Value;
using ArchLucid.ContextIngestion.Canonicalization;
using ArchLucid.ContextIngestion.Connectors;
using ArchLucid.ContextIngestion.Contracts;
using ArchLucid.ContextIngestion.Infrastructure;
using ArchLucid.ContextIngestion.Parsing;
using ArchLucid.ContextIngestion.Summaries;
using ArchLucid.Contracts.Abstractions.Evolution;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Diagrams;
using ArchLucid.Host.Composition.ValueReports;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Host.Core.Marketing;
using ArchLucid.Host.Core.Services;
using ArchLucid.KnowledgeGraph.Inference;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Mapping;
using ArchLucid.KnowledgeGraph.Services;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.DependencyInjection.Extensions;

using ContextConnector = ArchLucid.ContextIngestion.Interfaces.IContextConnector;
using ContextIngestionService = ArchLucid.ContextIngestion.Interfaces.IContextIngestionService;
using GraphBuilder = ArchLucid.KnowledgeGraph.Interfaces.IGraphBuilder;
using KnowledgeGraphService = ArchLucid.KnowledgeGraph.Interfaces.IKnowledgeGraphService;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterRunExportAndArchitectureAnalysis(IServiceCollection services, IConfiguration configuration)
    {
        ArchLucidOptions exportStorage = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        if (ArchLucidOptions.EffectiveIsInMemory(exportStorage.StorageProvider))

            services.AddSingleton<IRunExportRecordRepository, InMemoryRunExportRecordRepository>();

        else

            services.AddScoped<IRunExportRecordRepository, RunExportRecordRepository>();


        services.AddScoped<IRunExportAuditService, RunExportAuditService>();
        services.AddScoped<IArchitectureApplicationService, ArchitectureApplicationService>();
        services.AddScoped<IArchitectureAnalysisService, ArchitectureAnalysisService>();
        services.AddScoped<ISimulationEngine, SimulationEngine>();
        services.AddScoped<IShadowExecutionService, ShadowExecutionService>();
        services.AddScoped<ISimulationEvaluationService, SimulationEvaluationService>();
        services.AddScoped<IArchitectureAnalysisExportService, MarkdownArchitectureAnalysisExportService>();
        bool mermaidCliEnabled = configuration.GetValue("ArchLucid:MermaidCli:Enabled", false);

        if (mermaidCliEnabled)

            services.AddScoped<IDiagramImageRenderer, MermaidCliDiagramImageRenderer>();

        else

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

        ArchLucidOptions storageMode = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        if (ArchLucidOptions.EffectiveIsInMemory(storageMode.StorageProvider))

            services.AddSingleton<IComparisonRecordRepository, InMemoryComparisonRecordRepository>();

        else

            services.AddScoped<IComparisonRecordRepository, ComparisonRecordRepository>();


        services.AddScoped<IComparisonAuditService, ComparisonAuditService>();
        services.AddScoped<IComparisonDriftAnalyzer, ComparisonDriftAnalyzer>();
        services.AddScoped<IComparisonReplayService, ComparisonReplayService>();
        services.AddScoped<IComparisonReplayCostEstimator, ComparisonReplayCostEstimator>();
        services.AddScoped<IComparisonReplayApiService, ComparisonReplayApiService>();
        services.AddScoped<IComparisonDriftReportExportService, ComparisonDriftReportExportService>();
        services.AddSingleton<IReplayDiagnosticsRecorder, ReplayDiagnosticsRecorder>();
    }

    private static void RegisterRunReplayManifestAndDiffs(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IActorContext, ActorContext>();
        services.AddScoped<IBaselineMutationAuditService, BaselineMutationAuditService>();
        services.Configure<PreCommitGovernanceGateOptions>(
            configuration.GetSection(PreCommitGovernanceGateOptions.SectionPath));
        services.Configure<ArchitectureRunCreateOptions>(
            configuration.GetSection(ArchitectureRunCreateOptions.SectionPath));
        services.AddScoped<IPreCommitGovernanceGate, PreCommitGovernanceGate>();
        services.AddScoped<IManifestFinalizationService, ManifestFinalizationService>();
        services.AddScoped<IArchitectureRunCreateOrchestrator, ArchitectureRunCreateOrchestrator>();
        services.AddScoped<IArchitectureRunExecuteOrchestrator, ArchitectureRunExecuteOrchestrator>();
        // ADR 0030 PR A3 (2026-04-24): the legacy ArchitectureRunCommitOrchestrator + RunCommitPathSelector
        // + LegacyRunCommitPathOptions were deleted. The authority-driven orchestrator is the single commit implementation.
        services.AddScoped<IArchitectureRunCommitOrchestrator, AuthorityDrivenArchitectureRunCommitOrchestrator>();
        services.AddScoped<IRunDetailQueryService, RunDetailQueryService>();
        services.Configure<RunRoiEstimatorOptions>(configuration.GetSection(RunRoiEstimatorOptions.SectionPath));
        services.AddScoped<IRunRoiEstimator, RunRoiEstimator>();
        services.AddScoped<ITraceabilityBundleBuilder, TraceabilityBundleBuilder>();
        services.AddScoped<IFindingEvidenceChainService, FindingEvidenceChainService>();
        services.AddScoped<IFindingLlmAuditService, FindingLlmAuditService>();
        services.AddScoped<IPilotRunDeltaComputer, PilotRunDeltaComputer>();
        services.AddScoped<IRecentPilotRunDeltasService, RecentPilotRunDeltasService>();
        services.AddScoped<IPolicyPackDryRunService, PolicyPackDryRunService>();
        services.AddSingleton<IEvidencePackSourceProvider, EmbeddedResourceEvidencePackSourceProvider>();
        services.AddSingleton<IEvidencePackBuilder, EvidencePackBuilder>();
        services.AddSingleton<ISupportBundleAssembler, SupportBundleAssembler>();
        services.AddScoped<IReferenceEvidenceAdminExportService, ReferenceEvidenceAdminExportService>();
        services.AddSingleton<IExecutionProvenanceFooterRenderer,
            ExecutionProvenanceFooterRenderer>();
        services.AddScoped<FirstValueReportBuilder>();
        services.AddScoped<FirstValueReportPdfBuilder>();
        services.AddScoped<WhyArchLucidPackPdfBuilder>();
        services.AddScoped<ExecutiveSponsorBriefPdfBuilder>();
        services.AddScoped<PilotScorecardBuilder>();
        services.AddScoped<IPilotInProductScorecardService, PilotInProductScorecardService>();
        services.AddScoped<PilotOutcomeSummaryService>();
        services.AddScoped<SponsorOnePagerPdfBuilder>();
        services.AddScoped<BoardPackPdfBuilder>();
        services.TryAddSingleton<IInstrumentationCounterSnapshotProvider, MeterListenerCounterSnapshotProvider>();
        services.AddScoped<IWhyArchLucidSnapshotService, WhyArchLucidSnapshotService>();
        services.AddScoped<ISponsorEvidencePackService, SponsorEvidencePackService>();
        services.AddScoped<ITenantMeasuredRoiService, TenantMeasuredRoiService>();
        services.AddScoped<IDemoSeedRunResolver, DemoSeedRunResolver>();
        services.AddScoped<IDemoReadModelClient, DemoReadModelClient>();
        services.AddScoped<IDemoCommitPagePreviewClient, DemoCommitPagePreviewClient>();
        services.AddScoped<IPublicShowcaseCommitPageClient, PublicShowcaseCommitPageClient>();
        services.Configure<ValueReportComputationOptions>(
            configuration.GetSection(ValueReportComputationOptions.SectionPath));
        services.AddScoped<ValueReportBuilder>();
        services.AddSingleton<IValueReportJobQueue, InMemoryValueReportJobQueue>();
        services.AddScoped<IRunRationaleService, RunRationaleService>();
        services.AddScoped<IArchitectureRunProvenanceService, ArchitectureRunProvenanceService>();
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
        services.AddSingleton<IInfrastructureDeclarationParser, TerraformShowJsonInfrastructureDeclarationParser>();

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

        services.AddScoped<ContextIngestionService, ArchLucid.ContextIngestion.Services.ContextIngestionService>();
        services.AddScoped<IGraphNodeFactory, GraphNodeFactory>();
        services.AddScoped<IGraphEdgeInferer, DefaultGraphEdgeInferer>();
        services.AddSingleton<IGraphValidator, GraphValidator>();
        services.AddScoped<GraphBuilder, KnowledgeGraph.Builders.DefaultGraphBuilder>();
        services.AddScoped<KnowledgeGraphService, ArchLucid.KnowledgeGraph.Services.KnowledgeGraphService>();
    }
}
