using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleHealthSection
{
    [JsonPropertyName("live")]
    public SupportBundleHealthProbe Live { get; init; } = new();

    [JsonPropertyName("ready")]
    public SupportBundleHealthProbe Ready { get; init; } = new();

    [JsonPropertyName("combined")]
    public SupportBundleHealthProbe Combined { get; init; } = new();
}
