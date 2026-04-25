namespace ArchLucid.Core.Llm.Redaction;

/// <summary>Applies configured deny-list redaction to outbound LLM prompt material.</summary>
public interface IPromptRedactor
{
    /// <summary>When <paramref name="input" /> is null or empty, returns it unchanged with zero counts.</summary>
    PromptRedactionOutcome Redact(string? input);
}
