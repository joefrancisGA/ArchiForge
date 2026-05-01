namespace ArchLucid.Core.Configuration;

/// <summary>
///     Per-tenant estimated LLM spend (USD) per UTC calendar month from <see cref="ILlmCostEstimator" /> — warn near
///     <see cref="IncludedUsdPerUtcMonth" />, hard stop at <see cref="HardCutoffUsdPerUtcMonth" />.
/// </summary>
public sealed class LlmMonthlyTenantDollarBudgetOptions
{
    public const string SectionName = "LlmMonthlyTenantDollarBudget";

    /// <summary>
    ///     When true, <see cref="ArchLucid.AgentRuntime.LlmCompletionAccountingClient" /> enforces the monthly USD cap
    ///     (non-simulator providers only).
    /// </summary>
    public bool Enabled
    {
        get;
        set;
    }

    /// <summary>Plan “included” USD for UX — warn threshold is <c>IncludedUsdPerUtcMonth * WarnFraction</c>.</summary>
    public decimal IncludedUsdPerUtcMonth
    {
        get;
        set;
    } = 50m;

    /// <summary>Maximum estimated cumulative USD per tenant per UTC month before completions are rejected.</summary>
    public decimal HardCutoffUsdPerUtcMonth
    {
        get;
        set;
    } = 75m;

    /// <summary>Fraction of <see cref="IncludedUsdPerUtcMonth" /> at which to emit a once-per-month warning audit.</summary>
    public decimal WarnFraction
    {
        get;
        set;
    } = 0.75m;

    /// <summary>
    ///     Upper bound on prompt tokens for pre-call budget reservation when checking before the model returns usage.
    /// </summary>
    public int AssumedMaxPromptTokensPerRequest
    {
        get;
        set;
    } = 32_768;

    /// <summary>
    ///     Upper bound on completion tokens for pre-call budget reservation when checking before the model returns usage.
    /// </summary>
    public int AssumedMaxCompletionTokensPerRequest
    {
        get;
        set;
    } = 8_192;
}
