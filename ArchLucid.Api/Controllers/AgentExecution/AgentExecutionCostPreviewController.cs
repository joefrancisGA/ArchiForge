using ArchLucid.Api.Models;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Controllers.AgentExecution;

/// <summary>
///     Anonymous host configuration read for operator UI cost preview on the new-run wizard review step.
/// </summary>
[ApiController]
[Route("v{version:apiVersion}/agent-execution")]
[ApiVersion("1.0")]
[AllowAnonymous]
[EnableRateLimiting("fixed")]
[ProducesResponseType(typeof(AgentExecutionCostPreviewResponse), StatusCodes.Status200OK)]
public sealed class AgentExecutionCostPreviewController(
    IOptionsMonitor<AgentExecutionOptions> agentExecutionOptions,
    IOptionsMonitor<AzureOpenAiOptions> azureOpenAiOptions,
    IOptionsMonitor<LlmDailyTenantBudgetOptions> dailyTenantBudgetOptions,
    IOptionsMonitor<LlmCostEstimationOptions> llmCostEstimationOptions,
    ILlmCostEstimator llmCostEstimator) : ControllerBase
{
    /// <summary>Illustrative floor: one completion with a small prompt; see <c>docs/deployment/PER_TENANT_COST_MODEL.md</c>.</summary>
    private const int MinimalAssumedInputTokensPerCompletion = 8192;

    /// <summary>Starter-run parallel agents (Topology, Cost, Compliance, Critic).</summary>
    private const int StarterRunParallelAgentCount = 4;

    private readonly IOptionsMonitor<AgentExecutionOptions> _agentExecutionOptions =
        agentExecutionOptions ?? throw new ArgumentNullException(nameof(agentExecutionOptions));

    private readonly IOptionsMonitor<AzureOpenAiOptions> _azureOpenAiOptions =
        azureOpenAiOptions ?? throw new ArgumentNullException(nameof(azureOpenAiOptions));

    private readonly IOptionsMonitor<LlmDailyTenantBudgetOptions> _dailyTenantBudgetOptions =
        dailyTenantBudgetOptions ?? throw new ArgumentNullException(nameof(dailyTenantBudgetOptions));

    private readonly IOptionsMonitor<LlmCostEstimationOptions> _llmCostEstimationOptions =
        llmCostEstimationOptions ?? throw new ArgumentNullException(nameof(llmCostEstimationOptions));

    private readonly ILlmCostEstimator _llmCostEstimator =
        llmCostEstimator ?? throw new ArgumentNullException(nameof(llmCostEstimator));

    /// <summary>Returns execution mode, effective max completion tokens, optional USD estimate, and deployment name.</summary>
    [HttpGet("cost-preview")]
    public ActionResult<AgentExecutionCostPreviewResponse> GetCostPreview()
    {
        bool isReal = string.Equals(
            _agentExecutionOptions.CurrentValue.Mode.Trim(),
            "Real",
            StringComparison.OrdinalIgnoreCase);

        AzureOpenAiOptions azure = _azureOpenAiOptions.CurrentValue;
        int maxOut = azure.MaxCompletionTokens;

        if (maxOut <= 0)
            maxOut = AzureOpenAiOptions.DefaultMaxCompletionTokens;

        int assumedMaxInput = Math.Clamp(
            _dailyTenantBudgetOptions.CurrentValue.AssumedMaxTotalTokensPerRequest,
            1,
            2_000_000);

        LlmCostEstimationOptions costModel = _llmCostEstimationOptions.CurrentValue;
        LlmCostEstimationOptions shippedRateDefaults = new();

        bool illustrativeUsd =
            costModel.InputUsdPerMillionTokens == shippedRateDefaults.InputUsdPerMillionTokens
            && costModel.OutputUsdPerMillionTokens == shippedRateDefaults.OutputUsdPerMillionTokens;

        string modeLabel = isReal ? "Real" : "Simulator";
        string? deployment =
            isReal && !string.IsNullOrWhiteSpace(azure.DeploymentName) ? azure.DeploymentName.Trim() : null;

        double? lowUsd = null;
        double? highUsd = null;

        if (isReal && costModel.Enabled)
        {
            decimal? low = _llmCostEstimator.EstimateUsd(MinimalAssumedInputTokensPerCompletion, maxOut);
            decimal? perAgent = _llmCostEstimator.EstimateUsd(assumedMaxInput, maxOut);

            if (low is not null)
                lowUsd = (double)low.Value;

            if (perAgent is not null)
                highUsd = (double)(perAgent.Value * StarterRunParallelAgentCount);

            ArchLucidInstrumentation.RunsCostPreviewViewedTotal.Add(1);
        }

        AgentExecutionCostPreviewResponse body = new()
        {
            Mode = modeLabel,
            MaxCompletionTokens = maxOut,
            EstimatedCostUsd = highUsd,
            EstimatedCostUsdLow = lowUsd,
            EstimatedCostUsdHigh = highUsd,
            EstimatedCostBasis = BuildEstimatedCostBasis(assumedMaxInput, maxOut),
            PricingUsesIllustrativeUsdRates = illustrativeUsd,
            DeploymentName = deployment
        };

        return Ok(body);
    }

    private static string BuildEstimatedCostBasis(int assumedMaxInputTokens, int maxCompletionTokens)
    {
        return
            $"Starter run = {StarterRunParallelAgentCount} parallel agents (Topology, Cost, Compliance, Critic). "
            + $"Low = one completion at {MinimalAssumedInputTokensPerCompletion} assumed input tokens and "
            + $"{maxCompletionTokens} max output tokens. "
            + $"High = four completions each at up to LlmDailyTenantBudget:AssumedMaxTotalTokensPerRequest "
            + $"({assumedMaxInputTokens} assumed input tokens) and AzureOpenAI:MaxCompletionTokens ({maxCompletionTokens}). "
            + "Retries and schema remediation add extra completions. "
            + "Override AgentExecution:LlmCostEstimation USD/M rates to match your deployment.";
    }
}
