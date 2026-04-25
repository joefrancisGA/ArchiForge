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

    /// <summary>Upper-bound USD estimate from host <c>ILlmCostEstimator</c>, or null when estimation is disabled or mode is Simulator.</summary>
    public double? EstimatedCostUsd
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
