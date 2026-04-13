using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <inheritdoc cref="ILlmCostEstimator"/>
public sealed class LlmCostEstimator(IOptions<LlmCostEstimationOptions> options) : ILlmCostEstimator
{
    private readonly IOptions<LlmCostEstimationOptions> _options =
        options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public decimal? EstimateUsd(int inputTokens, int outputTokens)
    {
        LlmCostEstimationOptions o = _options.Value;

        if (!o.Enabled || inputTokens < 0 || outputTokens < 0)
            return null;

        if (inputTokens == 0 && outputTokens == 0)
            return null;

        decimal inPart = inputTokens * o.InputUsdPerMillionTokens / 1_000_000m;
        decimal outPart = outputTokens * o.OutputUsdPerMillionTokens / 1_000_000m;

        return inPart + outPart;
    }
}
