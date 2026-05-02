namespace ArchLucid.Api.Tests;

public static class TestRequestFactory
{
    public static object CreateArchitectureRequest(string requestId = "REQ-TEST-001")
    {
        return new
        {
            requestId,
            description =
                "Design a secure Azure RAG system for enterprise internal documents using Azure AI Search, managed identity, private endpoints, SQL metadata storage, and moderate cost sensitivity.",
            systemName = "EnterpriseRag",
            environment = "prod",
            cloudProvider = 1,
            constraints = new[] { "Private endpoints required", "Use managed identity" },
            requiredCapabilities = new[] { "Azure AI Search", "SQL", "Managed Identity", "Private Networking" },
            // Authority commit projects RequiredControls from graph SecurityBaseline nodes (findings), not from agent deltas.
            securityBaselineHints = new[] { "Private Endpoints", "Managed Identity" },
            assumptions = Array.Empty<string>(),
            priorManifestVersion = (string?)null
        };
    }
}
