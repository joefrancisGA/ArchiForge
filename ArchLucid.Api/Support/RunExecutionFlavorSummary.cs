using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Api.Support;

/// <summary>
///     Buyer-facing execution summary for run detail payloads — aligns with first-value report provenance semantics.
/// </summary>
internal static class RunExecutionFlavorSummary
{
    /// <summary>
    ///     Short paragraph explaining agent execution for sponsors. <paramref name="hostAgentExecutionMode" /> reflects
    ///     <c>AgentExecution:Mode</c> on the API host at HTTP request time (not a historical snapshot if configuration
    ///     changes).
    /// </summary>
    public static string Build(ArchitectureRun run, string? hostAgentExecutionMode)
    {
        return run is null ? throw new ArgumentNullException(nameof(run)) : Build(run.RealModeFellBackToSimulator, hostAgentExecutionMode);
    }

    /// <summary>
    ///     Same semantics as <see cref="Build(ArchitectureRun, string?)" /> using persisted fallback flag from any run
    ///     read model (authority or architecture aggregate).
    /// </summary>
    public static string Build(bool realModeFellBackToSimulator, string? hostAgentExecutionMode)
    {
        string mode = string.IsNullOrWhiteSpace(hostAgentExecutionMode) ? "Simulator" : hostAgentExecutionMode.Trim();

        if (realModeFellBackToSimulator)
        {
            return "Part of this architecture review used deterministic output after a live-model path failed. Treat numeric highlights cautiously; open the first-value report for the full execution provenance table.";
        }

        return string.Equals(mode, "Real", StringComparison.OrdinalIgnoreCase) ? "Agent steps for this review used the live model path, subject to this API host’s execution configuration when you loaded this page." : "Agent steps for this review used deterministic simulator execution (no live LLM calls for agent work).";
    }
}
