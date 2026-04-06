using ArchLucid.Contracts.Agents;

namespace ArchLucid.AgentRuntime.Prompts;

/// <summary>Materialized system prompt plus reproducibility metadata for tracing and OTel.</summary>
public sealed record ResolvedSystemPrompt(
    string Text,
    string TemplateId,
    string TemplateVersion,
    string ContentSha256Hex,
    string? ReleaseLabel)
{
    public AgentPromptReproMetadata ToReproMetadata() =>
        new(TemplateId, TemplateVersion, ContentSha256Hex, ReleaseLabel);
}
