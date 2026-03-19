using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentSimulator.Scenarios;

public interface IScenarioProvider
{
    bool CanHandle(ArchitectureRequest request);
}
