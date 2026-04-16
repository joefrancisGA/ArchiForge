namespace ArchLucid.Core.Metering;

/// <summary>Kinds of billable or capacity usage recorded in <c>dbo.UsageEvents</c>.</summary>
public enum UsageMeterKind
{
    LlmPromptTokens = 0,

    LlmCompletionTokens = 1,

    ApiRequest = 2,

    ArchitectureRun = 3,

    ArtifactStorageBytes = 4,

    AgentExecution = 5,
}
