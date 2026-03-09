using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using System;

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