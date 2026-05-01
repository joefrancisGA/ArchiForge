using ArchLucid.Application.Authority;
using ArchLucid.Application.Common;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Bootstrap;

/// <summary>
///     Idempotent seed for the Contoso Retail Modernization **trusted baseline** (two committed runs, governance workflow,
///     activations).
/// </summary>
/// <remarks>
///     Persists via <c>ArchLucid.Persistence</c> repositories. **Authority-only after ADR 0030 PR A3 (2026-04-24):**
///     each demo run is inserted into <c>dbo.Runs</c> via <see cref="IRunRepository.SaveAsync" /> (project slug
///     <c>Contoso Retail Platform</c>, matching system-name-as-project-id from coordinator ingestion mapping).
///     Committed manifest bodies AND decision traces are written through
///     <see cref="IAuthorityCommittedManifestChainWriter" /> in a single FK-chain insert
///     (Snapshot rows + GoldenManifest + AuthorityDecisionTrace). The previous
///     <c>ICoordinatorDecisionTraceRepository</c> second write to <c>dbo.DecisionTraces</c> was removed when
///     the coordinator interfaces themselves were deleted in PR A3 — see
///     <c>docs/adr/0030-coordinator-authority-pipeline-unification.md</c>.
///     The export row is optional metadata for export history — not required for consulting DOCX replay. See
///     <c>docs/TRUSTED_BASELINE.md</c>.
/// </remarks>
public sealed class DemoSeedService(
    IArchitectureRequestRepository requestRepository,
    IRunRepository runRepository,
    IScopeContextProvider scopeContextProvider,
    IAgentTaskRepository taskRepository,
    IAgentResultRepository resultRepository,
    IAuthorityCommittedManifestChainWriter authorityCommittedManifestChainWriter,
    IOptionsMonitor<DemoOptions> demoOptions,
    IGovernanceApprovalRequestRepository approvalRepository,
    IGovernancePromotionRecordRepository promotionRepository,
    IGovernanceEnvironmentActivationRepository activationRepository,
    IRunExportRecordRepository runExportRecordRepository,
    IAuditService auditService,
    IActorContext actorContext,
    ILogger<DemoSeedService> logger) : IDemoSeedService
{
    private static readonly DateTime DemoUtc = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IActorContext
        _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));

    private readonly IAuditService
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IAuthorityCommittedManifestChainWriter _authorityCommittedManifestChainWriter =
        authorityCommittedManifestChainWriter
        ?? throw new ArgumentNullException(nameof(authorityCommittedManifestChainWriter));

    private readonly IOptionsMonitor<DemoOptions> _demoOptions =
        demoOptions ?? throw new ArgumentNullException(nameof(demoOptions));

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        ContosoRetailDemoIds demo = ContosoRetailDemoIds.ForTenant(scope.TenantId);

        await EnsureRequestAsync(demo, cancellationToken);
        await EnsureCommittedRunAsync(
                demo,
                demo.AuthorityRunBaselineId,
                demo.TaskBaseline,
                demo.ResultBaseline,
                demo.ManifestBaseline,
                demo.TraceBaseline,
                false,
                cancellationToken)
            ;
        await EnsureCommittedRunAsync(
                demo,
                demo.AuthorityRunHardenedId,
                demo.TaskHardened,
                demo.ResultHardened,
                demo.ManifestHardened,
                demo.TraceHardened,
                true,
                cancellationToken)
            ;
        await EnsureGovernanceAsync(demo, cancellationToken);
        await EnsureExportRecordAsync(demo, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))

            logger.LogInformation(
                "Demo seed completed (Contoso Retail Modernization). Runs: {Baseline}, {Hardened}.",
                demo.RunBaseline,
                demo.RunHardened);
    }

    private async Task EnsureRequestAsync(ContosoRetailDemoIds demo, CancellationToken cancellationToken)
    {
        if (await requestRepository.GetByIdAsync(demo.RequestId, cancellationToken) is not null)
            return;

        ArchitectureRequest request = new()
        {
            RequestId = demo.RequestId,
            Description =
                "Contoso Retail modernization — migrate monolith checkout to Azure with PCI-aware boundaries.",
            SystemName = "Contoso Retail Platform",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints = ["Minimize public ingress", "Retain existing payment processor integration"]
        };

        await requestRepository.CreateAsync(request, cancellationToken);
    }

    private async Task EnsureCommittedRunAsync(
        ContosoRetailDemoIds demo,
        Guid authorityRunId,
        string taskId,
        string resultId,
        string manifestVersion,
        string traceId,
        bool isHardened,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        if (await runRepository.GetByIdAsync(scope, authorityRunId, cancellationToken) is not null)
            return;

        string legacyRunId = authorityRunId.ToString("N");

        RunRecord authorityRow = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            RunId = authorityRunId,
            ProjectId = "Contoso Retail Platform",
            Description = isHardened
                ? "Demo — Contoso retail hardened manifest (trusted baseline seed)."
                : "Demo — Contoso retail baseline manifest (trusted baseline seed).",
            CreatedUtc = DemoUtc,
            ArchitectureRequestId = demo.RequestId,
            LegacyRunStatus = nameof(ArchitectureRunStatus.Created)
        };

        await runRepository.SaveAsync(authorityRow, cancellationToken);

        AgentTask task = new()
        {
            TaskId = taskId,
            RunId = legacyRunId,
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
            RunId = legacyRunId,
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

        bool richSeed = IsVerticalDemoSeedDepth(_demoOptions.CurrentValue.SeedDepth);
        GoldenManifest manifest = BuildManifest(legacyRunId, manifestVersion, isHardened, richSeed);
        AuthorityChainKeying chainKeying = new(
            AuthorityDemoChainIds.Manifest(authorityRunId),
            AuthorityDemoChainIds.ContextSnapshot(authorityRunId),
            AuthorityDemoChainIds.GraphSnapshot(authorityRunId),
            AuthorityDemoChainIds.FindingsSnapshot(authorityRunId),
            AuthorityDemoChainIds.DecisionTrace(authorityRunId));

        AuthorityManifestPersistResult authorityChain = await _authorityCommittedManifestChainWriter
            .PersistCommittedChainAsync(
                scope,
                authorityRunId,
                "Contoso Retail Platform",
                manifest,
                chainKeying,
                DemoUtc,
                richSeed,
                cancellationToken);

        await AuthorityCommittedChainDurableAudit.TryLogAsync(
            _auditService,
            scopeContextProvider,
            _actorContext,
            logger,
            authorityRunId,
            "Contoso Retail Platform",
            authorityChain,
            "demo-seed",
            richSeed,
            cancellationToken);

        // Decision-trace persistence happens inside PersistCommittedChainAsync above (AuthorityDecisionTrace
        // FK-chain row keyed by chainKeying.DecisionTraceId). The legacy second write to dbo.DecisionTraces
        // via ICoordinatorDecisionTraceRepository was removed in ADR 0030 PR A3 (2026-04-24) along with the
        // interface itself. The traceId / event-shape metadata is no longer surfaced for the demo seed because
        // ArchitectureRunDetail.DecisionTraces now reads from AuthorityDecisionTraces (see RunDetailQueryService).
        _ = traceId;

        RunRecord? authorityCommitted = await runRepository.GetByIdAsync(scope, authorityRunId, cancellationToken);

        if (authorityCommitted is not null)
        {
            authorityCommitted.LegacyRunStatus = nameof(ArchitectureRunStatus.Committed);
            authorityCommitted.CurrentManifestVersion = manifestVersion;
            authorityCommitted.CompletedUtc = DemoUtc;
            authorityCommitted.ContextSnapshotId = authorityChain.ContextSnapshotId;
            authorityCommitted.GraphSnapshotId = authorityChain.GraphSnapshotId;
            authorityCommitted.FindingsSnapshotId = authorityChain.FindingsSnapshotId;
            authorityCommitted.GoldenManifestId = authorityChain.GoldenManifestId;
            authorityCommitted.DecisionTraceId = authorityChain.DecisionTraceId;
            await runRepository.UpdateAsync(authorityCommitted, cancellationToken);
        }
    }

    private static bool IsVerticalDemoSeedDepth(string? seedDepth)
    {
        if (string.IsNullOrWhiteSpace(seedDepth))
            return false;

        return string.Equals(seedDepth.Trim(), "vertical", StringComparison.OrdinalIgnoreCase)
               || string.Equals(seedDepth.Trim(), "full", StringComparison.OrdinalIgnoreCase)
               || string.Equals(seedDepth.Trim(), "production-realistic", StringComparison.OrdinalIgnoreCase);
    }

    private static GoldenManifest BuildManifest(string runId, string manifestVersion, bool isHardened, bool richSeed)
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

        // ADR 0030 owner Decision B (2026-04-23): quickstart writes one-of-each minimum (single
        // service + datastore + relationship); vertical writes the production-realistic depth
        // (multiple services + datastore + relationships including a service-to-service edge).
        string checkoutServiceId = isHardened ? "svc-checkout-api-v2" : "svc-checkout-api-v1";
        string ordersDatastoreId = isHardened ? "ds-orders-v2" : "ds-orders-v1";

        List<ManifestService> services =
        [
            new()
            {
                ServiceId = checkoutServiceId,
                ServiceName = "Checkout API",
                ServiceType = ServiceType.Api,
                RuntimePlatform = isHardened ? RuntimePlatform.ContainerApps : RuntimePlatform.AppService,
                Purpose = "Orchestrates cart and payment initiation.",
                Tags = isHardened ? ["edge-hardened"] : ["legacy-monolith"],
                RequiredControls = isHardened ? ["WAF", "ManagedIdentity"] : ["BasicAuthOff"]
            }
        ];

        List<ManifestDatastore> datastores =
        [
            new()
            {
                DatastoreId = ordersDatastoreId,
                DatastoreName = "Orders DB",
                DatastoreType = DatastoreType.Sql,
                RuntimePlatform = RuntimePlatform.SqlServer,
                Purpose = "Order and payment state."
            }
        ];

        List<ManifestRelationship> relationships =
        [
            new()
            {
                RelationshipId = $"rel-{checkoutServiceId}-writes-{ordersDatastoreId}",
                SourceId = checkoutServiceId,
                TargetId = ordersDatastoreId,
                RelationshipType = RelationshipType.WritesTo,
                Description = "Checkout API persists order and payment state."
            }
        ];

        if (!richSeed)
            return new GoldenManifest
            {
                RunId = runId,
                SystemName = "Contoso Retail Platform",
                Services = services,
                Datastores = datastores,
                Relationships = relationships,
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
        string paymentServiceId = isHardened ? "svc-payment-gateway-v2" : "svc-payment-gateway-v1";

        services.Add(new ManifestService
        {
            ServiceId = paymentServiceId,
            ServiceName = "Payment Gateway",
            ServiceType = ServiceType.Api,
            RuntimePlatform = isHardened ? RuntimePlatform.ContainerApps : RuntimePlatform.AppService,
            Purpose = "Tokenizes card data and brokers payment provider calls.",
            Tags = isHardened ? ["edge-hardened", "pci-scope"] : ["pci-scope"],
            RequiredControls = isHardened ? ["WAF", "ManagedIdentity", "PrivateLink"] : ["TLS-1.2"]
        });

        relationships.Add(new ManifestRelationship
        {
            RelationshipId = $"rel-{checkoutServiceId}-calls-{paymentServiceId}",
            SourceId = checkoutServiceId,
            TargetId = paymentServiceId,
            RelationshipType = RelationshipType.Calls,
            Description = "Checkout API invokes the Payment Gateway during order finalization."
        });

        relationships.Add(new ManifestRelationship
        {
            RelationshipId = $"rel-{paymentServiceId}-reads-{ordersDatastoreId}",
            SourceId = paymentServiceId,
            TargetId = ordersDatastoreId,
            RelationshipType = RelationshipType.ReadsFrom,
            Description = "Payment Gateway reads order context for reconciliation."
        });

        return new GoldenManifest
        {
            RunId = runId,
            SystemName = "Contoso Retail Platform",
            Services = services,
            Datastores = datastores,
            Relationships = relationships,
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

    private async Task EnsureGovernanceAsync(ContosoRetailDemoIds demo, CancellationToken cancellationToken)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        if (await approvalRepository.GetByIdAsync(demo.ApprovalRequest, cancellationToken) is null)
        {
            GovernanceApprovalRequest approval = new()
            {
                ApprovalRequestId = demo.ApprovalRequest,
                RunId = demo.RunHardened,
                ManifestVersion = demo.ManifestHardened,
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

            StampGovernanceScope(scope, approval);

            await approvalRepository.CreateAsync(approval, cancellationToken);
        }

        IReadOnlyList<GovernancePromotionRecord> promos =
            await promotionRepository.GetByRunIdAsync(demo.RunHardened, cancellationToken);

        if (promos.All(p => p.PromotionRecordId != demo.PromotionRecord))
        {
            GovernancePromotionRecord promotion = new()
            {
                PromotionRecordId = demo.PromotionRecord,
                RunId = demo.RunHardened,
                ManifestVersion = demo.ManifestHardened,
                SourceEnvironment = GovernanceEnvironment.Dev,
                TargetEnvironment = GovernanceEnvironment.Test,
                PromotedBy = "demo.release@contoso.com",
                PromotedUtc = DemoUtc.AddHours(3),
                ApprovalRequestId = demo.ApprovalRequest,
                Notes = "Demo promotion after approval (trusted baseline seed)."
            };

            StampGovernanceScope(scope, promotion);

            await promotionRepository.CreateAsync(promotion, cancellationToken);
        }

        await EnsureActivationAsync(
                scope,
                demo.ActivationDev,
                demo.RunBaseline,
                demo.ManifestBaseline,
                GovernanceEnvironment.Dev,
                cancellationToken)
            ;

        await EnsureActivationAsync(
                scope,
                demo.ActivationTest,
                demo.RunHardened,
                demo.ManifestHardened,
                GovernanceEnvironment.Test,
                cancellationToken)
            ;
    }

    private async Task EnsureActivationAsync(
        ScopeContext scope,
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

        StampGovernanceScope(scope, activation);

        await activationRepository.CreateAsync(activation, cancellationToken);
    }

    private static void StampGovernanceScope(ScopeContext scope, GovernanceApprovalRequest row)
    {
        if (scope.TenantId == Guid.Empty)
            return;

        row.TenantId = scope.TenantId;
        row.WorkspaceId = scope.WorkspaceId;
        row.ProjectId = scope.ProjectId;
    }

    private static void StampGovernanceScope(ScopeContext scope, GovernancePromotionRecord row)
    {
        if (scope.TenantId == Guid.Empty)
            return;

        row.TenantId = scope.TenantId;
        row.WorkspaceId = scope.WorkspaceId;
        row.ProjectId = scope.ProjectId;
    }

    private static void StampGovernanceScope(ScopeContext scope, GovernanceEnvironmentActivation row)
    {
        if (scope.TenantId == Guid.Empty)
            return;

        row.TenantId = scope.TenantId;
        row.WorkspaceId = scope.WorkspaceId;
        row.ProjectId = scope.ProjectId;
    }

    /// <summary>
    ///     Optional export <strong>history</strong> row for demos — not wired to consulting DOCX replay (no
    ///     AnalysisRequestJson).
    /// </summary>
    private async Task EnsureExportRecordAsync(ContosoRetailDemoIds demo, CancellationToken cancellationToken)
    {
        if (await runExportRecordRepository.GetByIdAsync(demo.ExportRecord, cancellationToken) is not null)
            return;

        RunExportRecord record = new()
        {
            ExportRecordId = demo.ExportRecord,
            RunId = demo.RunBaseline,
            ExportType = "ArchitectureAnalysis",
            Format = "Markdown",
            FileName = "contoso-baseline-architecture.md",
            TemplateProfile = "internal",
            TemplateProfileDisplayName = "Internal Technical Review",
            WasAutoSelected = false,
            ResolutionReason = "Demo seed export snapshot.",
            ManifestVersion = demo.ManifestBaseline,
            Notes = "Seeded by ArchLucid trusted baseline demo (export history only).",
            IncludedManifest = true,
            IncludedSummary = true,
            CreatedUtc = DemoUtc
        };

        await runExportRecordRepository.CreateAsync(record, cancellationToken);
    }
}
