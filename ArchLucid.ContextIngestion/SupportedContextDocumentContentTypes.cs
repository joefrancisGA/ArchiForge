namespace ArchiForge.ContextIngestion;

/// <summary>
/// Canonical list of MIME types accepted for inline <see cref="Models.ContextDocumentReference"/> at the API boundary.
/// Parsers (<see cref="Contracts.IContextDocumentParser"/>) should register support for these (or a subset).
/// </summary>
public static class SupportedContextDocumentContentTypes
{
    public static readonly IReadOnlyList<string> All =
    [
        "text/plain",
        "text/markdown"
    ];

    public static bool IsSupported(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType) && All.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }
}
