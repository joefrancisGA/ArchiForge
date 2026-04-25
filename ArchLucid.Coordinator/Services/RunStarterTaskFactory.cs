using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Coordinator.Services;

/// <summary>
///     Shared evidence bundle and starter task construction for <see cref="CoordinatorService" /> and deferred authority
///     completion.
/// </summary>
public static class RunStarterTaskFactory
{
    private const string PolicyPackEnterpriseDefault = "policy-pack:enterprise-default";
    private const string PolicyPackAzureSecurityBaseline = "policy-pack:azure-security-baseline";
    private const string PolicyPrivateNetworkingRequired = "policy:private-networking-required";
    private const string PolicyManagedIdentityRequired = "policy:managed-identity-required";
    private const string PolicyEncryptionAtRestRequired = "policy:encryption-at-rest-required";
    private const string CatalogAzureCoreServices = "catalog:azure-core-services";
    private const string CatalogAzureSql = "catalog:azure-sql";
    private const string CatalogAzureAiSearch = "catalog:azure-ai-search";
    private const string CatalogAzureAiServices = "catalog:azure-ai-services";
    private const string ToolServiceCatalogReader = "service-catalog-reader";
    private const string ToolPatternLibraryReader = "pattern-library-reader";
    private const string ToolPricingProfileReader = "pricing-profile-reader";
    private const string ToolCostEstimator = "cost-estimator";
    private const string ToolPolicyPackReader = "policy-pack-reader";
    private const string ToolControlMapper = "control-mapper";
    private const string SourceArchitectureRequest = "architecture-request";
    private const string SourcePolicyPack = "policy-pack";
    private const string SourceServiceCatalog = "service-catalog";
    private const string SourcePriorManifest = "prior-manifest";
    private const string SourcePricingProfile = "pricing-profile";

    /// <summary>Builds the evidence bundle injected into every starter agent task.</summary>
    public static EvidenceBundle BuildEvidenceBundle(ArchitectureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase)
        {
            ["systemName"] = request.SystemName,
            ["environment"] = request.Environment,
            ["cloudProvider"] = request.CloudProvider.ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.PriorManifestVersion))
            metadata["priorManifestVersion"] = request.PriorManifestVersion;

        return new EvidenceBundle
        {
            EvidenceBundleId = Guid.NewGuid().ToString("N"),
            RequestDescription = request.Description,
            PolicyRefs = BuildPolicyRefs(request),
            ServiceCatalogRefs = BuildServiceCatalogRefs(request),
            PriorManifestRefs = string.IsNullOrWhiteSpace(request.PriorManifestVersion)
                ? []
                : [request.PriorManifestVersion],
            Metadata = metadata
        };
    }

    /// <summary>Creates topology, cost, compliance, and critic starter tasks for the run.</summary>
    public static List<AgentTask> BuildStarterTasks(string runId, EvidenceBundle evidenceBundle,
        ArchitectureRequest request)
    {
        return
        [
            CreateTopologyTask(runId, evidenceBundle, request),
            CreateCostTask(runId, evidenceBundle, request),
            CreateComplianceTask(runId, evidenceBundle, request),
            CreateCriticTask(runId, evidenceBundle, request)
        ];
    }

    private static List<string> BuildPolicyRefs(ArchitectureRequest request)
    {
        List<string> refs =
        [
            PolicyPackEnterpriseDefault,
            PolicyPackAzureSecurityBaseline
        ];

        if (RequestConstraintClassifier.HasPrivateNetworkingConstraint(request))
            refs.Add(PolicyPrivateNetworkingRequired);

        if (RequestConstraintClassifier.HasManagedIdentityConstraint(request))
            refs.Add(PolicyManagedIdentityRequired);

        if (RequestConstraintClassifier.HasEncryptionConstraint(request))
            refs.Add(PolicyEncryptionAtRestRequired);

        return refs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> BuildServiceCatalogRefs(ArchitectureRequest request)
    {
        List<string> refs =
        [
            CatalogAzureCoreServices,
            CatalogAzureSql
        ];

        if (RequestConstraintClassifier.RequiresSearchCapability(request))
            refs.Add(CatalogAzureAiSearch);

        if (RequestConstraintClassifier.RequiresAiCapability(request))
            refs.Add(CatalogAzureAiServices);

        return refs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static AgentTask CreateTopologyTask(string runId, EvidenceBundle evidenceBundle,
        ArchitectureRequest request)
    {
        return new AgentTask
        {
            TaskId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = BuildTopologyObjective(request),
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            CompletedUtc = null,
            EvidenceBundleRef = evidenceBundle.EvidenceBundleId,
            AllowedTools = [ToolServiceCatalogReader, ToolPatternLibraryReader],
            AllowedSources =
            [
                SourceArchitectureRequest,
                SourcePolicyPack,
                SourceServiceCatalog,
                SourcePriorManifest
            ]
        };
    }

    private static AgentTask CreateCostTask(string runId, EvidenceBundle evidenceBundle, ArchitectureRequest request)
    {
        return new AgentTask
        {
            TaskId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            AgentType = AgentType.Cost,
            Objective = BuildCostObjective(request),
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            CompletedUtc = null,
            EvidenceBundleRef = evidenceBundle.EvidenceBundleId,
            AllowedTools = [ToolPricingProfileReader, ToolCostEstimator],
            AllowedSources =
            [
                SourceArchitectureRequest,
                SourcePricingProfile,
                SourceServiceCatalog,
                SourcePriorManifest
            ]
        };
    }

    private static AgentTask CreateComplianceTask(string runId, EvidenceBundle evidenceBundle,
        ArchitectureRequest request)
    {
        return new AgentTask
        {
            TaskId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            AgentType = AgentType.Compliance,
            Objective = BuildComplianceObjective(request),
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            CompletedUtc = null,
            EvidenceBundleRef = evidenceBundle.EvidenceBundleId,
            AllowedTools = [ToolPolicyPackReader, ToolControlMapper],
            AllowedSources =
            [
                SourceArchitectureRequest,
                SourcePolicyPack,
                SourceServiceCatalog,
                SourcePriorManifest
            ]
        };
    }

    private static AgentTask CreateCriticTask(string runId, EvidenceBundle evidenceBundle, ArchitectureRequest request)
    {
        return new AgentTask
        {
            TaskId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            AgentType = AgentType.Critic,
            Objective = BuildCriticObjective(request),
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            CompletedUtc = null,
            EvidenceBundleRef = evidenceBundle.EvidenceBundleId,
            AllowedTools = ["architecture-review-checklist", ToolPolicyPackReader],
            AllowedSources =
            [
                SourceArchitectureRequest,
                SourcePolicyPack,
                SourceServiceCatalog,
                SourcePriorManifest
            ]
        };
    }

    private static string BuildTopologyObjective(ArchitectureRequest request)
    {
        return
            $"Design an initial Azure topology for system '{request.SystemName}' " +
            $"in environment '{request.Environment}'. " +
            $"Description: {request.Description}";
    }

    private static string BuildCostObjective(ArchitectureRequest request)
    {
        return
            $"Estimate cost posture and cost-sensitive design considerations for system '{request.SystemName}'. " +
            $"Required capabilities: {string.Join(", ", request.RequiredCapabilities)}";
    }

    private static string BuildComplianceObjective(ArchitectureRequest request)
    {
        return
            $"Validate the proposed architecture for system '{request.SystemName}' " +
            $"against policy constraints: {string.Join(", ", request.Constraints)}";
    }

    private static string BuildCriticObjective(ArchitectureRequest request)
    {
        return
            $"Critique the implied architecture for system '{request.SystemName}' " +
            $"and identify omissions, contradictions, or weak assumptions " +
            $"that may undermine enterprise readiness or governance.";
    }
}
