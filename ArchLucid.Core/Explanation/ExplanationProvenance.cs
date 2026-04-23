namespace ArchLucid.Core.Explanation;

/// <summary>
///     Identifies which agent configuration and prompt revision produced an <see cref="ExplanationResult" />.
/// </summary>
public sealed record ExplanationProvenance(
    string AgentType,
    string ModelId,
    string? PromptTemplateId,
    string? PromptTemplateVersion,
    string? PromptContentHash);
