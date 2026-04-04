using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Governance;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Application.Bootstrap;

/// <summary>
/// Idempotent seed for the Contoso Retail Modernization **trusted baseline** (two committed runs, governance workflow, activations).
/// </summary>
/// <remarks>
/// Persists via <c>ArchiForge.Persistence.Data.Repositories</c> only. The export row is optional metadata for export history — not required for
/// consulting DOCX replay. See <c>docs/TRUSTED_BASELINE.md</c>.
/// </remarks>
public sealed class DemoSeedService(
    IArchitectureRequestRepository requestRepository,
    IArchitectureRunRepository runRepository,
    IAgentTaskRepository taskRepository,
    IAgentResultRepository resultRepository,
    IGoldenManifestRepository manifestRepository,
    IDecisionTraceRepository decisionTraceRepository,
    IGovernanceApprovalRequestRepository approvalRepository,
    IGovernancePromotionRecordRepository promotionRepository,
    IGovernanceEnvironmentActivationRepository activationRepository,
    IRunExportRecordRepository runExportRecordRepository,
    ILogger<DemoSeedService> logger) : IDemoSeedService
{
    private static readonly DateTime DemoUtc = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRequestAsync(cancellationToken);
        await EnsureCommittedRunAsync(
                ContosoRetailDemoIdentifiers.RunBaseline,
                DemoIds.TaskBaseline,
                DemoIds.ResultBaseline,
                ContosoRetailDemoIdentifiers.ManifestBaseline,
                isHardened: false,
                cancellationToken)
            ;
        await EnsureCommittedRunAsync(
                ContosoRetailDemoIdentifiers.RunHardened,
                DemoIds.TaskHardened,
                DemoIds.ResultHardened,
                ContosoRetailDemoIdentifiers.ManifestHardened,
                isHardened: true,
                cancellationToken)
            ;
        await EnsureGovernanceAsync(cancellationToken);
        await EnsureExportRecordAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        
            logger.LogInformation(
                "Demo seed completed (Contoso Retail Modernization). Runs: {Baseline}, {Hardened}.",
                ContosoRetailDemoIdentifiers.RunBaseline,
                ContosoRetailDemoIdentifiers.RunHardened);
        
    }

    private async Task EnsureRequestAsync(CancellationToken cancellationToken)
    {
        if (await requestRepository.GetByIdAsync(ContosoRetailDemoIdentifiers.RequestContoso, cancellationToken) is not null)
        
            return;
        

        ArchitectureRequest request = new()
        {
            RequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            Description = "Contoso Retail modernization — migrate monolith checkout to Azure with PCI-aware boundaries.",
            SystemName = "Contoso Retail Platform",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints = ["Minimize public ingress", "Retain existing payment processor integration"]
        };

        await requestRepository.CreateAsync(request, cancellationToken);
    }

    private async Task EnsureCommittedRunAsync(
        string runId,
        string taskId,
        string resultId,
        string manifestVersion,
        bool isHardened,
        CancellationToken cancellationToken)
    {
        if (await runRepository.GetByIdAsync(runId, cancellationToken) is not null)
        
            return;
        

        ArchitectureRun run = new()
        {
            RunId = runId,
            RequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            Status = ArchitectureRunStatus.Created,
            CreatedUtc = DemoUtc,
            CompletedUtc = null,
            CurrentManifestVersion = null
        };

        await runRepository.CreateAsync(run, cancellationToken);

        AgentTask task = new()
        {
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = isHardened
                ? "Hardened topology: add WAF, Key Vault references, and segmented subnets for retail APIs."
                : "Baseline topology: single App Service and SQL for retail checkout (minimal segmentation).",
            Status = AgentTaskStatus.Completed,
            CreatedUtc = DemoUtc,
            CompletedUtc = DemoUtc,
            EvidenceBundleRef = null,
            AllowedTools = [],
            AllowedSources = []
        };

        await taskRepository.CreateManyAsync([task], cancellationToken);

        AgentResult result = new()
        {
            ResultId = resultId,
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Claims =
            [
                isHardened
                    ? "Proposed hardened retail edge with WAF and private connectivity to payment dependencies."
                    : "Proposed consolidated App Service tier with direct SQL connectivity for faster initial rollout."
            ],
            EvidenceRefs = ["contoso-policy-retail-001"],
            Confidence = isHardened ? 0.88 : 0.72,
            Findings = [],
            ProposedChanges = null,
            CreatedUtc = DemoUtc
        };

        await resultRepository.CreateAsync(result, cancellationToken);

        GoldenManifest manifest = BuildManifest(runId, manifestVersion, isHardened);
        await manifestRepository.CreateAsync(manifest, cancellationToken);

        DecisionTrace trace = new()
        {
            TraceId = isHardened ? DemoIds.TraceHardened : DemoIds.TraceBaseline,
            RunId = runId,
            EventType = "ManifestCommitted",
            EventDescription = isHardened
                ? "Committed hardened Contoso retail manifest after governance review."
                : "Committed baseline Contoso retail manifest.",
            CreatedUtc = DemoUtc,
            Metadata = new Dictionary<string, string> { ["demo"] = "trusted-baseline-49R" }
        };

        await decisionTraceRepository.CreateManyAsync([trace], cancellationToken);

        await runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.Committed,
                currentManifestVersion: manifestVersion,
                completedUtc: DemoUtc,
                cancellationToken: cancellationToken)
            ;
    }

    private static GoldenManifest BuildManifest(string runId, string manifestVersion, bool isHardened)
    {
        ManifestGovernance gov = isHardened
            ? new ManifestGovernance
            {
                ComplianceTags = ["PCI-DSS", "SOC2"],
                PolicyConstraints = ["No public SQL endpoints", "Secrets in Key Vault only"],
                RequiredControls = ["WAF", "PrivateLink", "DefenderForCloud"],
                RiskClassification = "Moderate",
                CostClassification = "Moderate"
            }
            : new ManifestGovernance
            {
                ComplianceTags = ["PCI-DSS"],
                PolicyConstraints = ["HTTPS only"],
                RequiredControls = ["TLS-1.2"],
                RiskClassification = "High",
                CostClassification = "Low"
            };

        return new GoldenManifest
        {
            RunId = runId,
            SystemName = "Contoso Retail Platform",
            Services =
            [
                new ManifestService
                {
                    ServiceId = isHardened ? "svc-checkout-api-v2" : "svc-checkout-api-v1",
                    ServiceName = "Checkout API",
                    ServiceType = ServiceType.Api,
                    RuntimePlatform = isHardened ? RuntimePlatform.ContainerApps : RuntimePlatform.AppService,
                    Purpose = "Orchestrates cart and payment initiation.",
                    Tags = isHardened ? ["edge-hardened"] : ["legacy-monolith"],
                    RequiredControls = isHardened ? ["WAF", "ManagedIdentity"] : ["BasicAuthOff"]
                }
            ],
            Datastores =
            [
                new ManifestDatastore
                {
                    DatastoreId = isHardened ? "ds-orders-v2" : "ds-orders-v1",
                    DatastoreName = "Orders DB",
                    DatastoreType = DatastoreType.Sql,
                    RuntimePlatform = RuntimePlatform.SqlServer,
                    Purpose = "Order and payment state."
                }
            ],
            Relationships = [],
            Governance = gov,
            Metadata = new ManifestMetadata
            {
                ManifestVersion = manifestVersion,
                ParentManifestVersion = null,
                ChangeDescription = isHardened ? "Hardened retail posture" : "Baseline lift-and-shift",
                DecisionTraceIds = [],
                CreatedUtc = DemoUtc
            }
        };
    }

    private async Task EnsureGovernanceAsync(CancellationToken cancellationToken)
    {
        if (await approvalRepository.GetByIdAsync(ContosoRetailDemoIdentifiers.ApprovalRequest, cancellationToken) is null)
        {
            GovernanceApprovalRequest approval = new()
            {
                ApprovalRequestId = ContosoRetailDemoIdentifiers.ApprovalRequest,
                RunId = ContosoRetailDemoIdentifiers.RunHardened,
                ManifestVersion = ContosoRetailDemoIdentifiers.ManifestHardened,
                SourceEnvironment = GovernanceEnvironment.Dev,
                TargetEnvironment = GovernanceEnvironment.Test,
                Status = GovernanceApprovalStatus.Approved,
                RequestedBy = "demo.architect@contoso.com",
                ReviewedBy = "demo.reviewer@contoso.com",
                RequestComment = "Promote hardened retail manifest to test for integration validation.",
                ReviewComment = "Approved — controls and WAF requirements satisfied in manifest.",
                RequestedUtc = DemoUtc,
                ReviewedUtc = DemoUtc.AddHours(2)
            };

            await approvalRepository.CreateAsync(approval, cancellationToken);
        }

        IReadOnlyList<GovernancePromotionRecord> promos =
            await promotionRepository.GetByRunIdAsync(ContosoRetailDemoIdentifiers.RunHardened, cancellationToken);

        if (promos.All(p => p.PromotionRecordId != ContosoRetailDemoIdentifiers.PromotionRecord))
        {
            GovernancePromotionRecord promotion = new()
            {
                PromotionRecordId = ContosoRetailDemoIdentifiers.PromotionRecord,
                RunId = ContosoRetailDemoIdentifiers.RunHardened,
                ManifestVersion = ContosoRetailDemoIdentifiers.ManifestHardened,
                SourceEnvironment = GovernanceEnvironment.Dev,
                TargetEnvironment = GovernanceEnvironment.Test,
                PromotedBy = "demo.release@contoso.com",
                PromotedUtc = DemoUtc.AddHours(3),
                ApprovalRequestId = ContosoRetailDemoIdentifiers.ApprovalRequest,
                Notes = "Demo promotion after approval (trusted baseline seed)."
            };

            await promotionRepository.CreateAsync(promotion, cancellationToken);
        }

        await EnsureActivationAsync(
                ContosoRetailDemoIdentifiers.ActivationDev,
                ContosoRetailDemoIdentifiers.RunBaseline,
                ContosoRetailDemoIdentifiers.ManifestBaseline,
                GovernanceEnvironment.Dev,
                cancellationToken)
            ;

        await EnsureActivationAsync(
                ContosoRetailDemoIdentifiers.ActivationTest,
                ContosoRetailDemoIdentifiers.RunHardened,
                ContosoRetailDemoIdentifiers.ManifestHardened,
                GovernanceEnvironment.Test,
                cancellationToken)
            ;
    }

    private async Task EnsureActivationAsync(
        string activationId,
        string runId,
        string manifestVersion,
        string environment,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GovernanceEnvironmentActivation> rows =
            await activationRepository.GetByEnvironmentAsync(environment, cancellationToken);

        if (rows.Any(r => r.ActivationId == activationId))
        
            return;
        

        GovernanceEnvironmentActivation activation = new()
        {
            ActivationId = activationId,
            RunId = runId,
            ManifestVersion = manifestVersion,
            Environment = environment,
            IsActive = true,
            ActivatedUtc = DemoUtc
        };

        await activationRepository.CreateAsync(activation, cancellationToken);
    }

    /// <summary>Optional export <strong>history</strong> row for demos — not wired to consulting DOCX replay (no AnalysisRequestJson).</summary>
    private async Task EnsureExportRecordAsync(CancellationToken cancellationToken)
    {
        if (await runExportRecordRepository.GetByIdAsync(ContosoRetailDemoIdentifiers.ExportRecord, cancellationToken) is not null)
        
            return;
        

        RunExportRecord record = new()
        {
            ExportRecordId = ContosoRetailDemoIdentifiers.ExportRecord,
            RunId = ContosoRetailDemoIdentifiers.RunBaseline,
            ExportType = "ArchitectureAnalysis",
            Format = "Markdown",
            FileName = "contoso-baseline-architecture.md",
            TemplateProfile = "internal",
            TemplateProfileDisplayName = "Internal Technical Review",
            WasAutoSelected = false,
            ResolutionReason = "Demo seed export snapshot.",
            ManifestVersion = ContosoRetailDemoIdentifiers.ManifestBaseline,
            Notes = "Seeded by ArchiForge trusted baseline demo (export history only).",
            IncludedManifest = true,
            IncludedSummary = true,
            CreatedUtc = DemoUtc
        };

        await runExportRecordRepository.CreateAsync(record, cancellationToken);
    }

    private static class DemoIds
    {
        public const string TaskBaseline = "task-baseline-demo-topo";
        public const string TaskHardened = "task-hardened-demo-topo";
        public const string ResultBaseline = "result-baseline-demo-topo";
        public const string ResultHardened = "result-hardened-demo-topo";
        public const string TraceBaseline = "trace-baseline-demo-001";
        public const string TraceHardened = "trace-hardened-demo-001";
    }
}

