using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.AgentRuntime;

[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
}
