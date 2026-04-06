using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Requests;

using static ArchiForge.Contracts.Requests.RequestConstraintClassifier;

namespace ArchiForge.Application.Evidence;

/// <summary>
/// Default implementation of <see cref="IEvidenceBuilder"/> that produces a deterministic,
/// stub-catalog evidence package from an <see cref="ArchitectureRequest"/>.
/// </summary>
/// <remarks>
/// This implementation is suitable for development, integration tests, and demo environments.
/// It injects a fixed set of enterprise policies and service catalog entries rather than
/// querying a live policy or catalog store. Replace or decorate it in production when dynamic
/// catalog resolution is required.
/// </remarks>
public sealed class DefaultEvidenceBuilder : IEvidenceBuilder
{
    /// <inheritdoc />
    public Task<AgentEvidencePackage> BuildAsync(
        string runId,
        ArchitectureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        AgentEvidencePackage package = new()
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
        List<PolicyEvidence> policies =
        [
            new()
            {
                PolicyId = BuiltInPolicyIds.EnterpriseDefault,
                Title = "Enterprise Default Security Baseline",
                Summary = "Baseline governance expectations for internal enterprise workloads.",
                RequiredControls = ["RBAC", "Diagnostic Logging"],
                Tags = ["enterprise", "security", "baseline"]
            }
        ];

        if (HasManagedIdentityConstraint(request))
        
            policies.Add(new PolicyEvidence
            {
                PolicyId = BuiltInPolicyIds.ManagedIdentity,
                Title = "Managed Identity Required",
                Summary = "Services should prefer managed identity over embedded secrets.",
                RequiredControls = ["Managed Identity"],
                Tags = ["identity", "security"]
            });
        

        if (HasPrivateNetworkingConstraint(request))
        
            policies.Add(new PolicyEvidence
            {
                PolicyId = BuiltInPolicyIds.PrivateNetworking,
                Title = "Private Networking Required",
                Summary = "Data-bearing services should use private connectivity patterns.",
                RequiredControls = ["Private Endpoints", "Private Networking"],
                Tags = ["network", "security"]
            });
        

        if (HasEncryptionConstraint(request))
        
            policies.Add(new PolicyEvidence
            {
                PolicyId = BuiltInPolicyIds.EncryptionAtRest,
                Title = "Encryption At Rest Required",
                Summary = "Persistent storage must enforce encryption at rest.",
                RequiredControls = ["Encryption At Rest"],
                Tags = ["data", "security"]
            });
        

        return policies;
    }

    private static List<ServiceCatalogEvidence> BuildServiceCatalog(ArchitectureRequest request)
    {
        List<ServiceCatalogEvidence> services =
        [
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
        ];

        if (RequiresSearchCapability(request))
        
            services.Add(new ServiceCatalogEvidence
            {
                ServiceId = "svc-catalog-ai-search",
                ServiceName = "Azure AI Search",
                Category = "Search",
                Summary = "Managed search and retrieval platform suitable for enterprise RAG retrieval.",
                Tags = ["search", "retrieval", "rag"],
                RecommendedUseCases = ["EnterpriseSearch", "RagRetrieval"]
            });
        

        if (RequiresAiCapability(request))
        
            services.Add(new ServiceCatalogEvidence
            {
                ServiceId = "svc-catalog-azure-openai",
                ServiceName = "Azure OpenAI",
                Category = "AI",
                Summary = "Managed LLM inference service for summarization and generation.",
                Tags = ["ai", "llm", "generation"],
                RecommendedUseCases = ["Chat", "Summarization", "RagGeneration"]
            });
        

        return services;
    }

    private static List<PatternEvidence> BuildPatterns(ArchitectureRequest request)
    {
        List<PatternEvidence> patterns = [];

        if (RequiresSearchCapability(request))
        
            patterns.Add(new PatternEvidence
            {
                PatternId = "pattern-enterprise-rag",
                Name = "Enterprise RAG",
                Summary = "API + retrieval + metadata + model inference pattern for enterprise knowledge systems.",
                ApplicableCapabilities = ["Azure AI Search", "SQL", "Managed Identity"],
                SuggestedServices = ["Azure App Service", "Azure AI Search", "Azure OpenAI", "Azure SQL"]
            });
        

        return patterns;
    }

    // ReSharper disable once UnusedParameter.Local
    private static PriorManifestEvidence? BuildPriorManifest(ArchitectureRequest request)
    {
        // Return null until real manifest hydration is implemented; agents must not treat
        // an empty placeholder as valid prior-state evidence.
        return null;
    }

    private static List<EvidenceNote> BuildNotes(ArchitectureRequest request)
    {
        List<EvidenceNote> notes =
        [
            new()
            {
                NoteType = EvidenceNoteTypes.ExecutionMode,
                Message = "Evidence package was built using the default deterministic builder."
            }
        ];

        if (!string.IsNullOrWhiteSpace(request.PriorManifestVersion))
        
            notes.Add(new EvidenceNote
            {
                NoteType = EvidenceNoteTypes.PriorManifestUnavailable,
                Message = $"A prior manifest version '{request.PriorManifestVersion}' was requested " +
                          "but prior manifest hydration is not yet implemented. Agents should treat this as a greenfield design."
            });
        

        if (RequiresSearchCapability(request))
        
            notes.Add(new EvidenceNote
            {
                NoteType = EvidenceNoteTypes.PatternHint,
                Message = "Search-oriented architecture requested; enterprise RAG pattern is applicable."
            });
        

        return notes;
    }
}
