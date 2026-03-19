using ArchiForge.AgentSimulator.Services;

namespace ArchiForge.Application.Agents;

public sealed class DefaultAgentExecutorResolver(IAgentExecutor currentExecutor) : IAgentExecutorResolver
{
    public IAgentExecutor Resolve(string executionMode)
    {
        return currentExecutor;
    }
}
