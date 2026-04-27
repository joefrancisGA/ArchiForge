using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Core.Configuration;

/// <summary>
///     Optional USD cost estimation for LLM calls from reported token counts (Azure-style input/output split).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class LlmCostEstimationOptions
{
    public const string SectionPath = "AgentExecution:LlmCostEstimation";

    /// <summary>When <see langword="false" />, <see cref="ILlmCostEstimator" /> returns <see langword="null" />.</summary>
    /// <remarks>
    ///     Defaults to <see langword="true" /> so hosts without an explicit section still surface FinOps estimates;
    ///     disable via configuration when required.
    /// </remarks>
    public bool Enabled
    {
        get;
        set;
    } = true;

    /// <summary>USD per 1M prompt (input) tokens.</summary>
    public decimal InputUsdPerMillionTokens
    {
        get;
        set;
    } = 0.5m;

    /// <summary>USD per 1M completion (output) tokens.</summary>
    public decimal OutputUsdPerMillionTokens
    {
        get;
        set;
    } = 1.5m;
}
