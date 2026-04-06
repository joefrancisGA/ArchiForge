using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleCliBuildInfo
{
    [JsonPropertyName("informationalVersion")]
    public string InformationalVersion { get; init; } = string.Empty;

    [JsonPropertyName("assemblyVersion")]
    public string AssemblyVersion { get; init; } = string.Empty;

    [JsonPropertyName("runtimeFramework")]
    public string RuntimeFramework { get; init; } = string.Empty;
}
