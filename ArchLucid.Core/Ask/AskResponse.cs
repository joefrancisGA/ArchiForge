namespace ArchiForge.Core.Ask;

/// <summary>Grounded answer plus explicit references the model was instructed to cite.</summary>
public sealed class AskResponse
{
    /// <summary>Conversation thread id (new or existing).</summary>
    public Guid ThreadId { get; set; }

    /// <summary>Natural-language answer (or fallback when the LLM is unreachable).</summary>
    public string Answer { get; set; } = "";

    /// <summary>Decision titles/ids the model cited (normalized, case-insensitive distinct).</summary>
    public List<string> ReferencedDecisions { get; set; } = [];

    /// <summary>Finding references when provided by the model.</summary>
    public List<string> ReferencedFindings { get; set; } = [];

    /// <summary>Artifact or provenance labels when provided by the model.</summary>
    public List<string> ReferencedArtifacts { get; set; } = [];
}
