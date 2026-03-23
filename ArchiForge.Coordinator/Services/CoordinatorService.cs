using ArchiForge.ContextIngestion.Mapping;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Orchestration;

namespace ArchiForge.Coordinator.Services;

/// <summary>
/// Validates <see cref="ArchitectureRequest"/> input, delegates persistence to <see cref="IAuthorityRunOrchestrator"/>, and assembles <see cref="CoordinationResult"/> (run, evidence bundle, starter tasks, graph shell).
/// </summary>
public sealed class CoordinatorService(IAuthorityRunOrchestrator authorityRunOrchestrator) : ICoordinatorService
{
    /// <inheritdoc />
    public async Task<CoordinationResult> CreateRunAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default)
    {
        var output = new CoordinationResult();

        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            output.Errors.AddRange(validationErrors);
            return output;
        }

        var authorityRun = await authorityRunOrchestrator.ExecuteAsync(
            ContextIngestionRequestMapper.FromArchitectureRequest(request),
            cancellationToken);

        var runId = authorityRun.RunId.ToString("N");
        var run = BuildRunFromAuthority(authorityRun, request);

        var evidenceBundle = BuildEvidenceBundle(request);
        var tasks = BuildStarterTasks(runId, evidenceBundle, request);
        run.TaskIds = [.. tasks.Select(t => t.TaskId)];

        output.Run = run;
        output.EvidenceBundle = evidenceBundle;
        output.Tasks = tasks;

        return output;
    }

    private static List<string> ValidateRequest(ArchitectureRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.RequestId))
            errors.Add("RequestId is required.");

        if (string.IsNullOrWhiteSpace(request.SystemName))
            errors.Add("SystemName is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            errors.Add("Description is required.");

        return errors;
    }

    private static ArchitectureRun BuildRunFromAuthority(RunRecord authorityRun, ArchitectureRequest request)
    {
        return new ArchitectureRun
        {
            RunId = authorityRun.RunId.ToString("N"),
            RequestId = request.RequestId,
            Status = ArchitectureRunStatus.TasksGenerated,
            CreatedUtc = DateTime.UtcNow,
            CompletedUtc = null,
            CurrentManifestVersion = null,
            ContextSnapshotId = authorityRun.ContextSnapshotId?.ToString("N"),
            GraphSnapshotId = authorityRun.GraphSnapshotId,
            FindingsSnapshotId = authorityRun.FindingsSnapshotId,
            GoldenManifestId = authorityRun.GoldenManifestId,
            DecisionTraceId = authorityRun.DecisionTraceId,
            ArtifactBundleId = authorityRun.ArtifactBundleId,
            TaskIds = []
        };
    }

    private static EvidenceBundle BuildEvidenceBundle(ArchitectureRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["systemName"] = request.SystemName,
            ["environment"] = request.Environment,
            ["cloudProvider"] = request.CloudProvider.ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.PriorManifestVersion))
        {
            metadata["priorManifestVersion"] = request.PriorManifestVersion;
        }

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

    private static List<string> BuildPolicyRefs(ArchitectureRequest request)
    {
        var refs = new List<string>
        {
            "policy-pack:enterprise-default",
            "policy-pack:azure-security-baseline"
        };

        if (RequestConstraintClassifier.HasPrivateNetworkingConstraint(request))
            refs.Add("policy:private-networking-required");

        if (RequestConstraintClassifier.HasManagedIdentityConstraint(request))
            refs.Add("policy:managed-identity-required");

        if (RequestConstraintClassifier.HasEncryptionConstraint(request))
            refs.Add("policy:encryption-at-rest-required");

        return refs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> BuildServiceCatalogRefs(ArchitectureRequest request)
    {
        // catalog:azure-sql is always included because DefaultEvidenceBuilder always
        // provides SQL catalog evidence as a baseline service, keeping both in sync.
        var refs = new List<string>
        {
            "catalog:azure-core-services",
            "catalog:azure-sql"
        };

        if (RequestConstraintClassifier.RequiresSearchCapability(request))
            refs.Add("catalog:azure-ai-search");

        if (RequestConstraintClassifier.RequiresAiCapability(request))
            refs.Add("catalog:azure-ai-services");

        return refs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<AgentTask> BuildStarterTasks(
        string runId,
        EvidenceBundle evidenceBundle,
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

    private static AgentTask CreateTopologyTask(
        string runId,
        EvidenceBundle evidenceBundle,
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
            AllowedTools =
            [
                "service-catalog-reader",
                "pattern-library-reader"
            ],
            AllowedSources =
            [
                "architecture-request",
                "policy-pack",
                "service-catalog",
                "prior-manifest"
            ]
        };
    }

    private static AgentTask CreateCostTask(
        string runId,
        EvidenceBundle evidenceBundle,
        ArchitectureRequest request)
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
            AllowedTools =
            [
                "pricing-profile-reader",
                "cost-estimator"
            ],
            AllowedSources =
            [
                "architecture-request",
                "pricing-profile",
                "service-catalog",
                "prior-manifest"
            ]
        };
    }

    private static AgentTask CreateComplianceTask(
        string runId,
        EvidenceBundle evidenceBundle,
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
            AllowedTools =
            [
                "policy-pack-reader",
                "control-mapper"
            ],
            AllowedSources =
            [
                "architecture-request",
                "policy-pack",
                "service-catalog",
                "prior-manifest"
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

    private static AgentTask CreateCriticTask(
        string runId,
        EvidenceBundle evidenceBundle,
        ArchitectureRequest request)
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
            AllowedTools =
            [
                "architecture-review-checklist",
                "policy-pack-reader"
            ],
            AllowedSources =
            [
                "architecture-request",
                "policy-pack",
                "service-catalog",
                "prior-manifest"
            ]
        };
    }

    private static string BuildCriticObjective(ArchitectureRequest request)
    {
        return
            $"Critique the implied architecture for system '{request.SystemName}' " +
            $"and identify omissions, contradictions, or weak assumptions " +
            $"that may undermine enterprise readiness or governance.";
    }
}
