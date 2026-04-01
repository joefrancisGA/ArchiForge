using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleHealthProbe
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("httpStatus")]
    public int HttpStatus { get; init; }

    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;

    [JsonPropertyName("bodyTruncated")]
    public bool BodyTruncated { get; init; }
}
