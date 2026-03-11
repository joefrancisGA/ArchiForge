using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using System;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Manifest;
using ArchiForge.DecisionEngine.Services;

var request = new ArchitectureRequest
{
    RequestId = "REQ-001",
    SystemName = "EnterpriseRag",
    Description = "Design a secure Azure RAG system for enterprise internal documents.",
    Environment = "prod",
    Constraints =
    [
        "Private endpoints required",
        "Use managed identity"
    ],
    RequiredCapabilities =
    [
        "Azure AI Search",
        "SQL",
        "Managed Identity"
    ]
};

var coordinator = new CoordinatorService();
var result = coordinator.CreateRun(request);

if (!result.Success)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"ERROR: {error}");
    }

    return;
}

Console.WriteLine($"Run ID: {result.Run.RunId}");
Console.WriteLine($"Evidence Bundle ID: {result.EvidenceBundle.EvidenceBundleId}");

foreach (var task in result.Tasks)
{
    Console.WriteLine($"{task.AgentType}: {task.Objective}");
}

var decisionServiceRequest = new ArchitectureRequest
{
    RequestId = "REQ-001",
    SystemName = "EnterpriseRag",
    Description = "Design a secure Azure RAG system for internal documents.",
    Environment = "prod",
    Constraints = ["Private endpoints required", "Use managed identity"],
    RequiredCapabilities = ["Azure AI Search", "Managed Identity", "Private Networking"]
};

var topologyResult = new AgentResult
{
    ResultId = "RES-TOPO-001",
    TaskId = "TASK-TOPO-001",
    RunId = "RUN-001",
    AgentType = AgentType.Topology,
    Claims = ["Use App Service for API", "Use Azure AI Search for retrieval"],
    EvidenceRefs = ["decisionServiceRequest", "service-catalog"],
    Confidence = 0.90,
    ProposedChanges = new ManifestDeltaProposal
    {
        ProposalId = "PROP-TOPO-001",
        SourceAgent = AgentType.Topology,
        AddedServices =
        [
            new ManifestService
            {
                ServiceId = "svc-api",
                ServiceName = "rag-api",
                ServiceType = ServiceType.Api,
                RuntimePlatform = RuntimePlatform.AppService,
                Purpose = "Primary API for RAG queries"
            },
            new ManifestService
            {
                ServiceId = "svc-search",
                ServiceName = "rag-search",
                ServiceType = ServiceType.SearchService,
                RuntimePlatform = RuntimePlatform.AzureAiSearch,
                Purpose = "Enterprise document retrieval"
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
                Purpose = "Metadata storage"
            }
        ],
        AddedRelationships =
        [
            new ManifestRelationship
            {
                RelationshipId = "rel-1",
                SourceId = "svc-api",
                TargetId = "svc-search",
                RelationshipType = RelationshipType.Calls,
                Description = "API queries search service"
            }
        ]
    }
};

var complianceResult = new AgentResult
{
    ResultId = "RES-COMP-001",
    TaskId = "TASK-COMP-001",
    RunId = "RUN-001",
    AgentType = AgentType.Compliance,
    Claims = ["Private endpoints required", "Managed identity required"],
    EvidenceRefs = ["policy-pack"],
    Confidence = 0.95,
    ProposedChanges = new ManifestDeltaProposal
    {
        ProposalId = "PROP-COMP-001",
        SourceAgent = AgentType.Compliance,
        RequiredControls =
        [
            "Managed Identity",
            "Key Vault",
            "Private Endpoints"
        ]
    }
};

var costResult = new AgentResult
{
    ResultId = "RES-COST-001",
    TaskId = "TASK-COST-001",
    RunId = "RUN-001",
    AgentType = AgentType.Cost,
    Claims = ["App Service is lower operational overhead than AKS for MVP."],
    EvidenceRefs = ["pricing-profile"],
    Confidence = 0.78,
    ProposedChanges = new ManifestDeltaProposal
    {
        ProposalId = "PROP-COST-001",
        SourceAgent = AgentType.Cost,
        Warnings =
        [
            "Azure AI Search cost should be monitored as index volume grows."
        ]
    }
};

var engine = new DecisionEngineService();

var merge = engine.MergeResults(
    decisionServiceRequest,
    manifestVersion: "v1",
    results: [topologyResult, complianceResult, costResult],
    parentManifestVersion: null);

if (!merge.Success)
{
    foreach (var error in merge.Errors)
    {
        Console.WriteLine($"ERROR: {error}");
    }

    return;
}

Console.WriteLine($"Manifest Version: {merge.Manifest.Metadata.ManifestVersion}");
Console.WriteLine($"System Name: {merge.Manifest.SystemName}");
Console.WriteLine($"Services: {merge.Manifest.Services.Count}");
Console.WriteLine($"Datastores: {merge.Manifest.Datastores.Count}");
Console.WriteLine($"Required Controls: {string.Join(", ", merge.Manifest.Governance.RequiredControls)}");
Console.WriteLine($"Warnings: {string.Join(" | ", merge.Warnings)}");