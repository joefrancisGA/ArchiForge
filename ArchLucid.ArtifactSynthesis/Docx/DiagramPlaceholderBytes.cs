namespace ArchiForge.ArtifactSynthesis.Docx;

/// <summary>Minimal valid PNG (1×1 px) for v1 diagram placeholder until Mermaid/graph rendering exists.</summary>
internal static class DiagramPlaceholderBytes
{
    private static readonly byte[] PngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public static ReadOnlySpan<byte> Png => PngBytes;
}
