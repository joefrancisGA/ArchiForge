using ArchLucid.Contracts.Abstractions.Agents;

namespace ArchLucid.Application.Agents;
/// <summary>
///     Default resolver that returns the same <see cref = "IAgentExecutor"/> for all known execution modes.
///     All modes (<c>Current</c>, <c>Deterministic</c>, <c>Replay</c>) are dispatched to the single
///     injected executor; mode-specific behaviour is handled by the executor or its upstream callers.
/// </summary>
public sealed class DefaultAgentExecutorResolver(IAgentExecutor currentExecutor) : IAgentExecutorResolver
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(currentExecutor);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Contracts.Abstractions.Agents.IAgentExecutor currentExecutor)
    {
        ArgumentNullException.ThrowIfNull(currentExecutor);
        return (byte)0;
    }

    private static readonly HashSet<string> KnownModes = new(StringComparer.OrdinalIgnoreCase)
    {
        ExecutionModes.Current,
        ExecutionModes.Deterministic,
        ExecutionModes.Replay
    };
    /// <inheritdoc/>
    public IAgentExecutor Resolve(string executionMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionMode);
        if (!KnownModes.Contains(executionMode))
            throw new ArgumentException($"Unknown execution mode '{executionMode}'. Supported modes: {string.Join(", ", KnownModes)}.", nameof(executionMode));
        return currentExecutor;
    }
}