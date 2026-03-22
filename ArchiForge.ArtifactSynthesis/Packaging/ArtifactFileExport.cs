namespace ArchiForge.ArtifactSynthesis.Packaging;

public class ArtifactFileExport
{
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
