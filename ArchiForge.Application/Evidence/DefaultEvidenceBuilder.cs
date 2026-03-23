using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Requests;

using static ArchiForge.Contracts.Requests.RequestConstraintClassifier;

namespace ArchiForge.Application.Evidence;

public sealed class DefaultEvidenceBuilder : IEvidenceBuilder
{
    public Task<AgentEvidencePackage> BuildAsync(
        string runId,
        ArchitectureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(request);

        var package = new AgentEvidencePackage
        {
            EvidencePackageId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            RequestId = request.RequestId,
            SystemName = request.SystemName,
            Environment = request.Environment,
            CloudProvider = request.CloudProvider.ToString(),
            Request = new RequestEvidence
            {
                Description = request.Description,
                Constraints = request.Constraints.ToList(),
                RequiredCapabilities = request.RequiredCapabilities.ToList(),
                Assumptions = request.Assumptions.ToList()
            },
            Policies = BuildPolicies(request),
            ServiceCatalog = BuildServiceCatalog(request),
            Patterns = BuildPatterns(request),
            PriorManifest = BuildPriorManifest(request),
            Notes = BuildNotes(request),
            CreatedUtc = DateTime.UtcNow
        };

        return Task.FromResult(package);
    }

    private static List<PolicyEvidence> BuildPolicies(ArchitectureRequest request)
    {
        var policies = new List<PolicyEvidence>
        {
            new()
            {
                PolicyId = "policy-enterprise-default",
                Title = "Enterprise Default Security Baseline",
                Summary = "Baseline governance expectations for internal enterprise workloads.",
                RequiredControls = ["RBAC", "Diagnostic Logging"],
                Tags = ["enterprise", "security", "baseline"]
            }
        };

        if (HasManagedIdentityConstraint(request))
        {
            policies.Add(new PolicyEvidence
            {
                PolicyId = "policy-managed-identity",
                Title = "Managed Identity Required",
                Summary = "Services should prefer managed identity over embedded secrets.",
                RequiredControls = ["Managed Identity"],
                Tags = ["identity", "security"]
            });
        }

        if (HasPrivateNetworkingConstraint(request))
        {
            policies.Add(new PolicyEvidence
            {
                PolicyId = "policy-private-networking",
                Title = "Private Networking Required",
                Summary = "Data-bearing services should use private connectivity patterns.",
                RequiredControls = ["Private Endpoints", "Private Networking"],
                Tags = ["network", "security"]
            });
        }

        if (HasEncryptionConstraint(request))
        {
            policies.Add(new PolicyEvidence
            {
                PolicyId = "policy-encryption-at-rest",
                Title = "Encryption At Rest Required",
                Summary = "Persistent storage must enforce encryption at rest.",
                RequiredControls = ["Encryption At Rest"],
                Tags = ["data", "security"]
            });
        }

        return policies;
    }

    private static List<ServiceCatalogEvidence> BuildServiceCatalog(ArchitectureRequest request)
    {
        var services = new List<ServiceCatalogEvidence>
        {
            new()
            {
                ServiceId = "svc-catalog-app-service",
                ServiceName = "Azure App Service",
                Category = "Compute",
                Summary = "Managed web/API hosting platform suitable for MVP and enterprise web workloads.",
                Tags = ["compute", "managed", "web", "api"],
                RecommendedUseCases = ["Api", "Ui", "LowOpsMvp"]
            },
            new()
            {
                ServiceId = "svc-catalog-sql",
                ServiceName = "Azure SQL / SQL Server",
                Category = "Data",
                Summary = "Relational storage for application and metadata workloads.",
                Tags = ["data", "sql", "relational"],
                RecommendedUseCases = ["Metadata", "TransactionalData"]
            }
        };

        if (RequiresSearchCapability(request))
        {
            services.Add(new ServiceCatalogEvidence
            {
                ServiceId = "svc-catalog-ai-search",
                ServiceName = "Azure AI Search",
                Category = "Search",
                Summary = "Managed search and retrieval platform suitable for enterprise RAG retrieval.",
                Tags = ["search", "retrieval", "rag"],
                RecommendedUseCases = ["EnterpriseSearch", "RagRetrieval"]
            });
        }

        if (RequiresAiCapability(request))
        {
            services.Add(new ServiceCatalogEvidence
            {
                ServiceId = "svc-catalog-azure-openai",
                ServiceName = "Azure OpenAI",
                Category = "AI",
                Summary = "Managed LLM inference service for summarization and generation.",
                Tags = ["ai", "llm", "generation"],
                RecommendedUseCases = ["Chat", "Summarization", "RagGeneration"]
            });
        }

        return services;
    }

    private static List<PatternEvidence> BuildPatterns(ArchitectureRequest request)
    {
        var patterns = new List<PatternEvidence>();

        if (RequiresSearchCapability(request))
        {
            patterns.Add(new PatternEvidence
            {
                PatternId = "pattern-enterprise-rag",
                Name = "Enterprise RAG",
                Summary = "API + retrieval + metadata + model inference pattern for enterprise knowledge systems.",
                ApplicableCapabilities = ["Azure AI Search", "SQL", "Managed Identity"],
                SuggestedServices = ["Azure App Service", "Azure AI Search", "Azure OpenAI", "Azure SQL"]
            });
        }

        return patterns;
    }

    private static PriorManifestEvidence? BuildPriorManifest(ArchitectureRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PriorManifestVersion))
        {
            return null;
        }

        return new PriorManifestEvidence
        {
            ManifestVersion = request.PriorManifestVersion,
            Summary = "Prior manifest reference supplied, but hydrated prior manifest loading is not yet implemented.",
            ExistingServices = [],
            ExistingDatastores = [],
            ExistingRequiredControls = []
        };
    }

    private static List<EvidenceNote> BuildNotes(ArchitectureRequest request)
    {
        var notes = new List<EvidenceNote>
        {
            new()
            {
                NoteType = "ExecutionMode",
                Message = "Evidence package was built using the default deterministic builder."
            }
        };

        if (RequiresSearchCapability(request))
        {
            notes.Add(new EvidenceNote
            {
                NoteType = "PatternHint",
                Message = "Search-oriented architecture requested; enterprise RAG pattern is applicable."
            });
        }

        return notes;
    }
}
