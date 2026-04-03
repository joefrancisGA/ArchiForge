using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.AgentRuntime;

[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Hard cap on model output tokens per completion (maps to <c>MaxOutputTokenCount</c> on the chat request).
    /// When unset or zero, <see cref="AzureOpenAiCompletionClient.DefaultMaxCompletionTokens"/> is used so deployments are never unbounded by default.
    /// </summary>
    public int MaxCompletionTokens { get; set; }
}
