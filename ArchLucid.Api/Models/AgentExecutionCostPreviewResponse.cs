namespace ArchLucid.Api.Models;

/// <summary>Host-level AOAI cost preview for the operator new-run wizard (no tenant fields).</summary>
public sealed class AgentExecutionCostPreviewResponse
{
    /// <summary><c>Real</c> or <c>Simulator</c> from <c>AgentExecution:Mode</c>.</summary>
    public required string Mode
    {
        get;
        init;
    }

    /// <summary>Effective <c>AzureOpenAI:MaxCompletionTokens</c> (or default when unset/zero).</summary>
    public int MaxCompletionTokens
    {
        get;
        init;
    }

    /// <summary>
    ///     Same as <see cref="EstimatedCostUsdHigh" /> — conservative USD upper bound for a full starter run; kept for
    ///     backward-compatible clients that read a single number.
    /// </summary>
    public double? EstimatedCostUsd
    {
        get;
        init;
    }

    /// <summary>
    ///     USD lower bound: one completion with a small assumed prompt (8192 input tokens) at the host output cap, for
    ///     comparison only. Null when estimation is disabled or mode is Simulator.
    /// </summary>
    public double? EstimatedCostUsdLow
    {
        get;
        init;
    }

    /// <summary>
    ///     USD upper bound for a starter run: four parallel agent completions (Topology, Cost, Compliance, Critic),
    ///     each assumed to use up to <c>LlmDailyTenantBudget:AssumedMaxTotalTokensPerRequest</c> input tokens. Null when
    ///     estimation is disabled or mode is Simulator.
    /// </summary>
    public double? EstimatedCostUsdHigh
    {
        get;
        init;
    }

    /// <summary>Short operator-facing explanation of how low/high were derived (stable for UI copy).</summary>
    public required string EstimatedCostBasis
    {
        get;
        init;
    }

    /// <summary>
    ///     True when <c>AgentExecution:LlmCostEstimation</c> still uses shipped default USD/M rates; operators should
    ///     override to match their Azure OpenAI model list price.
    /// </summary>
    public bool PricingUsesIllustrativeUsdRates
    {
        get;
        init;
    }

    /// <summary>Configured deployment name when in Real mode; otherwise null.</summary>
    public string? DeploymentName
    {
        get;
        init;
    }
}
