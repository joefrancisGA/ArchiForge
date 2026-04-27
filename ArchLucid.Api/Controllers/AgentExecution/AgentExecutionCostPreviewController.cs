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
    ILlmCostEstimator llmCostEstimator) : ControllerBase
{
    /// <summary>
    ///     Illustrative upper bound for one completion at the host output cap; see
    ///     <c>docs/library/PER_TENANT_COST_MODEL.md</c> § Wizard preview.
    /// </summary>
    private const int CostPreviewAssumedInputTokens = 8192;

    private readonly IOptionsMonitor<AgentExecutionOptions> _agentExecutionOptions =
        agentExecutionOptions ?? throw new ArgumentNullException(nameof(agentExecutionOptions));

    private readonly IOptionsMonitor<AzureOpenAiOptions> _azureOpenAiOptions =
        azureOpenAiOptions ?? throw new ArgumentNullException(nameof(azureOpenAiOptions));

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

        string modeLabel = isReal ? "Real" : "Simulator";
        string? deployment =
            isReal && !string.IsNullOrWhiteSpace(azure.DeploymentName) ? azure.DeploymentName.Trim() : null;

        double? usd = null;

        if (isReal)
        {
            decimal? est = _llmCostEstimator.EstimateUsd(CostPreviewAssumedInputTokens, maxOut);

            if (est is not null)
                usd = (double)est.Value;

            ArchLucidInstrumentation.RunsCostPreviewViewedTotal.Add(1);
        }

        AgentExecutionCostPreviewResponse body = new()
        {
            Mode = modeLabel,
            MaxCompletionTokens = maxOut,
            EstimatedCostUsd = usd,
            DeploymentName = deployment
        };

        return Ok(body);
    }
}
