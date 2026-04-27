using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Core.Configuration;

[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    /// <summary>Used when <c>AzureOpenAI:MaxCompletionTokens</c> is omitted or zero.</summary>
    public const int DefaultMaxCompletionTokens = 4096;

    public string Endpoint
    {
        get;
        set;
    } = string.Empty;

    public string ApiKey
    {
        get;
        set;
    } = string.Empty;

    public string DeploymentName
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     Hard cap on model output tokens per completion (maps to <c>MaxOutputTokenCount</c> on the chat request).
    ///     When unset or zero, <see cref="DefaultMaxCompletionTokens" /> is used so deployments are never unbounded by default.
    /// </summary>
    public int MaxCompletionTokens
    {
        get;
        set;
    }
}
