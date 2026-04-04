namespace ArchiForge.Core.Configuration;

/// <summary>Configuration for prompt versioning labels surfaced in telemetry (<c>AgentPrompts</c> section).</summary>
public sealed class AgentPromptCatalogOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AgentPrompts";

    /// <summary>Per <c>agentTypeKey</c> version label (e.g. <c>v2026-04</c>).</summary>
    public Dictionary<string, string> Versions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
