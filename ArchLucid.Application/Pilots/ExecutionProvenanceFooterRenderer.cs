using System.Globalization;

namespace ArchLucid.Application.Pilots;

/// <inheritdoc cref="IExecutionProvenanceFooterRenderer" />
public sealed class ExecutionProvenanceFooterRenderer : IExecutionProvenanceFooterRenderer
{
    /// <inheritdoc />
    public string BuildYellowSimulatorSubstitutionCallout()
    {
        return """
            > [!WARNING]
            > Real Azure OpenAI execution failed and was substituted with simulator output. The numbers below are deterministic placeholders, not LLM-generated. See `docs/runbooks/AGENT_EXECUTION_FAILURES.md` for triage.
            """;
    }

    /// <inheritdoc />
    public string BuildFooterMarkdown(ExecutionProvenanceFooterInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        string modeLabel = ResolveModeLabel(input);
        string deployment = input.RealModeFellBackToSimulator
            ? (string.IsNullOrWhiteSpace(input.PilotAoaiDeploymentSnapshot)
                ? "(unknown at fallback)"
                : input.PilotAoaiDeploymentSnapshot)
            : (string.IsNullOrWhiteSpace(input.HostAzureOpenAiDeploymentName)
                ? "(n/a)"
                : input.HostAzureOpenAiDeploymentName);

        return $"""
            ## Execution provenance

            | Field | Value |
            | --- | --- |
            | Mode | {modeLabel} |
            | LLM completion traces (this run) | {input.LlmCompletionTraceCount.ToString(CultureInfo.InvariantCulture)} |
            | Azure OpenAI deployment (when known) | `{deployment}` |

            _Token totals per provider are not aggregated in this report; trace count reflects persisted completion attempts._
            """;
    }

    private static string ResolveModeLabel(ExecutionProvenanceFooterInput input)
    {
        if (input.RealModeFellBackToSimulator)
            return "Real → Simulator (fallback)";

        return string.Equals(input.HostAgentExecutionMode, "Real", StringComparison.OrdinalIgnoreCase) ? "Real" : "Simulator";
    }
}
