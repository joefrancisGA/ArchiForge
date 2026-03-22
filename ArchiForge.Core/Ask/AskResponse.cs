namespace ArchiForge.Core.Ask;

/// <summary>Grounded answer plus explicit references the model was instructed to cite.</summary>
public sealed class AskResponse
{
    public string Answer { get; set; } = "";

    public List<string> ReferencedDecisions { get; set; } = [];

    public List<string> ReferencedFindings { get; set; } = [];

    public List<string> ReferencedArtifacts { get; set; } = [];
}
