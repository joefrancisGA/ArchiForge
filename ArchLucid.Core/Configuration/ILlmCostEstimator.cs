namespace ArchLucid.Core.Configuration;

/// <summary>Estimates LLM call cost from token usage and <see cref="LlmCostEstimationOptions" />.</summary>
public interface ILlmCostEstimator
{
    /// <summary>Returns <see langword="null" /> when estimation is disabled or counts are non-positive.</summary>
    decimal? EstimateUsd(int inputTokens, int outputTokens);
}
