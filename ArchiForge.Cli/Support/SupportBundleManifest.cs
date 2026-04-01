using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleManifest
{
    [JsonPropertyName("bundleFormatVersion")]
    public string BundleFormatVersion { get; init; } = "1.0";

    [JsonPropertyName("createdUtc")]
    public string CreatedUtc { get; init; } = string.Empty;

    [JsonPropertyName("cliWorkingDirectory")]
    public string CliWorkingDirectory { get; init; } = string.Empty;

    [JsonPropertyName("notes")]
    public string Notes { get; init; } =
        "No secrets, connection strings, or API key values are included. Sensitive env vars appear only as (set)/(not set).";
}
