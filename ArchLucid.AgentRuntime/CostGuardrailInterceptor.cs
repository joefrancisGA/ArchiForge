using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>
///     Intercepts LLM completion calls to track cumulative token usage and cost across a single run.
///     Throws <see cref="CostLimitExceededException"/> if configured limits are breached.
/// </summary>
public sealed class CostGuardrailInterceptor(
    IAgentCompletionClient inner,
    IOptions<AgentOutputQualityGateOptions> options,
    ILlmCostEstimator costEstimator) : IAgentCompletionClient
{
    private readonly IAgentCompletionClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly IOptions<AgentOutputQualityGateOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILlmCostEstimator _costEstimator = costEstimator ?? throw new ArgumentNullException(nameof(costEstimator));

    private int _totalInputTokens;
    private int _totalOutputTokens;

    /// <inheritdoc />
    public LlmProviderDescriptor Descriptor => _inner.Descriptor;

    /// <inheritdoc />
    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        string result = await _inner.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);

        AgentCompletionTokenUsage.TryPeek(out int? inTok, out int? outTok);

        _totalInputTokens += inTok ?? 0;
        _totalOutputTokens += outTok ?? 0;

        AgentOutputQualityGateOptions opts = _options.Value;

        if (opts.MaxTokensPerRun.HasValue && (_totalInputTokens + _totalOutputTokens) > opts.MaxTokensPerRun.Value)
            throw new CostLimitExceededException($"Run exceeded maximum allowed tokens ({opts.MaxTokensPerRun.Value}).");

        if (!opts.MaxCostPerRun.HasValue)
            return result;

        decimal cost = _costEstimator.EstimateUsd(_totalInputTokens, _totalOutputTokens) ?? 0m;

        return cost > opts.MaxCostPerRun.Value ? throw new CostLimitExceededException($"Run exceeded maximum allowed cost (${opts.MaxCostPerRun.Value}).") : result;
    }
}
