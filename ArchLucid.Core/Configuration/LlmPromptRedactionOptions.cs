namespace ArchLucid.Core.Configuration;

/// <summary>Outbound LLM prompt deny-list redaction (defense-in-depth before Azure OpenAI and trace persistence).</summary>
public sealed class LlmPromptRedactionOptions
{
    public const string SectionName = "LlmPromptRedaction";

    /// <summary>When false, prompts are forwarded and persisted without regex redaction (emits skipped counter).</summary>
    public bool Enabled
    {
        get;
        set;
    } = true;

    /// <summary>Replacement text for each redacted span.</summary>
    public string ReplacementToken
    {
        get;
        set;
    } = "[REDACTED]";

    /// <summary>Optional extra regex patterns (each entire match is replaced); use sparingly — false positives cost signal.</summary>
    public IReadOnlyList<string> DenyListRegexes
    {
        get;
        set;
    } = [];
}
