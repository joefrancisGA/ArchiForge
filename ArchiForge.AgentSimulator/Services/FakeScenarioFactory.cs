using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Findings;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentSimulator.Services;

public static class FakeScenarioFactory
{
    public static AgentResult CreateTopologyResult(
        string runId,
        string taskId,
        ArchitectureRequest request)
    {
        return new AgentResult
        {
            ResultId = Guid.NewGuid().ToString("N"),
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Claims =
            [
                $"Use App Service for the primary API for system '{request.SystemName}'.",
                "Use Azure AI Search for retrieval.",
                "Use SQL Server for metadata storage."
            ],
            EvidenceRefs =
            [
                "request",
                "catalog:azure-core-services",
                "catalog:azure-ai-search",
                "catalog:azure-sql"
            ],
            Confidence = 0.91,
            Findings =
            [
                new ArchitectureFinding
                {
                    FindingId = Guid.NewGuid().ToString("N"),
                    SourceAgent = AgentType.Topology,
                    Severity = "Info",
                    Category = "Topology",
                    Message = "Simple managed-service topology selected for initial implementation.",
                    EvidenceRefs = ["request"]
                }
            ],
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = Guid.NewGuid().ToString("N"),
                SourceAgent = AgentType.Topology,
                AddedServices =
                [
                    new ManifestService
                    {
                        ServiceId = "svc-api",
                        ServiceName = "rag-api",
                        ServiceType = ServiceType.Api,
                        RuntimePlatform = RuntimePlatform.AppService,
                        Purpose = "Primary API for RAG queries",
                        Tags = ["rag", "api"]
                    },
                    new ManifestService
                    {
                        ServiceId = "svc-search",
                        ServiceName = "rag-search",
                        ServiceType = ServiceType.SearchService,
                        RuntimePlatform = RuntimePlatform.AzureAiSearch,
                        Purpose = "Enterprise retrieval layer",
                        Tags = ["search", "retrieval"]
                    },
                    new ManifestService
                    {
                        ServiceId = "svc-openai",
                        ServiceName = "rag-openai",
                        ServiceType = ServiceType.AiService,
                        RuntimePlatform = RuntimePlatform.AzureOpenAi,
                        Purpose = "LLM inference service",
                        Tags = ["ai", "llm"]
                    }
                ],
                AddedDatastores =
                [
                    new ManifestDatastore
                    {
                        DatastoreId = "ds-meta",
                        DatastoreName = "rag-metadata",
                        DatastoreType = DatastoreType.Sql,
                        RuntimePlatform = RuntimePlatform.SqlServer,
                        Purpose = "Stores metadata and citations",
                        EncryptionAtRestRequired = true
                    }
                ],
                AddedRelationships =
                [
                    new ManifestRelationship
                    {
                        RelationshipId = "rel-api-search",
                        SourceId = "svc-api",
                        TargetId = "svc-search",
                        RelationshipType = RelationshipType.Calls,
                        Description = "API queries retrieval service"
                    },
                    new ManifestRelationship
                    {
                        RelationshipId = "rel-api-openai",
                        SourceId = "svc-api",
                        TargetId = "svc-openai",
                        RelationshipType = RelationshipType.Calls,
                        Description = "API sends prompts to LLM service"
                    },
                    new ManifestRelationship
                    {
                        RelationshipId = "rel-api-meta",
                        SourceId = "svc-api",
                        TargetId = "ds-meta",
                        RelationshipType = RelationshipType.WritesTo,
                        Description = "API writes metadata and citations"
                    }
                ],
                Warnings =
                [
                    "Topology is optimized for MVP simplicity."
                ]
            },
            CreatedUtc = DateTime.UtcNow
        };
    }

    public static AgentResult CreateCostResult(
        string runId,
        string taskId,
        ArchitectureRequest request)
    {
        return new AgentResult
        {
            ResultId = Guid.NewGuid().ToString("N"),
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Cost,
            Claims =
            [
                "App Service is lower operational overhead than AKS for initial delivery.",
                "Azure AI Search should be monitored as corpus size grows.",
                "Managed services reduce support burden."
            ],
            EvidenceRefs =
            [
                "request",
                "pricing-profile"
            ],
            Confidence = 0.82,
            Findings =
            [
                new ArchitectureFinding
                {
                    FindingId = Guid.NewGuid().ToString("N"),
                    SourceAgent = AgentType.Cost,
                    Severity = "Info",
                    Category = "Cost",
                    Message = "Managed services selected for predictable operational cost.",
                    EvidenceRefs = ["pricing-profile"]
                }
            ],
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = Guid.NewGuid().ToString("N"),
                SourceAgent = AgentType.Cost,
                Warnings =
                [
                    "Search and token usage should be tracked from day one."
                ]
            },
            CreatedUtc = DateTime.UtcNow
        };
    }

    public static AgentResult CreateComplianceResult(
        string runId,
        string taskId,
        ArchitectureRequest request)
    {
        var requiredControls = new List<string>
        {
            "Managed Identity",
            "Private Endpoints",
            "Key Vault"
        };

        if (request.Constraints.Any(c =>
            c.Contains("encryption", StringComparison.OrdinalIgnoreCase)))
        {
            requiredControls.Add("Encryption At Rest");
        }

        return new AgentResult
        {
            ResultId = Guid.NewGuid().ToString("N"),
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Compliance,
            Claims =
            [
                "Managed identity is required.",
                "Private endpoints are required for data-bearing services.",
                "Secrets should be stored in Key Vault."
            ],
            EvidenceRefs =
            [
                "policy-pack:enterprise-default",
                "policy-pack:azure-security-baseline"
            ],
            Confidence = 0.96,
            Findings =
            [
                new ArchitectureFinding
                {
                    FindingId = Guid.NewGuid().ToString("N"),
                    SourceAgent = AgentType.Compliance,
                    Severity = "High",
                    Category = "Compliance",
                    Message = "PrivateNetworkingRequired",
                    EvidenceRefs = ["policy-pack:enterprise-default"]
                },
                new ArchitectureFinding
                {
                    FindingId = Guid.NewGuid().ToString("N"),
                    SourceAgent = AgentType.Compliance,
                    Severity = "High",
                    Category = "Compliance",
                    Message = "ManagedIdentityRequired",
                    EvidenceRefs = ["policy-pack:azure-security-baseline"]
                }
            ],
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = Guid.NewGuid().ToString("N"),
                SourceAgent = AgentType.Compliance,
                RequiredControls = requiredControls,
                Warnings =
                [
                    "Any public network access should require explicit exception review."
                ]
            },
            CreatedUtc = DateTime.UtcNow
        };
    }

    public static AgentResult CreateCriticResult(
        string runId,
        string taskId,
        ArchitectureRequest request)
    {
        return new AgentResult
        {
            ResultId = Guid.NewGuid().ToString("N"),
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Critic,
            Claims =
            [
                "No critical architecture contradictions detected in the starter pattern.",
                "Private networking and managed identity controls are necessary."
            ],
            EvidenceRefs =
            [
                "request",
                "critic-checklist"
            ],
            Confidence = 0.78,
            Findings =
            [
                new ArchitectureFinding
                {
                    FindingId = Guid.NewGuid().ToString("N"),
                    SourceAgent = AgentType.Critic,
                    Severity = "Info",
                    Category = "Critic",
                    Message = "Starter architecture is coherent for an MVP.",
                    EvidenceRefs = ["critic-checklist"]
                }
            ],
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = Guid.NewGuid().ToString("N"),
                SourceAgent = AgentType.Critic,
                Warnings =
                [
                    "Future growth may justify revisiting hosting and indexing topology."
                ]
            },
            CreatedUtc = DateTime.UtcNow
        };
    }
}
