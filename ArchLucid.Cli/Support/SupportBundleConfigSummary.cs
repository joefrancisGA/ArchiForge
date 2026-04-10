using System.Text.Json.Serialization;

namespace ArchLucid.Cli.Support;

public sealed class SupportBundleConfigSummary
{
    [JsonPropertyName("hasArchlucidJson")]
    public bool HasArchlucidJson { get; init; }

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
    public ArchLucidProjectScaffolder.ArchitectureSection? Architecture { get; init; }
}
