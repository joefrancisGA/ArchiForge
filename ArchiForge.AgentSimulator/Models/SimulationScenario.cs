namespace ArchiForge.AgentSimulator.Models;

public sealed class SimulationScenario
{
    public string ScenarioName { get; set; } = string.Empty;

    public List<AgentResultTemplate> Results { get; set; } = [];
}

public sealed class AgentResultTemplate
{
    public string AgentType { get; set; } = string.Empty;

    public List<string> Claims { get; set; } = [];

    public List<string> EvidenceRefs { get; set; } = [];

    public double Confidence { get; set; }

    public object? ProposedChanges { get; set; }
}
