namespace ArchiForge.Api.HttpContracts;

public class ReplayValidationResponse
{
    public bool ContextPresent { get; set; }
    public bool GraphPresent { get; set; }
    public bool FindingsPresent { get; set; }
    public bool ManifestPresent { get; set; }
    public bool TracePresent { get; set; }
    public bool ArtifactsPresent { get; set; }
    public bool ManifestHashMatches { get; set; }
    public bool ArtifactBundlePresentAfterReplay { get; set; }
    public List<string> Notes { get; set; } = new();
}
