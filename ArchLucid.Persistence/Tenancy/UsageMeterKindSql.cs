using ArchLucid.Core.Metering;

namespace ArchLucid.Persistence.Tenancy;

internal static class UsageMeterKindSql
{
    internal static string ToKindString(UsageMeterKind kind)
    {
        return kind switch
        {
            UsageMeterKind.LlmPromptTokens => "LlmPromptTokens",
            UsageMeterKind.LlmCompletionTokens => "LlmCompletionTokens",
            UsageMeterKind.ApiRequest => "ApiRequest",
            UsageMeterKind.ArchitectureRun => "ArchitectureRun",
            UsageMeterKind.ArtifactStorageBytes => "ArtifactStorageBytes",
            UsageMeterKind.AgentExecution => "AgentExecution",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    internal static UsageMeterKind ParseKind(string value)
    {
        return value switch
        {
            "LlmPromptTokens" => UsageMeterKind.LlmPromptTokens,
            "LlmCompletionTokens" => UsageMeterKind.LlmCompletionTokens,
            "ApiRequest" => UsageMeterKind.ApiRequest,
            "ArchitectureRun" => UsageMeterKind.ArchitectureRun,
            "ArtifactStorageBytes" => UsageMeterKind.ArtifactStorageBytes,
            "AgentExecution" => UsageMeterKind.AgentExecution,
            _ => throw new FormatException($"Unknown usage kind: {value}")
        };
    }
}
