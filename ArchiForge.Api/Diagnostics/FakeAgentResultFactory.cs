using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Findings;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Api.Diagnostics;


/* This gives you a deterministic way to generate:
    a topology proposal
    a cost proposal
    a compliance proposal

    ...using the real run and task IDs from your system.

    That means you can now do a real end-to-end smoke test without building live agents.*/

public static class FakeAgentResultFactory
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
                "Use Azure AI Search for enterprise retrieval.",
                "Use SQL Server for metadata storage."
            ],
            EvidenceRefs =
            [
                "request",
                "service-catalog:azure-core-services",
                "service-catalog:azure-ai-search",
                "service-catalog:azure-sql"
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
                    Message = "A simple App Service-based topology is appropriate for the initial implementation.",
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
                        Purpose = "Primary application API for retrieval-augmented generation requests",
                        Tags = ["rag", "api", "entrypoint"]
                    },
                    new ManifestService
                    {
                        ServiceId = "svc-search",
                        ServiceName = "rag-search",
                        ServiceType = ServiceType.SearchService,
                        RuntimePlatform = RuntimePlatform.AzureAiSearch,
                        Purpose = "Enterprise search and retrieval layer",
                        Tags = ["search", "retrieval", "index"]
                    },
                    new ManifestService
                    {
                        ServiceId = "svc-openai",
                        ServiceName = "rag-openai",
                        ServiceType = ServiceType.AiService,
                        RuntimePlatform = RuntimePlatform.AzureOpenAi,
                        Purpose = "LLM inference for summarization and response generation",
                        Tags = ["ai", "llm", "generation"]
                    }
                ],
                AddedDatastores =
                [
                    new ManifestDatastore
                    {
                        DatastoreId = "ds-metadata",
                        DatastoreName = "rag-metadata",
                        DatastoreType = DatastoreType.Sql,
                        RuntimePlatform = RuntimePlatform.SqlServer,
                        Purpose = "Stores document metadata, citations, and run metadata",
                        PrivateEndpointRequired = false,
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
                        Description = "API sends retrieval queries to search"
                    },
                    new ManifestRelationship
                    {
                        RelationshipId = "rel-api-openai",
                        SourceId = "svc-api",
                        TargetId = "svc-openai",
                        RelationshipType = RelationshipType.Calls,
                        Description = "API invokes LLM completions"
                    },
                    new ManifestRelationship
                    {
                        RelationshipId = "rel-api-metadata",
                        SourceId = "svc-api",
                        TargetId = "ds-metadata",
                        RelationshipType = RelationshipType.WritesTo,
                        Description = "API writes metadata and usage records"
                    }
                ],
                RequiredControls = [],
                Warnings =
                [
                    "Topology intentionally favors implementation simplicity over maximum future extensibility."
                ]
            },
            CreatedUtc = DateTime.UtcNow
        };
    }

    public static AgentResult CreateCostResult(
        string runId,
        string taskId,
        ArchitectureRequest _)
    {
        return new AgentResult
        {
            ResultId = Guid.NewGuid().ToString("N"),
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Cost,
            Claims =
            [
                "App Service is likely a lower operational overhead option than AKS for the initial MVP.",
                "Azure AI Search cost should be monitored as index size and query volume increase.",
                "Managed platform choices reduce support burden at modest additional platform cost."
            ],
            EvidenceRefs =
            [
                "request",
                "pricing-profile",
                "service-catalog:azure-core-services"
            ],
            Confidence = 0.79,
            Findings =
            [
                new ArchitectureFinding
                {
                    FindingId = Guid.NewGuid().ToString("N"),
                    SourceAgent = AgentType.Cost,
                    Severity = "Info",
                    Category = "Cost",
                    Message = "App Service minimizes operational complexity for initial deployment.",
                    EvidenceRefs = ["pricing-profile"]
                }
            ],
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = Guid.NewGuid().ToString("N"),
                SourceAgent = AgentType.Cost,
                AddedServices = [],
                AddedDatastores = [],
                AddedRelationships = [],
                RequiredControls = [],
                Warnings =
                [
                    "Search capacity and token usage should be tracked from the start.",
                    "Future high-scale growth may justify re-evaluating the hosting model."
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
        List<string> requiredControls = new()
        {
            "Managed Identity",
            "Key Vault",
            "Private Endpoints"
        };

        if (!request.RequiredCapabilities.Any(x =>
                x.Contains("private", StringComparison.OrdinalIgnoreCase)))
            return new AgentResult
            {
                ResultId = Guid.NewGuid().ToString("N"),
                TaskId = taskId,
                RunId = runId,
                AgentType = AgentType.Compliance,
                Claims =
                [
                    "Managed identity is required for service-to-service authentication.",
                    "Private endpoints are required for data-bearing services.",
                    "Secrets should be externalized into Key Vault."
                ],
                EvidenceRefs =
                [
                    "request",
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
                    AddedServices = [],
                    AddedDatastores = [],
                    AddedRelationships = [],
                    RequiredControls = requiredControls,
                    Warnings =
                    [
                        "Any public network exposure should be treated as an exception requiring explicit review."
                    ]
                },
                CreatedUtc = DateTime.UtcNow
            };
        
        if (!requiredControls.Contains("Private Networking", StringComparer.OrdinalIgnoreCase))
        {
            requiredControls.Add("Private Networking");
        }

        return new AgentResult
        {
            ResultId = Guid.NewGuid().ToString("N"),
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Compliance,
            Claims =
            [
                "Managed identity is required for service-to-service authentication.",
                "Private endpoints are required for data-bearing services.",
                "Secrets should be externalized into Key Vault."
            ],
            EvidenceRefs =
            [
                "request",
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
                AddedServices = [],
                AddedDatastores = [],
                AddedRelationships = [],
                RequiredControls = requiredControls,
                Warnings =
                [
                    "Any public network exposure should be treated as an exception requiring explicit review."
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
                $"The proposed topology for '{request.SystemName}' uses well-understood Azure services with no obvious architectural anti-patterns.",
                "Cost estimate assumptions are reasonable for an MVP scope; revisit as load increases.",
                "Compliance controls are aligned with the stated policy baseline."
            ],
            EvidenceRefs =
            [
                "request",
                "policy-pack:enterprise-default",
                "policy-pack:azure-security-baseline",
                "service-catalog:azure-core-services"
            ],
            Confidence = 0.85,
            Findings =
            [
                new ArchitectureFinding
                {
                    FindingId = Guid.NewGuid().ToString("N"),
                    SourceAgent = AgentType.Critic,
                    Severity = "Info",
                    Category = "Review",
                    Message = "No critical omissions or contradictions detected in the proposed architecture.",
                    EvidenceRefs = ["request", "policy-pack:enterprise-default"]
                }
            ],
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = Guid.NewGuid().ToString("N"),
                SourceAgent = AgentType.Critic,
                AddedServices = [],
                AddedDatastores = [],
                AddedRelationships = [],
                RequiredControls = [],
                Warnings =
                [
                    "Ensure observability stack (Application Insights or equivalent) is included before production."
                ]
            },
            CreatedUtc = DateTime.UtcNow
        };
    }

    public static IReadOnlyList<AgentResult> CreateStarterResults(
        string runId,
        IReadOnlyCollection<AgentTask> tasks,
        ArchitectureRequest request)
    {
        AgentTask topologyTask = tasks.FirstOrDefault(t => t.AgentType == AgentType.Topology)
                                 ?? throw new InvalidOperationException("Topology task was not found.");

        AgentTask costTask = tasks.FirstOrDefault(t => t.AgentType == AgentType.Cost)
                             ?? throw new InvalidOperationException("Cost task was not found.");

        AgentTask complianceTask = tasks.FirstOrDefault(t => t.AgentType == AgentType.Compliance)
                                   ?? throw new InvalidOperationException("Compliance task was not found.");

        AgentTask criticTask = tasks.FirstOrDefault(t => t.AgentType == AgentType.Critic)
                               ?? throw new InvalidOperationException("Critic task was not found.");

        return
        [
            CreateTopologyResult(runId, topologyTask.TaskId, request),
            CreateCostResult(runId, costTask.TaskId, request),
            CreateComplianceResult(runId, complianceTask.TaskId, request),
            CreateCriticResult(runId, criticTask.TaskId, request)
        ];
    }
}
