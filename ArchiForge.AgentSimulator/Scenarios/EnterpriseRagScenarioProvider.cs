using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentSimulator.Scenarios;

public sealed class EnterpriseRagScenarioProvider : IScenarioProvider
{
    public const string ScenarioName = "enterprise-rag";

    public bool CanHandle(ArchitectureRequest request)
    {
        bool hasRagIndicators = request.Description.Contains("RAG", StringComparison.OrdinalIgnoreCase)
                                || request.SystemName.Contains("Rag", StringComparison.OrdinalIgnoreCase)
                                || request.RequiredCapabilities.Any(c =>
                                    c.Contains("Azure AI Search", StringComparison.OrdinalIgnoreCase) ||
                                    c.Contains("search", StringComparison.OrdinalIgnoreCase));

        return hasRagIndicators;
    }

    public string GetScenarioName(ArchitectureRequest request)
    {
        return CanHandle(request) ? ScenarioName : string.Empty;
    }
}
