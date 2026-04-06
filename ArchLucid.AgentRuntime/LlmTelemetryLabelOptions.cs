namespace ArchiForge.AgentRuntime;

/// <summary>Provider labels attached to LLM token metrics (configured in DI, not required in appsettings).</summary>
public sealed class LlmTelemetryLabelOptions
{
    public string ProviderId { get; set; } = "unknown";

    public string ModelDeploymentLabel { get; set; } = "unknown";
}
