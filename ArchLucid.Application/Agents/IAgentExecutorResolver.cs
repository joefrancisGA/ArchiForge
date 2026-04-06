using ArchiForge.AgentSimulator.Services;

namespace ArchiForge.Application.Agents;

/// <summary>
/// Resolves the appropriate <see cref="IAgentExecutor"/> implementation for a given execution mode
/// (e.g. <c>"Current"</c>, <c>"Deterministic"</c>, <c>"Replay"</c>).
/// </summary>
public interface IAgentExecutorResolver
{
    /// <summary>
    /// Returns the executor for <paramref name="executionMode"/>.
    /// </summary>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="executionMode"/> is not a known mode.</exception>
    IAgentExecutor Resolve(string executionMode);
}
