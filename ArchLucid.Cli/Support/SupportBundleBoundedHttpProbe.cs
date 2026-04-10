using System.Text.Json.Serialization;

namespace ArchLucid.Cli.Support;

/// <summary>HTTP GET result with a UTF-8 body cap to keep bundles small.</summary>
public sealed class SupportBundleBoundedHttpProbe
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("httpStatus")]
    public int HttpStatus { get; init; }

    [JsonPropertyName("bodyPreview")]
    public string BodyPreview { get; init; } = string.Empty;

    [JsonPropertyName("bodyTruncated")]
    public bool BodyTruncated { get; init; }

    [JsonPropertyName("maxBytesCaptured")]
    public int MaxBytesCaptured { get; init; }
}
