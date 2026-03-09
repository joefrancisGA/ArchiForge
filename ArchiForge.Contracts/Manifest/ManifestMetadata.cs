namespace ArchiForge.Contracts.Manifest;

public sealed class ManifestMetadata
{
    public string ManifestVersion { get; set; } = "v1";

    public string? ParentManifestVersion { get; set; }

    public string ChangeDescription { get; set; } = string.Empty;

    public List<string> DecisionTraceIds { get; set; } = [];

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
