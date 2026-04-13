namespace ArchLucid.AgentRuntime;

/// <summary>Configuration for optional full prompt/response persistence to blob storage.</summary>
public sealed class AgentExecutionTraceStorageOptions
{
    public const string SectionPath = "AgentExecution:TraceStorage";

    /// <summary>When true, full (unsanitized) prompts and responses are uploaded asynchronously after trace insert.</summary>
    public bool PersistFullPrompts { get; set; } = true;
}
