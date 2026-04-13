namespace ArchLucid.AgentRuntime;

/// <summary>
/// Optional USD cost estimation for LLM calls from reported token counts (Azure-style input/output split).
/// </summary>
public sealed class LlmCostEstimationOptions
{
    public const string SectionPath = "AgentExecution:LlmCostEstimation";

    /// <summary>When <see langword="false"/>, <see cref="ILlmCostEstimator"/> returns <see langword="null"/>.</summary>
    public bool Enabled { get; set; }

    /// <summary>USD per 1M prompt (input) tokens.</summary>
    public decimal InputUsdPerMillionTokens { get; set; }

    /// <summary>USD per 1M completion (output) tokens.</summary>
    public decimal OutputUsdPerMillionTokens { get; set; }
}
