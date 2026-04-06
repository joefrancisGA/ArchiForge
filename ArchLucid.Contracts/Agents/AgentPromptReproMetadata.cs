namespace ArchLucid.Contracts.Agents;

/// <summary>
/// Identifies the system prompt template used for an LLM call: stable id, semantic version, UTF-8 content hash, and optional release/A-B label from configuration.
/// </summary>
public sealed record AgentPromptReproMetadata(
    string TemplateId,
    string TemplateVersion,
    string SystemPromptContentSha256Hex,
    string? ReleaseLabel);
