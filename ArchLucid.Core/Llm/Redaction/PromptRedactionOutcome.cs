namespace ArchLucid.Core.Llm.Redaction;

/// <summary>Result of applying deny-list redaction to a single prompt string.</summary>
public sealed class PromptRedactionOutcome(string text, IReadOnlyDictionary<string, int> countsByCategory)
{
    public string Text
    {
        get;
    } = text;

    public IReadOnlyDictionary<string, int> CountsByCategory
    {
        get;
    } = countsByCategory;
}
