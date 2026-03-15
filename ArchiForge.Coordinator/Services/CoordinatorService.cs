using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Coordinator.Services;

public sealed class CoordinatorService : ICoordinatorService
{
    public CoordinationResult CreateRun(ArchitectureRequest request)
    {
        var output = new CoordinationResult();

        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            output.Errors.AddRange(validationErrors);
            return output;
        }

        var runId = Guid.NewGuid().ToString("N");
        var evidenceBundle = BuildEvidenceBundle(request);
        var tasks = BuildStarterTasks(runId, evidenceBundle, request);
        var run = BuildRun(runId, request, tasks);

        output.Run = run;
        output.EvidenceBundle = evidenceBundle;
        output.Tasks = tasks;

        return output;
    }

    private static List<string> ValidateRequest(ArchitectureRequest request)
    {
        var errors = new List<string>();

        if (request is null)
        {
            errors.Add("Architecture request is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
            errors.Add("RequestId is required.");

        if (string.IsNullOrWhiteSpace(request.SystemName))
            errors.Add("SystemName is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            errors.Add("Description is required.");

        return errors;
    }

    private static ArchitectureRun BuildRun(
        string runId,
        ArchitectureRequest request,
        IReadOnlyCollection<AgentTask> tasks)
    {
        return new ArchitectureRun
        {
            RunId = runId,
            RequestId = request.RequestId,
            Status = ArchitectureRunStatus.TasksGenerated,
            CreatedUtc = DateTime.UtcNow,
            CompletedUtc = null,
            CurrentManifestVersion = null,
            TaskIds = tasks.Select(t => t.TaskId).ToList()
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

        if (request.Constraints.Any(c => c.Contains("private", StringComparison.OrdinalIgnoreCase)))
        {
            refs.Add("policy:private-networking-required");
        }

        if (request.Constraints.Any(c => c.Contains("managed identity", StringComparison.OrdinalIgnoreCase)))
        {
            refs.Add("policy:managed-identity-required");
        }

        return refs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> BuildServiceCatalogRefs(ArchitectureRequest request)
    {
        var refs = new List<string>
        {
            "catalog:azure-core-services"
        };

        if (request.RequiredCapabilities.Any(c =>
            c.Contains("search", StringComparison.OrdinalIgnoreCase)))
        {
            refs.Add("catalog:azure-ai-search");
        }

        if (request.RequiredCapabilities.Any(c =>
            c.Contains("sql", StringComparison.OrdinalIgnoreCase)))
        {
            refs.Add("catalog:azure-sql");
        }

        if (request.RequiredCapabilities.Any(c =>
            c.Contains("openai", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("ai", StringComparison.OrdinalIgnoreCase)))
        {
            refs.Add("catalog:azure-ai-services");
        }

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