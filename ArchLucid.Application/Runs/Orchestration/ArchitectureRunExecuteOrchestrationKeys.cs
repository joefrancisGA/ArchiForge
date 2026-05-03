namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>DI keys for alternate <see cref="IArchitectureRunExecuteOrchestrator" /> registrations.</summary>
public static class ArchitectureRunExecuteOrchestrationKeys
{
    /// <summary>
    ///     Anonymous marketing quick-start: execute with simulator trace recording (never real LLMs) even when
    ///     <c>AgentExecution:Mode</c> is Real.
    /// </summary>
    public const string QuickStartForcedSimulator = nameof(QuickStartForcedSimulator);
}
