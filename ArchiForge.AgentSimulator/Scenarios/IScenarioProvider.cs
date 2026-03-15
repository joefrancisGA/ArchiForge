using ArchiForge.Contracts.Requests;
using ArchiForge.AgentSimulator.Models;

namespace ArchiForge.AgentSimulator.Scenarios;

public interface IScenarioProvider
{
    bool CanHandle(ArchitectureRequest request);

    string GetScenarioName(ArchitectureRequest request);
}
