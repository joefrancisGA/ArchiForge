using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleBuildSection
{
    [JsonPropertyName("cli")]
    public SupportBundleCliBuildInfo Cli { get; init; } = new();

    [JsonPropertyName("apiVersionJson")]
    public string? ApiVersionJson { get; init; }

    [JsonPropertyName("apiVersionError")]
    public string? ApiVersionError { get; init; }
}
