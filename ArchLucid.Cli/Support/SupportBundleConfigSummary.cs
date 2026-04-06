using System.Text.Json.Serialization;

namespace ArchiForge.Cli.Support;

public sealed class SupportBundleConfigSummary
{
    [JsonPropertyName("hasArchiforgeJson")]
    public bool HasArchiforgeJson { get; init; }

    [JsonPropertyName("projectName")]
    public string? ProjectName { get; init; }

    [JsonPropertyName("schemaVersion")]
    public string? SchemaVersion { get; init; }

    [JsonPropertyName("apiBaseUrlRedacted")]
    public string ApiBaseUrlRedacted { get; init; } = string.Empty;

    [JsonPropertyName("inputsBriefPath")]
    public string? InputsBriefPath { get; init; }

    [JsonPropertyName("outputsLocalCacheDir")]
    public string? OutputsLocalCacheDir { get; init; }

    [JsonPropertyName("pluginsLockFile")]
    public string? PluginsLockFile { get; init; }

    [JsonPropertyName("terraformEnabled")]
    public bool? TerraformEnabled { get; init; }

    [JsonPropertyName("terraformPath")]
    public string? TerraformPath { get; init; }

    [JsonPropertyName("architecture")]
    public ArchiForgeProjectScaffolder.ArchitectureSection? Architecture { get; init; }
}
