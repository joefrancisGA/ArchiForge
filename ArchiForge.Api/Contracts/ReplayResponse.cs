namespace ArchiForge.Api.HttpContracts;

public class ReplayResponse
{
    public Guid RunId { get; set; }
    public string Mode { get; set; } = default!;
    public DateTime ReplayedUtc { get; set; }

    public Guid? RebuiltManifestId { get; set; }
    public string? RebuiltManifestHash { get; set; }
    public Guid? RebuiltArtifactBundleId { get; set; }

    public ReplayValidationResponse Validation { get; set; } = new();
}
