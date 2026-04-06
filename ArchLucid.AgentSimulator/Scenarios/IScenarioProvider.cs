using ArchiForge.Contracts.Requests;

using JetBrains.Annotations;

namespace ArchiForge.AgentSimulator.Scenarios;

public interface IScenarioProvider
{
    [UsedImplicitly]
    bool CanHandle(ArchitectureRequest request);
}
